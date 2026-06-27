using UnityEngine;

namespace Planet.Game
{
    /// <summary>
    /// Отладочная панель спавна (IMGUI). Кнопки спавна юнитов для игрока и противника.
    /// F1 — скрыть/показать.
    /// </summary>
    public sealed class DebugSpawnPanel : MonoBehaviour
    {
        private UnitSpawner _spawner;
        private bool _visible = true;

        public void Init(UnitSpawner spawner) => _spawner = spawner;

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _spawner == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 240, 520), GUI.skin.box);
            GUILayout.Label("DEBUG SPAWN (F1 — скрыть)");
            DrawOwner("Игрок (синие)", 0);
            GUILayout.Space(10);
            DrawOwner("Противник (красные)", 1);
            GUILayout.Space(10);
            if (GUILayout.Button("Ранить всех (−30% ХП)")) _spawner.DamageAll(30);
            GUILayout.EndArea();
        }

        private void DrawOwner(string title, int owner)
        {
            GUILayout.Label(title);

            GUILayout.Label("Пехота");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1")) _spawner.SpawnMany(DebugUnitKind.Infantry, owner, 1);
            if (GUILayout.Button("+5")) _spawner.SpawnMany(DebugUnitKind.Infantry, owner, 5);
            if (GUILayout.Button("+10")) _spawner.SpawnMany(DebugUnitKind.Infantry, owner, 10);
            GUILayout.EndHorizontal();

            GUILayout.Label("Лёгкая техника");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1")) _spawner.SpawnMany(DebugUnitKind.Light, owner, 1);
            if (GUILayout.Button("+3")) _spawner.SpawnMany(DebugUnitKind.Light, owner, 3);
            if (GUILayout.Button("+5")) _spawner.SpawnMany(DebugUnitKind.Light, owner, 5);
            GUILayout.EndHorizontal();

            GUILayout.Label("Тяжёлая техника");
            if (GUILayout.Button("+1")) _spawner.SpawnMany(DebugUnitKind.Heavy, owner, 1);
        }
    }
}
