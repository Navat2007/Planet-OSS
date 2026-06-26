using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Planet.Game;
using UnityEngine;
using UnityEngine.TestTools;

namespace Planet.Tests.PlayMode
{
    /// <summary>
    /// Headless-проверка, что пехотинец реально анимируется (скелет двигается), а не висит в T-позе.
    /// Прогоняем Animator вручную (animator.Update) — детерминированно, без зависимости от рендера.
    /// Требует созданных юнитов (Planet → Setup → Create Test Units).
    /// </summary>
    public sealed class SoldierAnimationTests
    {
        private static readonly string[] BoneNames =
        {
            "Bip001 R Forearm", "Bip001 R UpperArm", "Bip001 Spine",
            "Bip001 R Thigh", "Bip001 L Thigh", "Bip001 Head"
        };

        [UnityTest]
        public IEnumerator Soldier_SkeletonAnimates_NotTPose()
        {
            var def = Resources.Load<UnitDef>("Units/Soldier");
            Assert.IsNotNull(def, "UnitDef 'Soldier' не найден. Запусти Planet → Setup → Create Test Units.");
            Assert.IsNotNull(def.VisualPrefab, "У Soldier не назначен VisualPrefab.");

            var go = Object.Instantiate(def.VisualPrefab);
            var animator = go.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "В префабе солдата нет Animator.");
            Assert.IsNotNull(animator.runtimeAnimatorController, "У Animator не назначен контроллер.");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var bones = new List<Transform>();
            foreach (var n in BoneNames)
            {
                var b = FindDeep(go.transform, n);
                if (b != null) bones.Add(b);
            }
            Assert.Greater(bones.Count, 0, "Кости скелета не найдены.");

            yield return null; // дать Animator забиндиться

            // В batch время не идёт, поэтому сэмплируем клип Run на двух фазах напрямую.
            int runHash = Animator.StringToHash("Run");

            animator.Play(runHash, 0, 0f);
            animator.Update(0f);
            var poseA = new Quaternion[bones.Count];
            for (int i = 0; i < bones.Count; i++) poseA[i] = bones[i].localRotation;

            animator.Play(runHash, 0, 0.5f);
            animator.Update(0f);

            float maxAngle = 0f;
            string moved = "";
            for (int i = 0; i < bones.Count; i++)
            {
                float a = Quaternion.Angle(poseA[i], bones[i].localRotation);
                if (a > maxAngle) { maxAngle = a; moved = bones[i].name; }
            }

            Assert.Greater(maxAngle, 1f, $"Скелет не анимируется (макс {maxAngle:F2}° на '{moved}') — T-поза.");
            Debug.Log($"[Planet][Test] Анимация играет: макс смещение кости {maxAngle:F1}° ('{moved}').");

            Object.DestroyImmediate(go);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static bool HasParam(Animator a, string name)
        {
            foreach (var p in a.parameters)
                if (p.name == name) return true;
            return false;
        }
    }
}
