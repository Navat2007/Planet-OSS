using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Настройки «ощущения» RTS-камеры — общий пресет для всех карт.
    /// Границы карты и стартовая точка задаются на самом объекте камеры (они свои у уровня).
    /// </summary>
    [CreateAssetMenu(menuName = "Planet/Camera Settings", fileName = "CameraSettings")]
    public sealed class CameraSettings : ScriptableObject
    {
        [Header("Зум")]
        public float StartDistance = 32f;
        public float MinDistance = 12f;
        public float MaxDistance = 55f;
        public float ZoomStep = 4f;
        public float ZoomLerpSpeed = 10f;

        [Header("Панорама")]
        public float PanSpeed = 30f;
        public int EdgeScrollPixels = 8;
        public bool EdgeScrollEnabled = true;
        public float MmbPanAcceleration = 0.08f;
        public float MmbDeadZonePixels = 6f;

        [Header("Поворот / наклон")]
        public float RotateSpeed = 90f;
        public float MouseRotateSensitivity = 0.2f;

        [Tooltip("Наклон камеры вниз, градусы. 0 — горизонт, 90 — строго сверху.")]
        public float Pitch = 60f;
    }
}
