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
        private const string CursorFolder = ArtFolder + "/Cursor";
        private const string CursorTexturePath = CursorFolder + "/Cursor_small.png"; // 25x25 — годен для аппаратного курсора
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string CursorSettingsPath = ResourcesFolder + "/CursorSettings.asset";

        private static readonly Color GroundColor = new Color(0.30f, 0.55f, 0.25f);
        private const float MapExtent = 50f; // половина стороны карты, м

        // Контур курсора-стрелки (координаты от левого-верхнего угла, кончик в (0,0) = hotspot).
        private const int CursorTexSize = 32;
        private static readonly Vector2[] ArrowPolygon =
        {
            new Vector2(0, 0), new Vector2(0, 16), new Vector2(4, 12),
            new Vector2(7, 18), new Vector2(9, 17), new Vector2(6, 11), new Vector2(11, 11)
        };

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
            EnsureCursorSettings();

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
            EnsureCursorSettings();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Planet] Активная сцена настроена как RTS-уровень. Не забудьте сохранить (Ctrl+S).");
        }

        /// <summary>Добавить в сцену недостающие объекты RTS-уровня (идемпотентно).</summary>
        private static void SetupRtsLevel(Scene scene)
        {
            EnsureSun();
            EnsureGround();
            EnsureCamera();
            EnsureEnvironmentRoot();
            EnsureGameRoot();
        }

        [MenuItem("Planet/Setup/Create Cursor Settings")]
        public static void CreateCursorSettingsMenu()
        {
            EnsureCursorSettings();
            AssetDatabase.SaveAssets();
            var settings = AssetDatabase.LoadAssetAtPath<CursorSettings>(CursorSettingsPath);
            if (settings != null) Selection.activeObject = settings;
        }

        /// <summary>
        /// Создать (если нет) глобальный ассет курсора в Resources. Применяется на старте
        /// во всех сценах (см. CursorBootstrap). Если своего Cursor.png нет — генерирует стрелку.
        /// </summary>
        private static void EnsureCursorSettings()
        {
            EnsureCursorTexture();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CursorTexturePath);

            var settings = AssetDatabase.LoadAssetAtPath<CursorSettings>(CursorSettingsPath);
            if (settings == null)
            {
                EnsureFolder(ResourcesFolder);
                settings = ScriptableObject.CreateInstance<CursorSettings>();
                settings.Texture = tex;
                AssetDatabase.CreateAsset(settings, CursorSettingsPath);
            }
            else if (settings.Texture == null && tex != null)
            {
                settings.Texture = tex;
                EditorUtility.SetDirty(settings);
            }
        }

        /// <summary>
        /// Подготовить текстуру курсора. Если файл уже есть (твой PNG) — только чиним импорт (Texture Type = Cursor).
        /// Если нет — генерируем стрелку-заглушку.
        /// </summary>
        private static void EnsureCursorTexture()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(CursorTexturePath) != null)
            {
                EnsureCursorImportSettings(CursorTexturePath);
                return;
            }

            EnsureFolder(CursorFolder);
            int size = CursorTexSize;

            bool[,] fill = new bool[size, size]; // [x, y] в координатах от левого-верхнего угла
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    fill[x, y] = PointInPolygon(x + 0.5f, y + 0.5f, ArrowPolygon);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color c;
                    if (fill[x, y]) c = Color.white;
                    else if (HasFillNeighbor(fill, x, y, size)) c = Color.black; // авто-обводка
                    else c = new Color(0f, 0f, 0f, 0f);
                    tex.SetPixel(x, size - 1 - y, c); // origin текстуры — снизу, координаты — сверху
                }
            tex.Apply();

            System.IO.File.WriteAllBytes(CursorTexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(CursorTexturePath);
            EnsureCursorImportSettings(CursorTexturePath);
            Debug.Log($"[Planet] Сгенерирован курсор-стрелка: {CursorTexturePath}. Замените файл на свой PNG при желании.");
        }

        /// <summary>Выставить текстуре правильный импорт для курсора (если ещё не выставлен).</summary>
        private static void EnsureCursorImportSettings(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Cursor) { importer.textureType = TextureImporterType.Cursor; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static bool PointInPolygon(float px, float py, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > py) != (poly[j].y > py)) &&
                    (px < (poly[j].x - poly[i].x) * (py - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }

        private static bool HasFillNeighbor(bool[,] fill, int x, int y, int size)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size) continue;
                    if (fill[nx, ny]) return true;
                }
            return false;
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
