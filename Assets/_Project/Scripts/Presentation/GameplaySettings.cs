using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Единые настройки презентации: выделение, полосы ХП, маркер приказа, маршрут,
    /// призрак направления, приказы/рисование маршрута. Один ассет в
    /// <c>Resources/GameplaySettings</c> — всё настраивается в одном месте.
    /// Если ассета нет — берутся значения по умолчанию (см. <see cref="Instance"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Planet/Gameplay Settings", fileName = "GameplaySettings")]
    public sealed class GameplaySettings : ScriptableObject
    {
        public const string ResourcePath = "GameplaySettings";

        [Header("Выделение")]
        public Color SelectionBoxColor = new Color(0.30f, 1f, 0.40f, 1f);
        public float SelectionBoxThickness = 2f;
        [Tooltip("Радиус клика по юниту в пикселях.")]
        public float ClickPixelThreshold = 35f;
        [Tooltip("Сдвиг курсора (px), с которого ЛКМ считается рамкой, а не кликом.")]
        public float DragStartThreshold = 8f;
        public float DoubleClickTime = 0.3f;

        [Header("Полоса здоровья")]
        public Color HpColorFull = new Color(0.20f, 1f, 0.08f, 1f);
        public Color HpColorMid = new Color(1f, 0.82f, 0.05f, 1f);
        public Color HpColorLow = new Color(1f, 0.08f, 0.04f, 1f);
        public Color HpBackground = new Color(0f, 0f, 0f, 1f);
        public Color HpOutline = new Color(1f, 1f, 1f, 1f);
        [Range(0f, 1f)] public float HpMidThreshold = 0.55f; // ниже — жёлтый
        [Range(0f, 1f)] public float HpLowThreshold = 0.25f; // ниже — красный

        [Header("Маркер приказа")]
        public Color MarkerColor = new Color(0.30f, 1f, 0.40f, 1f);
        public float MarkerStartSize = 1.4f;
        public float MarkerLife = 0.5f;

        [Header("Маршрут (линия и флажки)")]
        public Color RouteColor = new Color(0.30f, 0.95f, 0.40f, 1f);
        public float RouteLineWidth = 0.09f;
        [Range(0f, 1f)] public float RouteLineAlpha = 0.5f;
        [Range(0f, 1f)] public float RouteFlagAlpha = 0.55f;
        [Tooltip("Длина одного цикла «штрих+пробел» пунктира, м.")]
        public float RouteDashLength = 0.9f;
        [Tooltip("Сколько секунд после приказа не срезать точки маршрута (лаг исполнения команды).")]
        public float RouteFreshGrace = 0.2f;

        [Header("Призрак направления (facing)")]
        public Color GhostColor = new Color(0.45f, 0.85f, 1f, 0.45f);

        [Header("Приказы и рисование маршрута")]
        [Tooltip("Сдвиг курсора (м), с которого зажатая ПКМ задаёт направление (facing).")]
        public float FacingDragMeters = 1.2f;
        [Tooltip("Как часто ставить точку при удержании Shift+ПКМ, сек.")]
        public float PaintInterval = 0.1f;
        [Tooltip("Мин. смещение курсора (м), чтобы поставить новую точку маршрута.")]
        public float PaintMinDistance = 1.0f;

        private static GameplaySettings _instance;

        /// <summary>Единственный экземпляр: грузится из Resources, иначе — значения по умолчанию.</summary>
        public static GameplaySettings Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Resources.Load<GameplaySettings>(ResourcePath);
                if (_instance == null)
                {
                    _instance = CreateInstance<GameplaySettings>();
                    Debug.LogWarning($"[Planet] Ассет Resources/{ResourcePath} не найден — используются значения по умолчанию.");
                }
                return _instance;
            }
        }
    }
}
