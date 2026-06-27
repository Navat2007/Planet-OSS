using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Planet.UI.Editor
{
    /// <summary>
    /// Собирает сцену главного меню из РЕАЛЬНЫХ объектов (Canvas, кнопки, фон, EventSystem),
    /// которые редактируются мышкой. Идемпотентно: пересборка заменяет содержимое сцены.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string CoverPath = "Assets/_Project/Resources/UI/MainMenuCover.png";

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);

        [MenuItem("Planet/Setup/Build Main Menu")]
        public static void BuildMainMenu()
        {
            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            CreateCamera();
            CreateEventSystem();
            var controller = new GameObject("MenuController").AddComponent<MainMenuController>();
            BuildCanvas(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureInBuildSettings(ScenePath);
            Debug.Log("[Planet] Сцена главного меню собрана: " + ScenePath);
        }

        private static void CreateCamera()
        {
            var go = new GameObject("UICamera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.13f);
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void BuildCanvas(MainMenuController controller)
        {
            var canvasGo = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            EnsureCoverSprite();
            var cover = AssetDatabase.LoadAssetAtPath<Sprite>(CoverPath);
            if (cover != null)
            {
                CreateImage(canvasGo.transform, "Background", cover, Color.white);
                CreateImage(canvasGo.transform, "Veil", null, new Color(0f, 0f, 0f, 0.30f));
            }

            // Лого — левый верхний угол.
            CreateText(canvasGo.transform, "Title", "PLANET OSS", TopLeft,
                new Vector2(60, -50), new Vector2(900, 90), 80, TextAnchor.UpperLeft);
            CreateText(canvasGo.transform, "Subtitle", "OUTER SPACE SUPREMACY — prototype", TopLeft,
                new Vector2(64, -140), new Vector2(900, 40), 26, TextAnchor.UpperLeft);

            // Кнопки — левый нижний угол, столбиком.
            const float x = 60f, step = 80f, y0 = 70f;
            CreateButton(canvasGo.transform, "Btn_SinglePlayer", "Одиночная игра", BottomLeft,
                new Vector2(x, y0 + step * 3), true, controller.OnSinglePlayer);
            CreateButton(canvasGo.transform, "Btn_Network", "Сетевая игра", BottomLeft,
                new Vector2(x, y0 + step * 2), false, null);
            CreateButton(canvasGo.transform, "Btn_Settings", "Настройки", BottomLeft,
                new Vector2(x, y0 + step), false, null);
            CreateButton(canvasGo.transform, "Btn_Quit", "Выход", BottomLeft,
                new Vector2(x, y0), true, controller.OnQuit);
        }

        private static void EnsureCoverSprite()
        {
            if (!System.IO.File.Exists(CoverPath)) return;
            AssetDatabase.ImportAsset(CoverPath);
            if (AssetImporter.GetAtPath(CoverPath) is not TextureImporter imp) return;

            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
            // Single — иначе главный ассет не Sprite и LoadAssetAtPath<Sprite> вернёт null.
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (changed) imp.SaveAndReimport();
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = false;
            img.raycastTarget = false;
            return img;
        }

        private static void CreateText(Transform parent, string name, string text, Vector2 anchor,
            Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = UiFont;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = Color.white;

            var o = go.GetComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
        }

        private static void CreateButton(Transform parent, string name, string text, Vector2 anchor,
            Vector2 pos, bool interactable, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = new Vector2(360, 64);
            rt.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.color = interactable ? new Color(0.20f, 0.35f, 0.55f, 0.95f) : new Color(0.22f, 0.24f, 0.27f, 0.9f);

            var button = go.GetComponent<Button>();
            button.interactable = interactable;
            if (interactable && onClick != null)
                UnityEventTools.AddPersistentListener(button.onClick, onClick);

            var tgo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)tgo.transform);
            var t = tgo.GetComponent<Text>();
            t.text = text;
            t.font = UiFont;
            t.fontSize = 28;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static void EnsureInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == scenePath) return;
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
