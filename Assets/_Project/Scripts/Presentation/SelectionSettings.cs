using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Настройки выделения юнитов (рамка, пороги клика).</summary>
    [CreateAssetMenu(menuName = "Planet/Settings/Selection", fileName = "SelectionSettings")]
    public sealed class SelectionSettings : ScriptableObject
    {
        public Color BoxColor = new Color(0.30f, 1f, 0.40f, 1f);
        public float BoxThickness = 2f;
        [Range(0f, 1f)] public float BoxFillAlpha = 0.12f; // полупрозрачная заливка области выделения
        [Tooltip("Радиус клика по юниту в пикселях.")]
        public float ClickPixelThreshold = 35f;
        [Tooltip("Сдвиг курсора (px), с которого ЛКМ считается рамкой, а не кликом.")]
        public float DragStartThreshold = 8f;
        public float DoubleClickTime = 0.3f;
    }
}
