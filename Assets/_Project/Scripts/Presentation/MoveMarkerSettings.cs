using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Настройки маркера точки приказа (кратковременный диск).</summary>
    [CreateAssetMenu(menuName = "Planet/Settings/Move Marker", fileName = "MoveMarkerSettings")]
    public sealed class MoveMarkerSettings : ScriptableObject
    {
        public Color Color = new Color(0.30f, 1f, 0.40f, 1f);
        public float StartSize = 1.4f;
        public float Life = 0.5f;
    }
}
