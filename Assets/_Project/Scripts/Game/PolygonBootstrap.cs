using Planet.Presentation;
using Planet.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet.Game
{
    /// <summary>
    /// Геймплейный bootstrap матча (Фаза 1–3). Отвечает ТОЛЬКО за симуляцию и спавн юнитов.
    /// Окружение (земля, свет, камера) — реальные объекты сцены (см. Editor: Planet → Setup),
    /// их редактируют вручную, а не создают кодом.
    ///
    /// Юниты пока плейсхолдер-кубы; полноценные данные/префабы появятся в Фазе 3.
    /// </summary>
    public sealed class PolygonBootstrap : MonoBehaviour
    {
        [Header("Симуляция")]
        [SerializeField] private uint _seed = 12345;
        [SerializeField] private int _unitsPerSide = 4;

        private SimRunner _runner;

        private void Start()
        {
            _runner = gameObject.AddComponent<SimRunner>();
            _runner.Initialize(_seed);
            SpawnPlaceholderArmies();
        }

        private void Update()
        {
            // Проект в режиме Input System (New), legacy-класс Input не используем.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SceneLoader.LoadMainMenu();
        }

        private void SpawnPlaceholderArmies()
        {
            // Две «армии» друг напротив друга. Owner 0 (синие) и Owner 1 (красные).
            SpawnSide(ownerId: 0, baseZMeters: -15, color: new Color(0.2f, 0.4f, 0.9f), moveToZMeters: 0);
            SpawnSide(ownerId: 1, baseZMeters: 15, color: new Color(0.9f, 0.25f, 0.2f), moveToZMeters: 0);
        }

        private void SpawnSide(int ownerId, int baseZMeters, Color color, int moveToZMeters)
        {
            int speed = 3 * SimConstants.UnitsPerMeter / SimConstants.TicksPerSecond; // 3 м/с
            int attackRange = 8 * SimConstants.UnitsPerMeter; // 8 м — пока остановка на дистанции «выстрела»
            for (int i = 0; i < _unitsPerSide; i++)
            {
                int xMeters = (i - _unitsPerSide / 2) * 3;
                SimVector2 pos = SimVector2.FromMeters(xMeters, baseZMeters);
                SimEntity e = _runner.World.Spawn(ownerId, pos, hp: 100, speedPerTick: speed, attackRange: attackRange);

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Unit_{ownerId}_{e.Id}";
                SetColor(cube.GetComponent<Renderer>(), color);
                var view = cube.AddComponent<UnitView>();
                view.Bind(e, y: 0.5f);

                // Демонстрация детерминированной команды: через 1 сек двинуть к центру.
                var move = new MoveCommand(ownerId, new[] { e.Id }, SimVector2.FromMeters(xMeters, moveToZMeters));
                _runner.Schedule.Add(SimConstants.TicksPerSecond, move);
            }
        }

        /// <summary>Задать цвет с учётом URP (свойство _BaseColor) и встроенного конвейера (_Color).</summary>
        private static void SetColor(Renderer renderer, Color color)
        {
            var mat = renderer.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }
    }
}
