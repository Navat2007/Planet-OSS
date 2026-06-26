using Planet.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Planet.UI
{
    /// <summary>
    /// Главное меню (Фаза 1). UI строится в рантайме, чтобы не держать хрупкую UI-разметку в .unity.
    /// Активна только «Одиночная игра» → загрузка полигона. Остальное — заглушки.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            EnsureEventSystem();
            EnsureCamera();
            BuildMenu();
        }

        /// <summary>Публичный вход для «Одиночной игры» — используется и кнопкой, и PlayMode-тестом.</summary>
        public void OnSinglePlayer() => SceneLoader.LoadPolygon();

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>(); // новый Input System
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("UICamera");
            var cam = go.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.13f);
        }

        private void BuildMenu()
        {
            var canvasGo = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            CreateLabel(canvasGo.transform, "PLANET OSS", new Vector2(0, 320), 72);
            CreateLabel(canvasGo.transform, "Eternal Sun — prototype", new Vector2(0, 250), 28);

            CreateButton(canvasGo.transform, "Одиночная игра", new Vector2(0, 120), true, OnSinglePlayer);
            CreateButton(canvasGo.transform, "Сетевая игра", new Vector2(0, 40), false, null);
            CreateButton(canvasGo.transform, "Настройки", new Vector2(0, -40), false, null);
            CreateButton(canvasGo.transform, "Выход", new Vector2(0, -120), true, OnQuit);
        }

        private static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static void CreateLabel(Transform parent, string text, Vector2 anchoredPos, int fontSize)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(900, 100);
            rt.anchoredPosition = anchoredPos;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = UiFont;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private static void CreateButton(Transform parent, string text, Vector2 anchoredPos, bool interactable,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(420, 64);
            rt.anchoredPosition = anchoredPos;

            var img = go.GetComponent<Image>();
            img.color = interactable ? new Color(0.20f, 0.35f, 0.55f) : new Color(0.22f, 0.24f, 0.27f);

            var button = go.GetComponent<Button>();
            button.interactable = interactable;
            if (onClick != null) button.onClick.AddListener(onClick);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var label = textGo.AddComponent<Text>();
            label.text = text;
            label.font = UiFont;
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
