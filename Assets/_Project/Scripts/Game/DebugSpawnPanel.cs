using Planet.Presentation;
using UnityEngine;

namespace Planet.Game
{
    /// <summary>
    /// Отладочная панель спавна (IMGUI). Кнопки спавна юнитов для игрока и противника.
    /// F1 — скрыть/показать.
    /// </summary>
    public sealed class DebugSpawnPanel : MonoBehaviour
    {
        private static readonly Rect PanelRect = new Rect(10, 10, 240, 520);

        private UnitSpawner _spawner;
        private bool _visible = true;

        public void Init(UnitSpawner spawner) => _spawner = spawner;

        private bool BlocksScreenPoint(Vector2 screenPosition)
        {
            if (!_visible || _spawner == null)
                return false;

            Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return PanelRect.Contains(guiPosition);
        }

        private void OnEnable() => PointerInputBlockers.Register(BlocksScreenPoint);

        private void OnDisable() => PointerInputBlockers.Unregister(BlocksScreenPoint);

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _spawner == null) return;

            GUILayout.BeginArea(PanelRect, GUI.skin.box);
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
