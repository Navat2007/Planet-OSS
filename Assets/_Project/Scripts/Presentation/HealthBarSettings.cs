using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Настройки полосы здоровья над юнитом (цвета и пороги).</summary>
    [CreateAssetMenu(menuName = "Planet/Settings/Health Bar", fileName = "HealthBarSettings")]
    public sealed class HealthBarSettings : ScriptableObject
    {
        public Color ColorFull = new Color(0.20f, 1f, 0.08f, 1f);
        public Color ColorMid = new Color(1f, 0.82f, 0.05f, 1f);
        public Color ColorLow = new Color(1f, 0.08f, 0.04f, 1f);
        public Color Background = new Color(0f, 0f, 0f, 1f);
        public Color Outline = new Color(1f, 1f, 1f, 1f);
        [Range(0f, 1f)] public float MidThreshold = 0.55f; // ниже — жёлтый
        [Range(0f, 1f)] public float LowThreshold = 0.25f; // ниже — красный
    }
}
