using NUnit.Framework;
using Planet.Sim;

namespace Planet.Tests.EditMode
{
    /// <summary>
    /// ГЛАВНЫЙ СТРАЖ проекта. Если этот тест краснеет — где-то в симуляцию просочился
    /// недетерминизм (float/Random/Time/нестабильный порядок), и сетевой lockstep сломается.
    /// Его гоняем на каждой фазе.
    /// </summary>
    public sealed class DeterminismTests
    {
        /// <summary>Прогон одного и того же сценария: спавн двух армий + приказы движения.</summary>
        private static void RunScenario(SimWorld world)
        {
            // Спавним детерминированно.
            for (int i = 0; i < 8; i++)
            {
                int owner = i % 2;
                var pos = SimVector2.FromMeters(i - 4, owner == 0 ? -10 : 10);
                world.Spawn(owner, pos, hp: 100, speedPerTick: 150);
            }
        }

        private static ISimCommand[] CommandsForTick(int tick)
        {
            // Детерминированный поток команд, не зависящий от реального времени.
            if (tick == 2)
                return new ISimCommand[] { new MoveCommand(0, new[] { 1, 3, 5, 7 }, SimVector2.FromMeters(0, 0)) };
            if (tick == 4)
                return new ISimCommand[] { new MoveCommand(1, new[] { 2, 4, 6, 8 }, SimVector2.FromMeters(2, 0)) };
            return null;
        }

        [Test]
        public void TwoWorlds_SameSeedSameCommands_StayIdentical()
        {
            var a = new SimWorld(seed: 777);
            var b = new SimWorld(seed: 777);
            RunScenario(a);
            RunScenario(b);

            for (int tick = 0; tick < 300; tick++)
            {
                a.Tick(CommandsForTick(tick));
                b.Tick(CommandsForTick(tick));
                Assert.AreEqual(a.StateHash(), b.StateHash(),
                    $"Рассинхрон на тике {tick}: симуляция недетерминирована.");
            }
        }

        [Test]
        public void DifferentSeed_ProducesDifferentRngState()
        {
            // Санитарная проверка: сид реально влияет на ГПСЧ (защита от «сид игнорируется»).
            var a = new SimWorld(seed: 1);
            var b = new SimWorld(seed: 2);
            for (int i = 0; i < 10; i++) { a.Rng.NextUInt(); b.Rng.NextUInt(); }
            Assert.AreNotEqual(a.Rng.State, b.Rng.State);
        }
    }
}
