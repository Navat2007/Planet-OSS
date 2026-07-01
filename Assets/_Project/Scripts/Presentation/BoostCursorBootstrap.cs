using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Создаёт глобальный boost-курсор один раз на старте приложения (переживает смену сцен),
    /// так же как <see cref="CursorBootstrap"/> — так СКМ-ускорение камеры работает в любой
    /// игровой сцене без ручного размещения BoostCursor. Настройки берутся из
    /// Resources/BoostCursorSettings.
    /// </summary>
    public static class BoostCursorBootstrap
    {
        public const string ResourcePath = "BoostCursorSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnStartup()
        {
            if (BoostCursor.Instance != null) return; // уже создан (например, повторный вызов после hot reload)

            var settings = Resources.Load<BoostCursorSettings>(ResourcePath);
            if (settings == null || settings.Texture == null) return;

            var go = new GameObject("BoostCursor (Global)");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<BoostCursor>().Configure(settings.Texture, settings.Size);
        }
    }
}
