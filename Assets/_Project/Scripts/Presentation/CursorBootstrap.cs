using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Применяет кастомный курсор глобально на старте приложения, до загрузки первой сцены.
    /// Так курсор активен везде (меню + игра) без объектов в сценах.
    /// Настройки берутся из ассета Resources/CursorSettings.
    /// </summary>
    public static class CursorBootstrap
    {
        public const string ResourcePath = "CursorSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            var settings = Resources.Load<CursorSettings>(ResourcePath);
            if (settings != null)
                settings.Apply();
        }
    }
}
