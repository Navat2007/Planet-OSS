using Planet.Presentation;
using Planet.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet.Game
{
    /// <summary>
    /// Геймплейный bootstrap матча (Фаза 1–3). Отвечает только за симуляцию и спавн юнитов
    /// из <see cref="UnitDef"/>. Окружение (земля, свет, камера) — реальные объекты сцены
    /// (см. Editor: Planet → Setup).
    ///
    /// Тестовые UnitDef'ы создаются меню Planet → Setup → Create Test Units и грузятся из Resources.
    /// </summary>
    public sealed class PolygonBootstrap : MonoBehaviour
    {
        [Header("Симуляция")]
        [SerializeField] private uint _seed = 12345;

        private SimRunner _runner;
        private UnitDef _infantry;
        private UnitDef _tank;
        private UnitDef _apc;

        private static readonly Color BlueTint = new Color(0.60f, 0.70f, 1.00f);
        private static readonly Color RedTint = new Color(1.00f, 0.62f, 0.55f);

        private void Start()
        {
            _runner = gameObject.AddComponent<SimRunner>();
            _runner.Initialize(_seed);

            _infantry = Resources.Load<UnitDef>("Units/Soldier");
            _tank = Resources.Load<UnitDef>("Units/Tank");
            _apc = Resources.Load<UnitDef>("Units/APC");

            if (_infantry == null && _tank == null && _apc == null)
            {
                Debug.LogWarning("[Planet] Тестовые юниты не найдены. Запусти меню Planet → Setup → Create Test Units.");
                return;
            }

            SpawnArmies();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SceneLoader.LoadMainMenu();
        }

        private void SpawnArmies()
        {
            SpawnSide(ownerId: 0, baseZMeters: -18, tint: BlueTint, moveToZMeters: 0);
            SpawnSide(ownerId: 1, baseZMeters: 18, tint: RedTint, moveToZMeters: 0);
        }

        private void SpawnSide(int ownerId, int baseZMeters, Color tint, int moveToZMeters)
        {
            int away = baseZMeters < 0 ? -1 : 1; // «назад» — от центра карты
            SpawnGroup(ownerId, _infantry, count: 6, zMeters: baseZMeters, spacing: 3, tint, moveToZMeters);
            SpawnGroup(ownerId, _tank, count: 2, zMeters: baseZMeters + away * 5, spacing: 6, tint, moveToZMeters);
            SpawnGroup(ownerId, _apc, count: 2, zMeters: baseZMeters + away * 9, spacing: 6, tint, moveToZMeters);
        }

        private void SpawnGroup(int ownerId, UnitDef def, int count, int zMeters, int spacing, Color tint, int moveToZMeters)
        {
            if (def == null) return;

            for (int i = 0; i < count; i++)
            {
                int xMeters = (i - count / 2) * spacing;
                SimVector2 pos = SimVector2.FromMeters(xMeters, zMeters);
                SimEntity e = _runner.World.Spawn(ownerId, pos, def.MaxHp, def.SpeedPerTick, def.AttackRangeSim);

                var root = new GameObject($"{def.DisplayName}_{ownerId}_{e.Id}");
                if (def.VisualPrefab != null)
                {
                    var model = Instantiate(def.VisualPrefab, root.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one * def.VisualScale;
                }

                var view = root.AddComponent<UnitView>();
                view.Bind(e, y: 0f, yawOffset: def.VisualYawOffset);
                view.ApplyTint(tint);

                // Демонстрация: двинуть к центру; на дистанции атаки остановятся (Фаза 2.5).
                var move = new MoveCommand(ownerId, new[] { e.Id }, SimVector2.FromMeters(xMeters, moveToZMeters));
                _runner.Schedule.Add(SimConstants.TicksPerSecond, move);
            }
        }
    }
}
