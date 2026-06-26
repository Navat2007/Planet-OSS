using UnityEngine.SceneManagement;

namespace Planet.Game
{
    /// <summary>Централизованная загрузка сцен. Единая точка для будущего экрана загрузки/сетевого старта.</summary>
    public static class SceneLoader
    {
        public static void LoadMainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);

        public static void LoadPolygon() => SceneManager.LoadScene(SceneNames.Polygon);
    }
}
