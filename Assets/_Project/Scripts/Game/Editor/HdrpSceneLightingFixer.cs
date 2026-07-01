using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Planet.Game.Editor
{
    /// <summary>
    /// Доводит освещение активной сцены до HDRP. После конвертации материалов свет и
    /// камеры часто остаются с URP-компонентами (UniversalAdditionalLightData/CameraData),
    /// которые HDRP не понимает: солнце читается как ~1 люкс, авто-экспозиция задирает
    /// яркость и сцена «засвечивается». Утилита снимает URP-компоненты, ставит HDRP-данные
    /// света/камеры с физической интенсивностью и добавляет Global Volume с Fixed Exposure
    /// и физическим небом.
    ///
    /// Значения освещения — рабочая отправная точка; финальную яркость/экспозицию удобно
    /// докрутить в инспекторе солнца и Volume.
    /// </summary>
    public static class HdrpSceneLightingFixer
    {
        // Солнечный день: ~100k люкс на солнце и Fixed Exposure ~14 EV100 дают нормальную
        // освещённость без пересвета. Точные числа подбираются под арт.
        private const float SunLux = 100000f;
        private const float FixedExposureEV = 14f;

        [MenuItem("Planet/Setup/Fix Active Scene Lighting (HDRP)")]
        public static void FixActiveScene()
        {
            int urpRemoved = 0, lights = 0, cameras = 0;

            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                urpRemoved += RemoveUrpAdditionalData(light.gameObject);

                // HDRP требует свой компонент данных света; интенсивность в Unity 6 задаётся
                // через нативный Light в физических единицах (для солнца — люксы).
                if (light.GetComponent<HDAdditionalLightData>() == null)
                    light.gameObject.AddComponent<HDAdditionalLightData>();
                if (light.type == LightType.Directional)
                {
                    light.lightUnit = LightUnit.Lux;
                    light.intensity = SunLux;
                }
                EditorUtility.SetDirty(light);
                lights++;
            }

            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                urpRemoved += RemoveUrpAdditionalData(cam.gameObject);
                if (cam.GetComponent<HDAdditionalCameraData>() == null)
                    cam.gameObject.AddComponent<HDAdditionalCameraData>();
                cameras++;
            }

            EnsureGlobalVolume();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Planet] HDRP-освещение сцены '{scene.name}': света {lights}, камер {cameras}, " +
                      $"снято URP-компонентов {urpRemoved}. Не забудьте сохранить (Ctrl+S).");
        }

        /// <summary>Удалить URP-компоненты доп-данных (света/камеры) по имени типа — без ссылки на URP.</summary>
        private static int RemoveUrpAdditionalData(GameObject go)
        {
            int removed = 0;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n == "UniversalAdditionalLightData" || n == "UniversalAdditionalCameraData")
                {
                    UnityEngine.Object.DestroyImmediate(c, true);
                    removed++;
                }
            }
            return removed;
        }

        private static void EnsureGlobalVolume()
        {
            Volume global = null;
            foreach (var v in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (v.isGlobal) { global = v; break; }

            if (global == null)
            {
                var go = new GameObject("Global Volume");
                global = go.AddComponent<Volume>();
                global.isGlobal = true;
            }

            VolumeProfile profile = global.sharedProfile;
            if (profile == null)
            {
                const string dir = "Assets/_Project/Settings";
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/_Project", "Settings");
                string sceneName = SceneManager.GetActiveScene().name;
                string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{sceneName}_VolumeProfile.asset");
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
                global.sharedProfile = profile;
            }

            // Fixed Exposure — убирает авто-задирание яркости (главную причину засветки).
            var exposure = GetOrAddOverride<Exposure>(profile);
            exposure.active = true;
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(FixedExposureEV);

            // Физическое небо как источник неба и ambient (иначе фон/окружение «никакие»).
            var visualEnv = GetOrAddOverride<VisualEnvironment>(profile);
            var sky = GetOrAddOverride<PhysicallyBasedSky>(profile);
            sky.active = true;
            visualEnv.skyType.Override((int)SkyType.PhysicallyBased);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Вернуть override нужного типа из профиля, добавив его при отсутствии. Важно:
        /// компоненты Volume — это ScriptableObject-подассеты профиля, поэтому новый
        /// компонент нужно ещё и зарегистрировать через AddObjectToAsset, иначе при
        /// сохранении профиль останется пустым (override не персистится).
        /// </summary>
        private static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out T existing)) return existing;
            T comp = profile.Add<T>(true);
            comp.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            AssetDatabase.AddObjectToAsset(comp, profile);
            return comp;
        }
    }
}
