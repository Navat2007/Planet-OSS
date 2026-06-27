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
    ///  - Shift+ПКМ — добавить точку в очередь маршрута (waypoints).
    /// Приказ оформляется как детерминированная команда (готово к lockstep).
    /// </summary>
    public sealed class OrderController : MonoBehaviour
    {
        [SerializeField] private int _localOwnerId = 0;
        [SerializeField] private float _facingDragMeters = 1.2f;

        private Camera _cam;
        private SimRunner _runner;
        private SelectionController _selection;
        private GhostPreview _ghost;

        private bool _rmbDown;
        private bool _facing;
        private Vector3 _pressGround;

        private readonly List<UnitView> _tmpViews = new List<UnitView>();

        public void Init(SimRunner runner, SelectionController selection)
        {
            _runner = runner;
            _selection = selection;
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
            }

            if (_rmbDown && mouse.rightButton.isPressed && TryGroundPoint(screen, out Vector3 cur))
            {
                Vector3 d = cur - _pressGround;
                d.y = 0f;
                if (!_facing && d.magnitude > _facingDragMeters)
                {
                    _facing = true;
                    _ghost.Begin(SelectedViews()); // клоны моделей под текущее выделение — один раз
                }
                if (_facing) _ghost.UpdatePose(_pressGround, d);
            }

            if (_rmbDown && mouse.rightButton.wasReleasedThisFrame)
            {
                _rmbDown = false;
                _ghost.Hide();

                Vector3 facingWorld = Vector3.zero;
                if (_facing && TryGroundPoint(screen, out Vector3 rel))
                {
                    facingWorld = rel - _pressGround;
                    facingWorld.y = 0f;
                }
                IssueMove(_pressGround, IsShiftHeld(), facingWorld);
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

        private void IssueMove(Vector3 world, bool queue, Vector3 facingWorld)
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
            MoveMarker.Spawn(world);
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
