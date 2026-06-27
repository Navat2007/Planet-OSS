using Planet.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Planet.Game.Editor
{
    /// <summary>
    /// Создаёт тестовые UnitDef'ы (Фаза 3) на скачанных моделях в Resources/Units и настраивает
    /// анимацию пехотинца (риг → AnimatorController idle/run → анимированный префаб). Идемпотентно.
    /// </summary>
    public static class UnitSetup
    {
        private const string UnitsFolder = "Assets/_Project/Resources/Units";
        private const string AnimFolder = "Assets/_Project/Animations";
        private const string UnitPrefabsFolder = "Assets/_Project/Prefabs/Units";

        private const string SoldierModel =
            "Assets/_Project/Models/TestFaction/ToonSoldiers_demo/models/ToonSoldier_demo.FBX";
        private const string TankModel =
            "Assets/_Project/Models/TestFaction/CartoonMilitaryModelPack/Prefebs/Tank_Prefebs/Tank_01_Prefeb.prefab";
        private const string ApcModel =
            "Assets/_Project/Models/TestFaction/CartoonMilitaryModelPack/Prefebs/APC_Prefebs/APC_01_Prefeb.prefab";

        private const string AnimDir = "Assets/_Project/Models/TestFaction/ToonSoldiers_demo/animation/";
        private const string IdleFbx = AnimDir + "assault_combat_idle.FBX";
        private const string RunFbx = AnimDir + "assault_combat_run.FBX";

        private const string SoldierControllerPath = AnimFolder + "/Soldier.controller";
        private const string SoldierVisualPrefabPath = UnitPrefabsFolder + "/Soldier_Visual.prefab";

        [MenuItem("Planet/Setup/Create Test Units")]
        public static void CreateTestUnits()
        {
            EnsureFolder(UnitsFolder);

            GameObject soldierVisual = SetupSoldierAnimated(); // риг + контроллер + анимированный префаб

            CreateOrUpdate("Soldier", UnitCategory.Infantry, hp: 80, speed: 3.5f, range: 9f,
                dmg: 8, reload: 0.8f, collision: 0.4f, selection: 0.6f, reverse: 1f, modelPath: SoldierModel,
                scale: 1f, visualOverride: soldierVisual);

            CreateOrUpdate("Tank", UnitCategory.Tank, hp: 320, speed: 2.2f, range: 14f,
                dmg: 30, reload: 2.0f, collision: 1.2f, selection: 1.6f, reverse: 9f, modelPath: TankModel, scale: 1f);

            CreateOrUpdate("APC", UnitCategory.LightVehicle, hp: 170, speed: 3.2f, range: 10f,
                dmg: 12, reload: 1.2f, collision: 1.0f, selection: 1.3f, reverse: 9f, modelPath: ApcModel, scale: 1f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Planet] Тестовые юниты созданы/обновлены в " + UnitsFolder);
        }

        // --- UnitDef ---

        private static void CreateOrUpdate(string name, UnitCategory category, int hp, float speed, float range,
            int dmg, float reload, float collision, float selection, float reverse, string modelPath, float scale,
            GameObject visualOverride = null)
        {
            string path = $"{UnitsFolder}/{name}.asset";
            var def = AssetDatabase.LoadAssetAtPath<UnitDef>(path);
            bool isNew = def == null;
            if (isNew) def = ScriptableObject.CreateInstance<UnitDef>();

            def.DisplayName = name;
            def.Category = category;
            def.MaxHp = hp;
            def.MoveSpeed = speed;
            def.AttackRange = range;
            def.AttackDamage = dmg;
            def.ReloadSeconds = reload;
            def.CollisionRadius = collision;
            def.SelectionRadius = selection;
            def.ReverseDistance = reverse;
            def.VisualScale = scale;

            var visual = visualOverride != null ? visualOverride : AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (visual != null) def.VisualPrefab = visual;
            else Debug.LogWarning($"[Planet] Визуал не найден для {name}: {modelPath}");

            if (isNew) AssetDatabase.CreateAsset(def, path);
            else EditorUtility.SetDirty(def);
        }

        // --- Анимация пехотинца ---

        [MenuItem("Planet/Setup/Rebuild Soldier Animation")]
        public static void RebuildSoldierAnimationMenu()
        {
            var prefab = SetupSoldierAnimated();
            if (prefab != null) Selection.activeObject = prefab;
        }

        private static GameObject SetupSoldierAnimated()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierModel);
            if (model == null)
            {
                Debug.LogWarning($"[Planet] Модель солдата не найдена: {SoldierModel}");
                return null;
            }

            // Generic без аватара: клип привязывается к костям по путям (скелет одинаковый,
            // Animator на корне, "Bip001" — прямой ребёнок → пути совпадают). Надёжно генерит клип.
            ConfigureModelRigGeneric(SoldierModel);
            ConfigureClipGeneric(IdleFbx);
            ConfigureClipGeneric(RunFbx);

            // Выносим клипы из FBX в отдельные .anim — стабильные ссылки (FBX-сабассеты «отваливаются»).
            AnimationClip idle = ExtractClip(IdleFbx, "Soldier_Idle");
            AnimationClip run = ExtractClip(RunFbx, "Soldier_Run");
            if (idle == null || run == null)
            {
                Debug.LogWarning("[Planet] Не найдены клипы idle/run — солдат будет без анимации.");
                return model;
            }

            AnimatorController controller = BuildController(idle, run);
            return BuildSoldierVisualPrefab(model, controller, null);
        }

        private static void ConfigureModelRigGeneric(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter imp) return;
            bool changed = false;
            if (imp.animationType != ModelImporterAnimationType.Generic) { imp.animationType = ModelImporterAnimationType.Generic; changed = true; }
            if (imp.avatarSetup != ModelImporterAvatarSetup.NoAvatar) { imp.avatarSetup = ModelImporterAvatarSetup.NoAvatar; changed = true; }
            if (changed) imp.SaveAndReimport();
        }

        private static void ConfigureClipGeneric(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter imp) return;
            bool changed = false;
            if (imp.animationType != ModelImporterAnimationType.Generic) { imp.animationType = ModelImporterAnimationType.Generic; changed = true; }
            if (imp.avatarSetup != ModelImporterAvatarSetup.NoAvatar) { imp.avatarSetup = ModelImporterAvatarSetup.NoAvatar; changed = true; }
            if (!imp.importAnimation) { imp.importAnimation = true; changed = true; }

            // Зацикливаем тейк.
            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            bool clipChanged = false;
            for (int i = 0; i < clips.Length; i++)
                if (!clips[i].loopTime) { clips[i].loopTime = true; clipChanged = true; }
            if (clipChanged) { imp.clipAnimations = clips; changed = true; }

            if (changed) imp.SaveAndReimport();
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath))
                if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                    return c;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                    return c;
            return null;
        }

        /// <summary>Скопировать клип из FBX в отдельный .anim-ассет (стабильная ссылка для контроллера).</summary>
        private static AnimationClip ExtractClip(string fbxPath, string outName)
        {
            var src = LoadClip(fbxPath);
            if (src == null) return null;

            EnsureFolder(AnimFolder);
            string outPath = $"{AnimFolder}/{outName}.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) != null)
                AssetDatabase.DeleteAsset(outPath);

            var copy = Object.Instantiate(src);
            copy.name = outName;
            AssetDatabase.CreateAsset(copy, outPath);
            return copy;
        }

        private static AnimatorController BuildController(AnimationClip idle, AnimationClip run)
        {
            EnsureFolder(AnimFolder);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SoldierControllerPath) != null)
                AssetDatabase.DeleteAsset(SoldierControllerPath);

            // С клипом — гарантированно создаётся базовый слой + дефолтное состояние (idle).
            var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(SoldierControllerPath, idle);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;
            var idleState = sm.defaultState; // авто-созданное состояние с клипом idle
            idleState.name = "Idle";

            var runState = sm.AddState("Run");
            runState.motion = run;

            var toRun = idleState.AddTransition(runState);
            toRun.hasExitTime = false;
            toRun.duration = 0.1f;
            toRun.AddCondition(AnimatorConditionMode.If, 0f, "Moving");

            var toIdle = runState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static GameObject BuildSoldierVisualPrefab(GameObject model, AnimatorController controller, Avatar avatar)
        {
            EnsureFolder(UnitPrefabsFolder);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);

            var animator = inst.GetComponent<Animator>();
            if (animator == null) animator = inst.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            if (avatar != null) animator.avatar = avatar;

            if (inst.GetComponent<UnitAnimator>() == null) inst.AddComponent<UnitAnimator>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, SoldierVisualPrefabPath);
            Object.DestroyImmediate(inst);
            return prefab;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
