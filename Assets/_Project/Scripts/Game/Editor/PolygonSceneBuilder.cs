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
        private const string BoostCursorTexturePath = CursorFolder + "/CursorBoost2.png";
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string CursorSettingsPath = ResourcesFolder + "/CursorSettings.asset";
        private const string BoostCursorSettingsPath = ResourcesFolder + "/BoostCursorSettings.asset";
        private const string MenuCoverFolder = ResourcesFolder + "/UI";
        private const string MenuCoverPath = MenuCoverFolder + "/MainMenuCover.png";
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string CameraSettingsPath = SettingsFolder + "/CameraSettings.asset";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string CameraPrefabPath = PrefabsFolder + "/RTSCamera.prefab";

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

            SetupRtsLevel(scene, withGround: true); // полигон-песочница: плоская земля уместна
            EnsureCursorSettings();
            EnsureBoostCursorSettings();
            EnsureGameplaySettings();
            WireSettingsToBootstrap();

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
            // Землю-плоскость не создаём: реальные карты используют террейн, и плоскость
            // Ground с ним конфликтует. Настраиваем только свет, камеру, спавны и пр.
            SetupRtsLevel(scene, withGround: false);
            EnsureCursorSettings();
            EnsureBoostCursorSettings();
            EnsureGameplaySettings();
            WireSettingsToBootstrap();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Planet] Активная сцена настроена как RTS-уровень. Не забудьте сохранить (Ctrl+S).");
        }

        /// <summary>
        /// Добавить в сцену недостающие объекты RTS-уровня (идемпотентно).
        /// <paramref name="withGround"/> — создавать ли плоскую землю Ground (для полигона-песочницы);
        /// для реальных карт с террейном земля не нужна.
        /// </summary>
        private static void SetupRtsLevel(Scene scene, bool withGround)
        {
            EnsureSun();
            if (withGround) EnsureGround();
            EnsureCamera();
            EnsureEnvironmentRoot();
            EnsureSpawnPoints();
            EnsureGameRoot();
        }

        private static void EnsureSpawnPoints()
        {
            if (Object.FindFirstObjectByType<SpawnPoint>() != null) return;

            var parent = new GameObject("SpawnPoints");
            CreateSpawnPoint(parent.transform, "Spawn_Player", 0, new Vector3(0f, 0f, -15f));
            CreateSpawnPoint(parent.transform, "Spawn_Enemy", 1, new Vector3(0f, 0f, 15f));
        }

        private static void CreateSpawnPoint(Transform parent, string name, int ownerId, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<SpawnPoint>().OwnerId = ownerId;
        }

        [MenuItem("Planet/Setup/Setup Menu Cover")]
        public static void SetupMenuCover()
        {
            EnsureFolder(MenuCoverFolder);
            if (AssetImporter.GetAtPath(MenuCoverPath) is not TextureImporter importer)
            {
                Debug.LogWarning($"[Planet] Картинка обложки не найдена. Положи PNG в {MenuCoverPath} и повтори пункт меню.");
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (changed) importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MenuCoverPath);
            if (sprite != null) { Selection.activeObject = sprite; Debug.Log($"[Planet] Обложка меню готова: {MenuCoverPath}"); }
            else Debug.LogWarning("[Planet] Не удалось загрузить обложку как Sprite.");
        }

        [MenuItem("Planet/Setup/Create Cursor Settings")]
        public static void CreateCursorSettingsMenu()
        {
            EnsureCursorSettings();
            AssetDatabase.SaveAssets();
            var settings = AssetDatabase.LoadAssetAtPath<CursorSettings>(CursorSettingsPath);
            if (settings != null) Selection.activeObject = settings;
        }

        [MenuItem("Planet/Setup/Create Boost Cursor Settings")]
        public static void CreateBoostCursorSettingsMenu()
        {
            EnsureBoostCursorSettings();
            AssetDatabase.SaveAssets();
            var settings = AssetDatabase.LoadAssetAtPath<BoostCursorSettings>(BoostCursorSettingsPath);
            if (settings != null) Selection.activeObject = settings;
        }

        [MenuItem("Planet/Setup/Create Gameplay Settings")]
        public static void CreateGameplaySettingsMenu()
        {
            EnsureGameplaySettings();
            WireSettingsToBootstrap();
            AssetDatabase.SaveAssets();
            var sel = AssetDatabase.LoadAssetAtPath<SelectionSettings>($"{SettingsFolder}/SelectionSettings.asset");
            if (sel != null) Selection.activeObject = sel;
        }

        /// <summary>Создать (если нет) отдельные ассеты настроек презентации в Settings/.</summary>
        private static void EnsureGameplaySettings()
        {
            EnsureFolder(SettingsFolder);
            EnsureSettingsAsset<SelectionSettings>("SelectionSettings");
            EnsureSettingsAsset<HealthBarSettings>("HealthBarSettings");
            EnsureSettingsAsset<MoveMarkerSettings>("MoveMarkerSettings");
            EnsureSettingsAsset<RouteSettings>("RouteSettings");
            EnsureSettingsAsset<GhostSettings>("GhostSettings");
            EnsureSettingsAsset<OrderSettings>("OrderSettings");
        }

        private static void EnsureSettingsAsset<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{SettingsFolder}/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        /// <summary>Прописать ссылки на ассеты настроек в PolygonBootstrap открытой сцены.</summary>
        private static void WireSettingsToBootstrap()
        {
            var bootstrap = Object.FindFirstObjectByType<PolygonBootstrap>();
            if (bootstrap == null) return;

            var so = new SerializedObject(bootstrap);
            SetSettingsRef(so, "_selectionSettings", "SelectionSettings");
            SetSettingsRef(so, "_healthBarSettings", "HealthBarSettings");
            SetSettingsRef(so, "_markerSettings", "MoveMarkerSettings");
            SetSettingsRef(so, "_routeSettings", "RouteSettings");
            SetSettingsRef(so, "_ghostSettings", "GhostSettings");
            SetSettingsRef(so, "_orderSettings", "OrderSettings");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
        }

        private static void SetSettingsRef(SerializedObject so, string property, string assetName)
        {
            var prop = so.FindProperty(property);
            if (prop == null) return;
            prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{SettingsFolder}/{assetName}.asset");
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
        /// Создать (если нет) глобальный ассет boost-курсора в Resources. Применяется на старте
        /// во всех сценах (см. BoostCursorBootstrap). Текстура — готовый арт CursorBoost2.png.
        /// </summary>
        private static void EnsureBoostCursorSettings()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BoostCursorTexturePath);
            if (tex == null)
            {
                Debug.LogWarning($"[Planet] Текстура boost-курсора не найдена: {BoostCursorTexturePath}");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<BoostCursorSettings>(BoostCursorSettingsPath);
            if (settings == null)
            {
                EnsureFolder(ResourcesFolder);
                settings = ScriptableObject.CreateInstance<BoostCursorSettings>();
                settings.Texture = tex;
                AssetDatabase.CreateAsset(settings, BoostCursorSettingsPath);
            }
            else if (settings.Texture == null)
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
            var existing = GameObject.Find("Ground");
            if (existing != null)
            {
                // Земля есть — починить материал, если он отсутствует (например, после смены пайплайна).
                var r = existing.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial == null)
                    r.sharedMaterial = LoadOrCreateGroundMaterial();
                return;
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(MapExtent * 2f / 10f, 1f, MapExtent * 2f / 10f); // план 10м → 100м
            ground.GetComponent<Renderer>().sharedMaterial = LoadOrCreateGroundMaterial();
            ground.isStatic = true;
        }

        [MenuItem("Planet/Setup/Create Camera Prefab")]
        public static void CreateCameraPrefabMenu()
        {
            var prefab = EnsureCameraPrefab();
            AssetDatabase.SaveAssets();
            if (prefab != null) Selection.activeObject = prefab;
        }

        /// <summary>
        /// Поставить в сцену камеру-префаб (единую для всех уровней). Если в сцене уже есть «сырая»
        /// камера — мигрировать её на префаб, сохранив границы карты и стартовую точку.
        /// </summary>
        private static void EnsureCamera()
        {
            GameObject prefab = EnsureCameraPrefab();

            var existing = Object.FindFirstObjectByType<RtsCamera>();
            if (existing != null)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(existing.gameObject)) return; // уже инстанс префаба

                ReadPlacement(existing, out var mapMin, out var mapMax, out var startPivot, out var startYaw);
                Object.DestroyImmediate(existing.gameObject);
                var migrated = InstantiateCameraPrefab(prefab);
                WritePlacement(migrated.GetComponent<RtsCamera>(), mapMin, mapMax, startPivot, startYaw);
                return;
            }

            // Камеры нет — убираем «лишние» камеры (например, дефолтную Main Camera) и ставим префаб.
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(c.gameObject);

            var instance = InstantiateCameraPrefab(prefab);
            WritePlacement(instance.GetComponent<RtsCamera>(),
                new Vector2(-MapExtent, -MapExtent), new Vector2(MapExtent, MapExtent), Vector3.zero, 0f);
        }

        /// <summary>Создать (если нет) префаб камеры с привязанным ассетом CameraSettings.</summary>
        private static GameObject EnsureCameraPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            if (existing != null) return existing;

            EnsureFolder(PrefabsFolder);
            var go = new GameObject("RTSCamera");
            go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            var rts = go.AddComponent<RtsCamera>();
            AssignCameraSettings(rts);
            go.tag = "MainCamera";

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CameraPrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[Planet] Создан префаб камеры: {CameraPrefabPath}");
            return prefab;
        }

        private static void AssignCameraSettings(RtsCamera rts)
        {
            var settings = AssetDatabase.LoadAssetAtPath<CameraSettings>(CameraSettingsPath);
            if (settings == null) return;
            var so = new SerializedObject(rts);
            so.FindProperty("_settings").objectReferenceValue = settings;
            so.ApplyModifiedProperties();
        }

        private static GameObject InstantiateCameraPrefab(GameObject prefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "RTSCamera";
            return inst;
        }

        private static void ReadPlacement(RtsCamera rts, out Vector2 mapMin, out Vector2 mapMax,
            out Vector3 startPivot, out float startYaw)
        {
            var so = new SerializedObject(rts);
            mapMin = so.FindProperty("_mapMin").vector2Value;
            mapMax = so.FindProperty("_mapMax").vector2Value;
            startPivot = so.FindProperty("_startPivot").vector3Value;
            startYaw = so.FindProperty("_startYaw").floatValue;
        }

        private static void WritePlacement(RtsCamera rts, Vector2 mapMin, Vector2 mapMax,
            Vector3 startPivot, float startYaw)
        {
            var so = new SerializedObject(rts);
            so.FindProperty("_mapMin").vector2Value = mapMin;
            so.FindProperty("_mapMax").vector2Value = mapMax;
            so.FindProperty("_startPivot").vector3Value = startPivot;
            so.FindProperty("_startYaw").floatValue = startYaw;
            so.ApplyModifiedProperties();
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
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            bool hdrp = rp != null && rp.GetType().Name.Contains("HDRenderPipeline");
            Shader shader = hdrp ? Shader.Find("HDRP/Lit") : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
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
