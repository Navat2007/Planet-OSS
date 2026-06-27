using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Planet.Presentation
{
    /// <summary>
    /// Выделение юнитов (Фаза 4): ЛКМ — выбрать один, рамкой — группу, двойной клик — всех
    /// такого же типа на экране, Shift — добавить/убрать. Выделяются только свои (localOwnerId).
    /// Выделение — локальное состояние, на симуляцию/lockstep не влияет.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        [SerializeField] private int _localOwnerId = 0;
        [SerializeField] private float _clickPixelThreshold = 35f;
        [SerializeField] private float _dragStartThreshold = 8f;
        [SerializeField] private float _doubleClickTime = 0.3f;

        private Camera _cam;
        private readonly HashSet<UnitView> _selected = new HashSet<UnitView>();
        private readonly List<UnitView> _selectionScratch = new List<UnitView>();
        private Vector2 _dragStart;
        private bool _dragging;
        private bool _isBox;
        private float _lastClickTime = -1f;
        private UnitView _lastClicked;

        public IReadOnlyCollection<UnitView> Selected => _selected;

        private void Awake() => _cam = Camera.main;

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            var mouse = Mouse.current;
            if (_cam == null || mouse == null) return;

            Vector2 pos = mouse.position.ReadValue();
            PruneInvalidSelection();

            // Раздельные проверки (не else-if): быстрый клик может прийти press+release в одном кадре.
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUi())
                {
                    _dragging = false;
                    _isBox = false;
                    return;
                }

                _dragStart = pos;
                _dragging = true;
                _isBox = false;
            }

            if (_dragging && !_isBox && (pos - _dragStart).magnitude > _dragStartThreshold)
                _isBox = true;

            if (_dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                _dragging = false;
                bool additive = IsShiftHeld();
                if (_isBox) DoBox(_dragStart, pos, additive);
                else DoClick(pos, additive);
            }
        }

        private void DoClick(Vector2 screenPos, bool additive)
        {
            UnitView hit = PickNearest(screenPos);

            bool isDouble = hit != null && hit == _lastClicked &&
                            (Time.unscaledTime - _lastClickTime) <= _doubleClickTime;
            _lastClickTime = Time.unscaledTime;
            _lastClicked = hit;

            if (isDouble && hit != null)
            {
                if (!additive) Clear();
                foreach (var v in UnitView.Active)
                    if (IsSelectable(v) && v.TypeKey == hit.TypeKey && OnScreen(v))
                        Add(v);
                return;
            }

            if (hit == null)
            {
                if (!additive) Clear();
                return;
            }

            if (additive)
            {
                if (_selected.Contains(hit)) Remove(hit); else Add(hit);
            }
            else
            {
                Clear();
                Add(hit);
            }
        }

        private void DoBox(Vector2 a, Vector2 b, bool additive)
        {
            if (!additive) Clear();
            Rect r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            foreach (var v in UnitView.Active)
            {
                if (!IsSelectable(v)) continue;
                if (ScreenFootprint(v, out Rect footprint, out _) && r.Overlaps(footprint, true)) Add(v);
            }
        }

        private UnitView PickNearest(Vector2 screenPos)
        {
            UnitView best = null;
            float bestScore = 1f;
            foreach (var v in UnitView.Active)
            {
                if (!IsSelectable(v)) continue;
                if (!ScreenFootprint(v, out Rect footprint, out Vector2 center)) continue;

                float radiusPixels = Mathf.Max(_clickPixelThreshold, Mathf.Max(footprint.width, footprint.height) * 0.5f);
                float distance = (center - screenPos).magnitude;
                if (!footprint.Contains(screenPos) && distance > radiusPixels) continue;

                float score = distance / Mathf.Max(radiusPixels, 1f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = v;
                }
            }
            return best;
        }

        private bool IsSelectable(UnitView v) =>
            v != null && v.Entity != null && v.Entity.Alive && v.Entity.OwnerId == _localOwnerId;

        private bool ScreenPos(UnitView v, out Vector2 sp)
        {
            Vector3 w = _cam.WorldToScreenPoint(v.transform.position + Vector3.up * 0.5f);
            sp = new Vector2(w.x, w.y);
            return w.z > 0f;
        }

        private bool OnScreen(UnitView v) =>
            ScreenFootprint(v, out Rect footprint, out _) &&
            footprint.Overlaps(new Rect(0f, 0f, Screen.width, Screen.height), true);

        private void Add(UnitView v) { if (_selected.Add(v)) v.SetSelected(true); }
        private void Remove(UnitView v) { if (_selected.Remove(v)) v.SetSelected(false); }
        private void Clear() { foreach (var v in _selected) if (v != null) v.SetSelected(false); _selected.Clear(); }

        private void PruneInvalidSelection()
        {
            _selectionScratch.Clear();
            foreach (var v in _selected)
                if (!IsSelectable(v))
                    _selectionScratch.Add(v);

            foreach (var v in _selectionScratch)
                Remove(v);

            if (_lastClicked != null && !IsSelectable(_lastClicked))
                _lastClicked = null;
        }

        private bool ScreenFootprint(UnitView v, out Rect footprint, out Vector2 center)
        {
            footprint = default;
            center = default;

            if (!ScreenPos(v, out center)) return false;

            float radius = Mathf.Max(v.SelectionRadius, 0.25f);
            Vector3 origin = v.transform.position;
            SpanWorldFootprint(origin, radius, out Vector2 min, out Vector2 max);

            const float minPickHalfSize = 8f;
            min.x = Mathf.Min(min.x, center.x - minPickHalfSize);
            min.y = Mathf.Min(min.y, center.y - minPickHalfSize);
            max.x = Mathf.Max(max.x, center.x + minPickHalfSize);
            max.y = Mathf.Max(max.y, center.y + minPickHalfSize);

            footprint = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private void SpanWorldFootprint(Vector3 origin, float radius, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            AddProjectedPoint(origin + new Vector3(-radius, 0f, -radius), ref min, ref max);
            AddProjectedPoint(origin + new Vector3(-radius, 0f, radius), ref min, ref max);
            AddProjectedPoint(origin + new Vector3(radius, 0f, -radius), ref min, ref max);
            AddProjectedPoint(origin + new Vector3(radius, 0f, radius), ref min, ref max);
            AddProjectedPoint(origin + Vector3.up * Mathf.Max(radius, 0.5f), ref min, ref max);
        }

        private void AddProjectedPoint(Vector3 world, ref Vector2 min, ref Vector2 max)
        {
            Vector3 p = _cam.WorldToScreenPoint(world);
            min.x = Mathf.Min(min.x, p.x);
            min.y = Mathf.Min(min.y, p.y);
            max.x = Mathf.Max(max.x, p.x);
            max.y = Mathf.Max(max.y, p.y);
        }

        private void OnDisable() => Clear();

        private static bool IsPointerOverUi()
        {
            var mouse = Mouse.current;
            if (mouse != null && PointerInputBlockers.BlocksScreenPoint(mouse.position.ReadValue()))
                return true;

            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsShiftHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }

        // --- Рамка выделения (IMGUI) ---

        private static Texture2D _px;

        private void OnGUI()
        {
            if (!_dragging || !_isBox || Mouse.current == null) return;

            Vector2 cur = Mouse.current.position.ReadValue();
            float x0 = Mathf.Min(_dragStart.x, cur.x);
            float x1 = Mathf.Max(_dragStart.x, cur.x);
            float y0 = Screen.height - Mathf.Max(_dragStart.y, cur.y); // экран→GUI (инверсия Y)
            float y1 = Screen.height - Mathf.Min(_dragStart.y, cur.y);
            DrawRectOutline(new Rect(x0, y0, x1 - x0, y1 - y0), new Color(0.3f, 1f, 0.4f, 1f));
        }

        private static void DrawRectOutline(Rect r, Color c)
        {
            if (_px == null)
            {
                _px = new Texture2D(1, 1);
                _px.SetPixel(0, 0, Color.white);
                _px.Apply();
            }

            Color old = GUI.color;
            GUI.color = c;
            const float t = 2f;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), _px);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), _px);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), _px);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), _px);
            GUI.color = old;
        }
    }

    public static class PointerInputBlockers
    {
        private static readonly List<Func<Vector2, bool>> Blockers = new List<Func<Vector2, bool>>();

        public static void Register(Func<Vector2, bool> blocker)
        {
            if (blocker != null && !Blockers.Contains(blocker))
                Blockers.Add(blocker);
        }

        public static void Unregister(Func<Vector2, bool> blocker)
        {
            if (blocker != null)
                Blockers.Remove(blocker);
        }

        public static bool BlocksScreenPoint(Vector2 screenPosition)
        {
            for (int i = Blockers.Count - 1; i >= 0; i--)
            {
                Func<Vector2, bool> blocker = Blockers[i];
                if (blocker != null && blocker(screenPosition))
                    return true;
            }

            return false;
        }
    }
}
