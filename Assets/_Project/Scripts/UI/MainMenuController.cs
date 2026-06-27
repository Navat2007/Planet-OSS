using Planet.Game;
using UnityEngine;

namespace Planet.UI
{
    /// <summary>
    /// Контроллер главного меню (Фаза 1). Тонкий: только обработка нажатий кнопок.
    /// Сама разметка меню — реальные объекты сцены (собираются меню Planet → Setup → Build Main Menu).
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        /// <summary>«Одиночная игра» — используется и кнопкой (onClick), и PlayMode-тестом.</summary>
        public void OnSinglePlayer() => SceneLoader.LoadPolygon();

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
