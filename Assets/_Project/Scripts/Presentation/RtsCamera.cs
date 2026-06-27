using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet.Presentation
{
    /// <summary>
    /// RTS-камера в стиле Generals: Zero Hour (Фаза 2). Чисто презентационный компонент —
    /// на симуляцию не влияет.
    ///
    /// Модель: точка фокуса (pivot) скользит по земле в плоскости XZ; камера висит над ней
    /// на фиксированном наклоне (pitch) на расстоянии distance, с поворотом по рысканью (yaw).
    ///  - Панорама: WASD/стрелки + скролл краями экрана.
    ///  - Зум: колесо мыши, плавный (целевое расстояние + сглаживание).
    ///  - Поворот: Q/E.
    ///  - Pivot ограничен прямоугольником карты.
    ///
    /// Компонент самодостаточен: параметры — поля в Инспекторе, сохраняются в сцене.
    /// [ExecuteAlways] + OnValidate дают живой предпросмотр кадра в редакторе.
    /// Ввод обрабатывается только в Play-режиме.
    /// </summary>
    [ExecuteAlways]
    public sealed class RtsCamera : MonoBehaviour
    {
        [Header("Пресет настроек (общий для карт)")]
        [Tooltip("Если задан — зум/панорама/поворот/наклон берутся отсюда (вынесено в настройки).")]
        [SerializeField] private CameraSettings _settings;

        [Header("Старт (кадрирование уровня)")]
        [SerializeField] private Vector3 _startPivot = Vector3.zero;
        [SerializeField] private float _startYaw = 0f;
        [SerializeField] private float _startDistance = 32f;

        [Header("Панорама")]
        [SerializeField] private float _panSpeed = 30f;          // м/с
        [SerializeField] private int _edgeScrollPixels = 8;      // зона скролла у края экрана
        [SerializeField] private bool _edgeScrollEnabled = true;

        [Header("Зум")]
        [SerializeField] private float _zoomStep = 4f;           // на сколько метров приближает один щелчок колеса
        [SerializeField] private float _zoomLerpSpeed = 10f;     // скорость сглаживания зума (больше = резче)
        [SerializeField] private float _minDistance = 12f;
        [SerializeField] private float _maxDistance = 55f;

        [Header("Поворот")]
        [SerializeField] private float _rotateSpeed = 90f;            // град/с (Q/E)
        [Tooltip("Чувствительность поворота при зажатом Alt + движении мыши, град/пиксель.")]
        [SerializeField] private float _mouseRotateSensitivity = 0.2f;
        [SerializeField] private float _pitch = 60f;                 // наклон камеры вниз (fallback)

        [Header("СКМ — панорама с ускорением")]
        [Tooltip("Зажать СКМ и вести мышь: камера ускоряется в сторону смещения курсора. Чем дальше увели мышь от точки нажатия — тем быстрее. Это множитель скорости.")]
        [SerializeField] private float _mmbPanAcceleration = 0.08f;
        [Tooltip("Мёртвая зона у точки нажатия СКМ, пиксели (чтобы лёгкое дрожание не двигало камеру).")]
        [SerializeField] private float _mmbDeadZonePixels = 6f;

        [Header("Границы карты (XZ)")]
        [SerializeField] private Vector2 _mapMin = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 _mapMax = new Vector2(50f, 50f);

        private Vector3 _pivot;
        private float _distance;        // текущее (сглаженное) расстояние
        private float _targetDistance;  // целевое расстояние, к которому плавно идём
        private float _yaw;
        private bool _pointerActivated; // курсор хоть раз двигался — защита от старта в (0,0)
        private Vector2 _mmbAnchor;     // точка нажатия СКМ
        private bool _mmbActive;        // идёт ли СКМ-панорама

        public Vector3 Pivot => _pivot;
        public float Distance => _distance;
        public float TargetDistance => _targetDistance;
        public float Yaw => _yaw;

        /// <summary>Программная настройка (необязательна — обычно всё задаётся в Инспекторе).</summary>
        public void Initialize(Vector3 pivot, Vector2 mapMin, Vector2 mapMax, float yaw = 0f)
        {
            _startPivot = pivot;
            _startYaw = yaw;
            _mapMin = mapMin;
            _mapMax = mapMax;
            Configure();
        }

        /// <summary>Центрировать камеру на точке мира (например, на стартовой точке игрока).</summary>
        public void CenterOn(Vector3 worldPos)
        {
            _startPivot = new Vector3(worldPos.x, 0f, worldPos.z);
            _pivot = ClampToMap(_startPivot);
            ApplyTransform();
        }

        private void Awake() => Configure();

        private void OnValidate() => Configure(); // живой предпросмотр в редакторе

        /// <summary>Применить стартовые параметры к рабочему состоянию и пересобрать трансформ.</summary>
        private void Configure()
        {
            if (_settings != null) SyncFromSettings();
            if (_maxDistance < _minDistance) _maxDistance = _minDistance;
            _distance = Mathf.Clamp(_startDistance, _minDistance, _maxDistance);
            _targetDistance = _distance;
            _yaw = _startYaw;
            _pivot = ClampToMap(_startPivot);
            ApplyTransform();
        }

        /// <summary>Скопировать «ощущение» камеры из ассета настроек в рабочие поля.</summary>
        private void SyncFromSettings()
        {
            _startDistance = _settings.StartDistance;
            _minDistance = _settings.MinDistance;
            _maxDistance = _settings.MaxDistance;
            _zoomStep = _settings.ZoomStep;
            _zoomLerpSpeed = _settings.ZoomLerpSpeed;
            _panSpeed = _settings.PanSpeed;
            _edgeScrollPixels = _settings.EdgeScrollPixels;
            _edgeScrollEnabled = _settings.EdgeScrollEnabled;
            _mmbPanAcceleration = _settings.MmbPanAcceleration;
            _mmbDeadZonePixels = _settings.MmbDeadZonePixels;
            _rotateSpeed = _settings.RotateSpeed;
            _mouseRotateSensitivity = _settings.MouseRotateSensitivity;
            _pitch = _settings.Pitch;
        }

        private void Update()
        {
            if (!Application.isPlaying) return; // в редакторе — только OnValidate-предпросмотр

            float dt = Time.deltaTime;
            Pan(ReadMoveInput(), dt);
            MiddleMousePan(dt);           // СКМ: панорама с ускорением по смещению курсора
            Rotate(ReadRotateInput(), dt);// Q/E
            RotateByMouse();              // Alt + движение мыши
            Zoom(ReadZoomInput());        // меняет целевое расстояние
            UpdateZoomSmoothing(dt);      // плавно подтягивает текущее к целевому
            ApplyTransform();
        }

        // --- Чистая логика движения (тестируемая) ---

        /// <summary>Сдвиг точки фокуса. input: x — вправо, y — вперёд (в локальных осях по yaw).</summary>
        public void Pan(Vector2 input, float dt)
        {
            if (input.sqrMagnitude > 1f) input = input.normalized;
            Vector3 forward = YawForward();
            Vector3 right = YawRight();
            _pivot += (right * input.x + forward * input.y) * (_panSpeed * dt);
            _pivot = ClampToMap(_pivot);
        }

        /// <summary>
        /// Панорама по экранному смещению курсора (СКМ): скорость пропорциональна величине
        /// смещения, направление — к нему. offsetPixels: x — экранный вправо, y — экранный вверх.
        /// </summary>
        public void PanByScreenOffset(Vector2 offsetPixels, float dt)
        {
            Vector3 dir = YawRight() * offsetPixels.x + YawForward() * offsetPixels.y;
            _pivot += dir * (_mmbPanAcceleration * dt);
            _pivot = ClampToMap(_pivot);
        }

        /// <summary>Задать целевой зум по «щелчку» колеса. Положительный scroll — приблизить.</summary>
        public void Zoom(float scroll)
        {
            if (Mathf.Abs(scroll) < 0.01f) return;
            // По знаку колеса — один шаг. Так зум одинаков на разных платформах (где скролл = 1 или = 120).
            _targetDistance = Mathf.Clamp(_targetDistance - Mathf.Sign(scroll) * _zoomStep, _minDistance, _maxDistance);
        }

        /// <summary>Плавно приблизить текущее расстояние к целевому (кадронезависимое сглаживание).</summary>
        public void UpdateZoomSmoothing(float dt)
        {
            _distance = Mathf.Lerp(_distance, _targetDistance, 1f - Mathf.Exp(-_zoomLerpSpeed * dt));
            if (Mathf.Abs(_distance - _targetDistance) < 0.001f) _distance = _targetDistance;
        }

        /// <summary>Поворот вокруг вертикали. input: +1 — по часовой, -1 — против.</summary>
        public void Rotate(float input, float dt)
        {
            _yaw += input * _rotateSpeed * dt;
        }

        // --- Сборка трансформа ---

        private void ApplyTransform()
        {
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = _pivot - (rot * Vector3.forward) * _distance;
            transform.rotation = rot;
        }

        private Vector3 YawForward()
        {
            float r = _yaw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        private Vector3 YawRight()
        {
            float r = _yaw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(r), 0f, -Mathf.Sin(r));
        }

        private Vector3 ClampToMap(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, _mapMin.x, _mapMax.x);
            p.z = Mathf.Clamp(p.z, _mapMin.y, _mapMax.y);
            p.y = 0f;
            return p;
        }

        // --- Чтение ввода (новый Input System) ---

        private Vector2 ReadMoveInput()
        {
            Vector2 move = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            }

            if (_edgeScrollEnabled && move == Vector2.zero)
                move += ReadEdgeScroll();

            return move;
        }

        private Vector2 ReadEdgeScroll()
        {
            // Скроллим краем только если окно в фокусе и курсор уже «ожил».
            // Иначе на старте позиция мыши = (0,0) и камера уезжает в угол.
            if (!Application.isFocused) return Vector2.zero;

            var mouse = Mouse.current;
            if (mouse == null) return Vector2.zero;

            if (mouse.middleButton.isPressed) return Vector2.zero; // во время СКМ-панорамы край не скроллит

            if (!_pointerActivated)
            {
                if (mouse.delta.ReadValue().sqrMagnitude > 0.01f) _pointerActivated = true;
                else return Vector2.zero;
            }

            Vector2 pos = mouse.position.ReadValue();
            if (pos.x < 0 || pos.y < 0 || pos.x > Screen.width || pos.y > Screen.height)
                return Vector2.zero;

            Vector2 edge = Vector2.zero;
            if (pos.x <= _edgeScrollPixels) edge.x -= 1f;
            else if (pos.x >= Screen.width - _edgeScrollPixels) edge.x += 1f;
            if (pos.y <= _edgeScrollPixels) edge.y -= 1f;
            else if (pos.y >= Screen.height - _edgeScrollPixels) edge.y += 1f;
            return edge;
        }

        private float ReadRotateInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return 0f;
            float r = 0f;
            if (kb.eKey.isPressed) r += 1f;
            if (kb.qKey.isPressed) r -= 1f;
            return r;
        }

        /// <summary>Поворот зажатым Alt + движением мыши (в дополнение к Q/E).</summary>
        private void RotateByMouse()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;
            if (!kb.leftAltKey.isPressed && !kb.rightAltKey.isPressed) return;

            float dx = mouse.delta.ReadValue().x; // пиксели за кадр
            _yaw += dx * _mouseRotateSensitivity;
        }

        /// <summary>СКМ-панорама: зажал — ведёшь мышь — камера ускоряется в ту сторону.</summary>
        private void MiddleMousePan(float dt)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.middleButton.wasPressedThisFrame)
            {
                _mmbAnchor = mouse.position.ReadValue();
                _mmbActive = true;
            }
            if (!mouse.middleButton.isPressed)
            {
                _mmbActive = false;
                return;
            }
            if (!_mmbActive) return;

            Vector2 offset = mouse.position.ReadValue() - _mmbAnchor;
            if (offset.magnitude < _mmbDeadZonePixels) return;

            PanByScreenOffset(offset, dt);
        }

        private float ReadZoomInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return 0f;
            return mouse.scroll.ReadValue().y; // сырое значение; шаг берём по знаку в Zoom()
        }
    }
}
