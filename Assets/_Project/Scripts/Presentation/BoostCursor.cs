using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet.Presentation
{
    /// <summary>
    /// Программный курсор ускоренной панорамы (СКМ). Пока зажата СКМ и мышь уведена за
    /// мёртвую зону, системный курсор прячется, а вместо него рисуется стрелка (CursorBoost2),
    /// повёрнутая в сторону ускорения камеры. Факт ускорения и направление берём у RtsCamera.
    ///
    /// Рисуем через OnGUI: аппаратный курсор (Cursor.SetCursor) вращать нельзя, поэтому на
    /// время boost переходим на программную отрисовку.
    /// </summary>
    public sealed class BoostCursor : MonoBehaviour
    {
        [Tooltip("Камера, чья СКМ-панорама рисует boost-курсор. Пусто — найдём в сцене.")]
        [SerializeField] private RtsCamera _camera;
        [Tooltip("Текстура-стрелка, указывающая ВВЕРХ (CursorBoost2). Тип импорта — Default.")]
        [SerializeField] private Texture2D _boostTexture;
        [Tooltip("Размер значка на экране, пиксели.")]
        [SerializeField] private Vector2 _size = new Vector2(48f, 48f);

        private bool _active;

        private void Awake()
        {
            if (_camera == null) _camera = FindFirstObjectByType<RtsCamera>();
        }

        private void Update()
        {
            bool boost = _boostTexture != null && _camera != null && _camera.IsBoostPanning;
            if (boost == _active) return;
            _active = boost;
            Cursor.visible = !boost; // на время boost прячем системный курсор
        }

        private void OnGUI()
        {
            if (!_active || Event.current.type != EventType.Repaint) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mp = mouse.position.ReadValue();
            Vector2 pivot = new Vector2(mp.x, Screen.height - mp.y); // GUI-координаты: y сверху вниз

            // Стрелка в текстуре смотрит вверх (0,+1); поворачиваем её к экранному направлению
            // ускорения. Atan2(x, y) даёт угол по часовой от «вверх», что совпадает со знаком
            // поворота GUIUtility.RotateAroundPivot в GUI-пространстве (y вниз).
            Vector2 dir = _camera.BoostOffsetPixels;
            float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot);
            GUI.DrawTexture(new Rect(pivot.x - _size.x * 0.5f, pivot.y - _size.y * 0.5f, _size.x, _size.y), _boostTexture);
            GUI.matrix = saved;
        }

        private void OnDisable()
        {
            if (!_active) return;
            _active = false;
            Cursor.visible = true; // вернуть системный курсор, если выключились посреди boost
        }
    }
}
