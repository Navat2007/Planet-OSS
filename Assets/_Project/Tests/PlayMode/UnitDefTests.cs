using NUnit.Framework;
using Planet.Game;
using Planet.Sim;
using UnityEngine;

namespace Planet.Tests.PlayMode
{
    public sealed class UnitDefTests
    {
        [Test]
        public void UnitDef_ConvertsMetersToSimUnits()
        {
            var def = ScriptableObject.CreateInstance<UnitDef>();
            def.MoveSpeed = 3f;   // м/с
            def.AttackRange = 8f; // м

            // 3 м/с * 1000 мм / 20 тиков = 150 мм/тик
            Assert.AreEqual(3 * SimConstants.UnitsPerMeter / SimConstants.TicksPerSecond, def.SpeedPerTick);
            Assert.AreEqual(8 * SimConstants.UnitsPerMeter, def.AttackRangeSim);

            Object.DestroyImmediate(def);
        }

        [Test]
        public void UnitDef_SpeedNeverZero()
        {
            var def = ScriptableObject.CreateInstance<UnitDef>();
            def.MoveSpeed = 0.001f; // округлилось бы в 0 — но минимум 1
            Assert.GreaterOrEqual(def.SpeedPerTick, 1);
            Object.DestroyImmediate(def);
        }
    }
}
