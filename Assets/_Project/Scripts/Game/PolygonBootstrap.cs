using Planet.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet.Game
{
    /// <summary>
    /// Геймплейный bootstrap матча. Поднимает симуляцию и отладочный спавн.
    /// Стартовых армий нет — юниты спавнятся вручную через DebugSpawnPanel
    /// в точки спавна (SpawnPoint) игрока/противника.
    /// </summary>
    public sealed class PolygonBootstrap : MonoBehaviour
    {
        [Header("Симуляция")]
        [SerializeField] private uint _seed = 12345;

        private SimRunner _runner;

        private void Start()
        {
            _runner = gameObject.AddComponent<SimRunner>();
            _runner.Initialize(_seed);

            var spawner = gameObject.AddComponent<UnitSpawner>();
            spawner.Init(_runner);

            var panel = gameObject.AddComponent<DebugSpawnPanel>();
            panel.Init(spawner);

            gameObject.AddComponent<SelectionController>();

            CenterCameraOnPlayerSpawn();
        }

        /// <summary>Центрировать камеру на стартовой точке локального игрока (в сети — на своей).</summary>
        private void CenterCameraOnPlayerSpawn()
        {
            const int localOwner = 0;
            var cam = Camera.main;
            var rts = cam != null ? cam.GetComponent<RtsCamera>() : null;
            if (rts == null) return;

            foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                if (sp.OwnerId == localOwner)
                {
                    rts.CenterOn(sp.transform.position);
                    return;
                }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SceneLoader.LoadMainMenu();
        }
    }
}
