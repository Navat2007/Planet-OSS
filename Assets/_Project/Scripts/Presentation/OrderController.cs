using System.Collections.Generic;
using Planet.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet.Presentation
{
    /// <summary>
    /// Приказы движения (Фаза 5):
    ///  - ПКМ-клик по земле — движение выделенных в точку (строем);
    ///  - зажать ПКМ и тянуть — задать направление (facing), показывается призрак-превью;
    ///  - Shift+ПКМ — добавить точку в очередь маршрута (waypoints);
    ///  - Shift + зажать ПКМ и вести — рисовать маршрут: точка в очередь каждые PaintInterval
    ///    секунд, если курсор сместился не меньше чем на PaintMinDistance (см. GameplaySettings).
    /// Приказ оформляется как детерминированная команда (готово к lockstep).
    /// </summary>
    public sealed class OrderController : MonoBehaviour
    {
        [SerializeField] private int _localOwnerId = 0;

        private GameplaySettings _s;
        private Camera _cam;
        private SimRunner _runner;
        private SelectionController _selection;
        private GhostPreview _ghost;

        private bool _rmbDown;
        private bool _facing;
        private bool _painting;       // режим рисования маршрута (Shift на нажатии ПКМ)
        private float _paintTimer;
        private Vector3 _pressGround;
        private Vector3 _lastPaintPoint;

        private readonly List<UnitView> _tmpViews = new List<UnitView>();

        public void Init(SimRunner runner, SelectionController selection)
        {
            _runner = runner;
            _selection = selection;
            _s = GameplaySettings.Instance;
            _cam = Camera.main;
            _ghost = new GameObject("GhostPreview").AddComponent<GhostPreview>();
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            var mouse = Mouse.current;
            if (_cam == null || mouse == null || _runner == null || _runner.World == null || _selection == null) return;

            Vector2 screen = mouse.position.ReadValue();

            if (mouse.rightButton.wasPressedThisFrame && TryGroundPoint(screen, out Vector3 press))
            {
                _pressGround = press;
                _rmbDown = true;
                _facing = false;
                _painting = IsShiftHeld(); // режим решается на нажатии: Shift → рисуем маршрут

                if (_painting)
                {
                    // первый сегмент маршрута — в очередь (как обычный Shift+ПКМ)
                    IssueMove(press, queue: true, facingWorld: Vector3.zero, marker: false);
                    _lastPaintPoint = press;
                    _paintTimer = 0f;
                }
            }

            if (_rmbDown && mouse.rightButton.isPressed && TryGroundPoint(screen, out Vector3 cur))
            {
                if (_painting)
                {
                    _paintTimer += Time.deltaTime;
                    if (_paintTimer >= _s.PaintInterval)
                    {
                        _paintTimer = 0f;
                        Vector3 m = cur - _lastPaintPoint; m.y = 0f;
                        if (m.magnitude >= _s.PaintMinDistance) // только если курсор сместился
                        {
                            IssueMove(cur, queue: true, facingWorld: Vector3.zero, marker: false);
                            _lastPaintPoint = cur;
                        }
                    }
                }
                else
                {
                    Vector3 d = cur - _pressGround;
                    d.y = 0f;
                    if (!_facing && d.magnitude > _s.FacingDragMeters)
                    {
                        _facing = true;
                        _ghost.Begin(SelectedViews()); // клоны моделей под текущее выделение — один раз
                    }
                    if (_facing) _ghost.UpdatePose(_pressGround, d);
                }
            }

            if (_rmbDown && mouse.rightButton.wasReleasedThisFrame)
            {
                _rmbDown = false;
                _ghost.Hide();

                if (_painting)
                {
                    // финальная точка маршрута, если сместились от последней
                    if (TryGroundPoint(screen, out Vector3 rel))
                    {
                        Vector3 m = rel - _lastPaintPoint; m.y = 0f;
                        if (m.magnitude >= _s.PaintMinDistance)
                            IssueMove(rel, queue: true, facingWorld: Vector3.zero, marker: false);
                    }
                    _painting = false;
                }
                else
                {
                    Vector3 facingWorld = Vector3.zero;
                    if (_facing && TryGroundPoint(screen, out Vector3 rel))
                    {
                        facingWorld = rel - _pressGround;
                        facingWorld.y = 0f;
                    }
                    IssueMove(_pressGround, queue: false, facingWorld);
                }
            }
        }

        private bool TryGroundPoint(Vector2 screen, out Vector3 world)
        {
            world = default;
            Ray ray = _cam.ScreenPointToRay(screen);

            if (Physics.Raycast(ray, out RaycastHit hit, 2000f))
            {
                world = hit.point;
                return true;
            }
            if (Mathf.Abs(ray.direction.y) > 1e-4f)
            {
                float t = -ray.origin.y / ray.direction.y;
                if (t > 0f) { world = ray.origin + ray.direction * t; return true; }
            }
            return false;
        }

        private void IssueMove(Vector3 world, bool queue, Vector3 facingWorld, bool marker = true)
        {
            var views = SelectedViews();
            if (views.Count == 0) return;

            var ids = new int[views.Count];
            for (int i = 0; i < views.Count; i++)
            {
                ids[i] = views[i].Entity.Id;
                if (!queue) views[i].RoutePoints.Clear(); // новый приказ — сбросить маршрут
                views[i].RoutePoints.Add(world);          // точка ровно там, где кликнули
                views[i].RouteFreshTime = Time.time;      // grace: не срезать точки, пока команда не исполнилась
            }

            SimVector2 target = SimConvert.ToSim(world);
            SimVector2 facing = facingWorld.sqrMagnitude > 0.01f ? SimConvert.ToSim(facingWorld) : SimVector2.Zero;

            _runner.Schedule.Add(_runner.World.CurrentTick + 1,
                new MoveOrderCommand(_localOwnerId, ids, target, queue, facing));
            if (marker) MoveMarker.Spawn(world);
        }

        private IReadOnlyList<UnitView> SelectedViews()
        {
            _tmpViews.Clear();
            foreach (var v in _selection.Selected)
                if (v != null && v.Entity != null && v.Entity.Alive && v.Entity.OwnerId == _localOwnerId)
                    _tmpViews.Add(v);
            return _tmpViews;
        }

        private static bool IsShiftHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }
    }
}
