using Planet.Presentation;
using Planet.Sim;
using UnityEngine;

namespace Planet.Game
{
    public enum DebugUnitKind { Infantry, Light, Heavy }

    /// <summary>
    /// Спавнит юнитов из UnitDef в позицию точки спавна владельца, подбирая свободное место
    /// рядом (по размеру юнита). Создаёт sim-сущность + визуал + тинт стороны.
    /// </summary>
    public sealed class UnitSpawner : MonoBehaviour
    {
        private static readonly Color BlueTint = new Color(0.60f, 0.70f, 1.00f);
        private static readonly Color RedTint = new Color(1.00f, 0.62f, 0.55f);

        private SimRunner _runner;
        private UnitDef _infantry;
        private UnitDef _light;
        private UnitDef _heavy;

        public void Init(SimRunner runner)
        {
            _runner = runner;
            _infantry = Resources.Load<UnitDef>("Units/Soldier");
            _light = Resources.Load<UnitDef>("Units/APC");
            _heavy = Resources.Load<UnitDef>("Units/Tank");
        }

        public UnitDef DefFor(DebugUnitKind kind) => kind switch
        {
            DebugUnitKind.Infantry => _infantry,
            DebugUnitKind.Light => _light,
            DebugUnitKind.Heavy => _heavy,
            _ => null
        };

        public void SpawnMany(DebugUnitKind kind, int ownerId, int count)
        {
            UnitDef def = DefFor(kind);
            if (def == null || _runner == null || _runner.World == null) return;

            SimVector2 center = GetSpawnCenter(ownerId);
            for (int i = 0; i < count; i++)
                SpawnOne(def, ownerId, center);
        }

        // Зазор между юнитами при спавне (мм) — чтобы стояли с расстоянием, не вплотную.
        private static readonly int SpawnGap = (int)(0.8f * SimConstants.UnitsPerMeter);

        private void SpawnOne(UnitDef def, int ownerId, SimVector2 center)
        {
            int radius = def.CollisionRadiusSim;
            SimVector2 pos = SimPlacement.FindFreeSpot(_runner.World, center, radius, SpawnGap);
            SimEntity e = _runner.World.Spawn(ownerId, pos, def.MaxHp, def.SpeedPerTick, def.AttackRangeSim, radius);
            e.ReverseDistance = def.ReverseDistanceSim;

            var root = new GameObject($"{def.DisplayName}_{ownerId}_{e.Id}");
            GameObject model = null;
            if (def.VisualPrefab != null)
            {
                model = Instantiate(def.VisualPrefab, root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * def.VisualScale;
            }

            var view = root.AddComponent<UnitView>();
            view.Model = model;
            view.TypeKey = def.DisplayName;
            view.SelectionRadius = def.SelectionRadius;
            view.Bind(e, y: 0f, yawOffset: def.VisualYawOffset);
            view.ApplyTint(ownerId == 0 ? BlueTint : RedTint);
        }

        /// <summary>Дебаг: уменьшить ХП всех живых юнитов на percent% (для проверки полос ХП).</summary>
        public void DamageAll(int percent)
        {
            if (_runner == null || _runner.World == null) return;
            foreach (var e in _runner.World.Entities)
            {
                if (!e.Alive) continue;
                e.Hp = Mathf.Max(1, e.Hp - e.MaxHp * percent / 100);
            }
        }

        private SimVector2 GetSpawnCenter(int ownerId)
        {
            foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                if (sp.OwnerId == ownerId)
                    return SimConvert.ToSim(sp.transform.position);

            // Запасной вариант, если точки спавна не расставлены.
            return SimVector2.FromMeters(0, ownerId == 0 ? -15 : 15);
        }
    }
}
