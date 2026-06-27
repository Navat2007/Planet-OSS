using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Настройки приказов движения и рисования маршрута удержанием Shift+ПКМ.</summary>
    [CreateAssetMenu(menuName = "Planet/Settings/Order", fileName = "OrderSettings")]
    public sealed class OrderSettings : ScriptableObject
    {
        [Tooltip("Сдвиг курсора (м), с которого зажатая ПКМ задаёт направление (facing).")]
        public float FacingDragMeters = 1.2f;
        [Tooltip("Как часто ставить точку при удержании Shift+ПКМ, сек.")]
        public float PaintInterval = 0.1f;
        [Tooltip("Мин. смещение курсора (м), чтобы поставить новую точку маршрута.")]
        public float PaintMinDistance = 1.0f;
    }
}
