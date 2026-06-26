using System.Collections.Generic;
using Planet.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Planet.Game.Editor
{
    /// <summary>
    /// Инструменты сборки игровых сцен из РЕАЛЬНЫХ объектов (Фаза 2+). В отличие от
    /// рантайм-создания, объекты остаются в сцене и редактируются вручную:
    /// можно крутить параметры камеры в Инспекторе, ставить окружение под «Environment» и т.д.
    ///
    /// «Build Polygon Scene» — собрать/обновить тестовый полигон.
    /// «Setup Active Scene As RTS Level» — превратить любую открытую сцену в игровой уровень
    /// (заготовка под создание новых карт).
    /// </summary>
    public static class PolygonSceneBuilder
    {
        private const string PolygonScenePath = "Assets/_Project/Scenes/Polygon.unity";
        private const string ArtFolder = "Assets/_Project/Art";
        private const string GroundMaterialPath = ArtFolder + "/GroundMaterial.mat";
        private const string CursorFolder = ArtFolder + "/Cursors";
        private const string CursorTexturePath = CursorFolder + "/Cursor.png";

        private static readonly Color GroundColor = new Color(0.30f, 0.55f, 0.25f);
        private const float MapExtent = 50f; // половина стороны карты, м

        [MenuItem("Planet/Setup/Build Polygon Scene")]
        public static void BuildPolygonScene()
        {
            Scene scene;
            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(PolygonScenePath) != null;
            if (exists)
            {
                scene = EditorSceneManager.OpenScene(PolygonScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            SetupRtsLevel(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PolygonScenePath);
            EnsureInBuildSettings(PolygonScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Planet] Сцена полигона собрана: {PolygonScenePath}");
        }

        [MenuItem("Planet/Setup/Setup Active Scene As RTS Level")]
        public static void SetupActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            SetupRtsLevel(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Planet] Активная сцена настроена как RTS-уровень. Не забудьте сохранить (Ctrl+S).");
        }

        /// <summary>Добавить в сцену недостающие объекты RTS-уровня (идемпотентно).</summary>
        private static void SetupRtsLevel(Scene scene)
        {
            EnsureSun();
            EnsureGround();
            EnsureCamera();
            EnsureCursor();
            EnsureEnvironmentRoot();
            EnsureGameRoot();
        }

        private static void EnsureCursor()
        {
            if (Object.FindFirstObjectByType<CursorController>() != null) return;

            EnsureFolder(CursorFolder);
            var go = new GameObject("Cursor");
            var cc = go.AddComponent<CursorController>();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CursorTexturePath);
            if (tex != null)
            {
                var so = new SerializedObject(cc);
                so.FindProperty("_cursorTexture").objectReferenceValue = tex;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning(
                    $"[Planet] Текстура курсора не найдена. Положите PNG в {CursorTexturePath} " +
                    "(Inspector → Texture Type = Cursor), затем переназначьте поле Cursor Texture " +
                    "на объекте 'Cursor' или пересоберите сцену.");
            }
        }

        private static void EnsureSun()
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) return;

            var go = new GameObject("Sun");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            l.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureGround()
        {
            if (GameObject.Find("Ground") != null) return;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(MapExtent * 2f / 10f, 1f, MapExtent * 2f / 10f); // план 10м → 100м
            ground.GetComponent<Renderer>().sharedMaterial = LoadOrCreateGroundMaterial();
            ground.isStatic = true;
        }

        private static void EnsureCamera()
        {
            if (Object.FindFirstObjectByType<RtsCamera>() != null) return;

            // Переиспользуем существующую камеру (из дефолтной сцены), иначе создаём.
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

            GameObject go;
            if (cam != null)
            {
                go = cam.gameObject;
            }
            else
            {
                go = new GameObject("RTSCamera");
                go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            go.name = "RTSCamera";
            go.tag = "MainCamera";
            var rts = go.AddComponent<RtsCamera>();
            rts.Initialize(Vector3.zero, new Vector2(-MapExtent, -MapExtent), new Vector2(MapExtent, MapExtent));
        }

        private static void EnsureEnvironmentRoot()
        {
            if (GameObject.Find("Environment") == null)
                new GameObject("Environment"); // сюда вручную складывают пропсы/окружение уровня
        }

        private static void EnsureGameRoot()
        {
            if (Object.FindFirstObjectByType<PolygonBootstrap>() != null) return;
            var go = new GameObject("GameRoot");
            go.AddComponent<PolygonBootstrap>();
        }

        private static Material LoadOrCreateGroundMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (mat != null) return mat;

            EnsureFolder(ArtFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { name = "GroundMaterial" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", GroundColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", GroundColor);
            AssetDatabase.CreateAsset(mat, GroundMaterialPath);
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void EnsureInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == scenePath) return;

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
