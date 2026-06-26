using Planet.Sim;
using UnityEngine;

namespace Planet.Game
{
    public enum UnitCategory { Infantry, LightVehicle, Tank, Aircraft, Building }

    /// <summary>
    /// Описание типа юнита — базовый набор параметров (Фаза 3). Сейчас фракционно-нейтрален;
    /// позже сгруппируем UnitDef'ы во фракции (FactionDef) и добавим стоимость/требования/апгрейды.
    /// Бой (урон/перезарядка) появится в Фазе 6 — поля заложены заранее.
    /// </summary>
    [CreateAssetMenu(menuName = "Planet/Unit Def", fileName = "UnitDef")]
    public sealed class UnitDef : ScriptableObject
    {
        [Header("Идентификация")]
        public string DisplayName = "Unit";
        public UnitCategory Category = UnitCategory.Infantry;

        [Header("Бой и движение")]
        public int MaxHp = 100;
        [Tooltip("Скорость, м/с")] public float MoveSpeed = 3f;
        [Tooltip("Дальность атаки, м")] public float AttackRange = 8f;
        [Tooltip("Урон за выстрел (Фаза 6)")] public int AttackDamage = 10;
        [Tooltip("Перезарядка, сек (Фаза 6)")] public float ReloadSeconds = 1f;
        [Tooltip("Радиус коллизии/расхождения, м")] public float CollisionRadius = 0.5f;
        [Tooltip("Радиус выделения/HP-бара, м")] public float SelectionRadius = 0.6f;

        [Header("Визуал")]
        public GameObject VisualPrefab;
        [Tooltip("Множитель масштаба модели, если меш импортирован крупнее/мельче.")]
        public float VisualScale = 1f;
        [Tooltip("Поправка разворота модели, градусы (если 'перёд' модели не +Z).")]
        public float VisualYawOffset = 0f;

        /// <summary>Скорость в единицах симуляции за тик (мм/тик).</summary>
        public int SpeedPerTick =>
            Mathf.Max(1, Mathf.RoundToInt(MoveSpeed * SimConstants.UnitsPerMeter / SimConstants.TicksPerSecond));

        /// <summary>Дальность атаки в единицах симуляции (мм).</summary>
        public int AttackRangeSim => Mathf.RoundToInt(AttackRange * SimConstants.UnitsPerMeter);
    }
}
