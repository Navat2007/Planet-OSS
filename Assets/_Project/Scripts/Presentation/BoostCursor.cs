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
    ///
    /// Живёт глобально: создаётся один раз через <see cref="BoostCursorBootstrap"/> и
    /// переживает смену сцен, поэтому камеру (она у каждой сцены своя) переподхватывает
    /// автоматически, когда старая ссылка становится недействительной.
    /// </summary>
    public sealed class BoostCursor : MonoBehaviour
    {
        [Tooltip("Камера, чья СКМ-панорама рисует boost-курсор. Пусто — найдём в активной сцене.")]
        [SerializeField] private RtsCamera _camera;
        [Tooltip("Текстура-стрелка, указывающая ВВЕРХ (CursorBoost2).")]
        [SerializeField] private Texture2D _boostTexture;
        [Tooltip("Размер значка на экране, пиксели.")]
        [SerializeField] private Vector2 _size = new Vector2(48f, 48f);

        private bool _active;

        public static BoostCursor Instance { get; private set; }

        /// <summary>Задать текстуру/размер programmatically (используется BoostCursorBootstrap).</summary>
        public void Configure(Texture2D texture, Vector2 size)
        {
            _boostTexture = texture;
            _size = size;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // дубликат при возврате в сцену — убираем
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Камера живёт в игровой сцене (не persistent), поэтому при смене сцены старая
            // ссылка «умирает» — переподхватываем, пока не найдём (или сцена без камеры, например меню).
            if (_camera == null) _camera = FindFirstObjectByType<RtsCamera>();

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
