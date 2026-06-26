using System.Collections;
using NUnit.Framework;
using Planet.Game;
using Planet.Presentation;
using Planet.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Planet.Tests.PlayMode
{
    /// <summary>
    /// Интеграционные тесты потока сцен (Фаза 1). Требуют, чтобы MainMenu и Polygon
    /// были добавлены в Build Settings (см. EditorBuildSettings.asset).
    /// </summary>
    public sealed class SceneFlowTests
    {
        [UnityTest]
        public IEnumerator SinglePlayerButton_LoadsPolygon()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
            yield return null;
            yield return null;

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "В сцене меню должен быть MainMenuController.");

            menu.OnSinglePlayer();
            yield return null;
            yield return null;

            Assert.AreEqual(SceneNames.Polygon, SceneManager.GetActiveScene().name,
                "«Одиночная игра» должна загрузить сцену полигона.");
        }

        [UnityTest]
        public IEnumerator Polygon_BootsDeterministicSimulation()
        {
            SceneManager.LoadScene(SceneNames.Polygon);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimRunner>();
            Assert.IsNotNull(runner, "Сцена полигона должна поднять SimRunner.");
            Assert.IsNotNull(runner.World, "Мир симуляции должен быть инициализирован.");
            Assert.Greater(runner.World.Entities.Count, 0, "На полигоне должны заспавниться юниты.");

            int startTick = runner.World.CurrentTick;
            float t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; yield return null; }

            Assert.Greater(runner.World.CurrentTick, startTick, "Симуляция должна продвигаться по тикам.");
        }
    }
}
