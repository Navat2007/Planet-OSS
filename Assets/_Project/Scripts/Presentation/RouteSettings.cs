using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Настройки отрисовки маршрута выделенных юнитов (линия и флажки).</summary>
    [CreateAssetMenu(menuName = "Planet/Settings/Route", fileName = "RouteSettings")]
    public sealed class RouteSettings : ScriptableObject
    {
        public Color Color = new Color(0.30f, 0.95f, 0.40f, 1f);
        public float LineWidth = 0.09f;
        [Range(0f, 1f)] public float LineAlpha = 0.5f;
        [Range(0f, 1f)] public float FlagAlpha = 0.55f;
        [Tooltip("Длина одного цикла «штрих+пробел» пунктира, м.")]
        public float DashLength = 0.9f;
        [Tooltip("Сколько секунд после приказа не срезать точки маршрута (лаг исполнения команды).")]
        public float FreshGrace = 0.2f;
    }
}
