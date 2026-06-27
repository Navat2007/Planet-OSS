using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Настройки призрака направления (facing-превью моделями юнитов).</summary>
    [CreateAssetMenu(menuName = "Planet/Settings/Ghost", fileName = "GhostSettings")]
    public sealed class GhostSettings : ScriptableObject
    {
        public Color Color = new Color(0.45f, 0.85f, 1f, 0.45f);
    }
}
