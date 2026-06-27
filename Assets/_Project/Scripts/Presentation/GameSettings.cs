using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Точка доступа к настройкам презентации из рантайма. Ассеты лежат в
    /// <c>Assets/_Project/Settings</c> и подключаются ссылками в сцене (см. PolygonBootstrap),
    /// который вызывает <see cref="Configure"/>. Если что-то не подключено — берутся
    /// значения по умолчанию (CreateInstance), чтобы ничего не падало (тесты/пустая сцена).
    /// </summary>
    public static class GameSettings
    {
        private static SelectionSettings _selection;
        private static HealthBarSettings _healthBar;
        private static MoveMarkerSettings _marker;
        private static RouteSettings _route;
        private static GhostSettings _ghost;
        private static OrderSettings _order;

        public static SelectionSettings Selection => _selection != null ? _selection : (_selection = ScriptableObject.CreateInstance<SelectionSettings>());
        public static HealthBarSettings HealthBar => _healthBar != null ? _healthBar : (_healthBar = ScriptableObject.CreateInstance<HealthBarSettings>());
        public static MoveMarkerSettings Marker => _marker != null ? _marker : (_marker = ScriptableObject.CreateInstance<MoveMarkerSettings>());
        public static RouteSettings Route => _route != null ? _route : (_route = ScriptableObject.CreateInstance<RouteSettings>());
        public static GhostSettings Ghost => _ghost != null ? _ghost : (_ghost = ScriptableObject.CreateInstance<GhostSettings>());
        public static OrderSettings Order => _order != null ? _order : (_order = ScriptableObject.CreateInstance<OrderSettings>());

        /// <summary>Подключить ассеты настроек (null-аргументы игнорируются — остаётся прежнее/дефолт).</summary>
        public static void Configure(
            SelectionSettings selection,
            HealthBarSettings healthBar,
            MoveMarkerSettings marker,
            RouteSettings route,
            GhostSettings ghost,
            OrderSettings order)
        {
            if (selection != null) _selection = selection;
            if (healthBar != null) _healthBar = healthBar;
            if (marker != null) _marker = marker;
            if (route != null) _route = route;
            if (ghost != null) _ghost = ghost;
            if (order != null) _order = order;
        }
    }
}
