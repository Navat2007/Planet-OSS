using NUnit.Framework;
using Planet.Sim;

namespace Planet.Tests.EditMode
{
    public sealed class SimMechanicsTests
    {
        [Test]
        public void Spawn_CreatesEntityWithDefinedHp()
        {
            var world = new SimWorld(seed: 1);
            var e = world.Spawn(ownerId: 0, position: SimVector2.Zero, hp: 100, speedPerTick: 100);
            Assert.AreEqual(100, e.Hp);
            Assert.IsTrue(e.Alive);
            Assert.AreSame(e, world.Find(e.Id));
        }

        [Test]
        public void MoveCommand_AppliesExactlyOnScheduledTick()
        {
            var world = new SimWorld(seed: 1);
            var e = world.Spawn(0, SimVector2.Zero, 100, speedPerTick: 100);
            var schedule = new CommandSchedule();
            schedule.Add(3, new MoveCommand(0, new[] { e.Id }, SimVector2.FromMeters(10, 0)));

            // Тики 0,1,2 — цели нет, юнит стоит.
            for (int tick = 0; tick < 3; tick++)
            {
                world.Tick(schedule.Take(world.CurrentTick));
                Assert.IsFalse(e.HasTarget, $"Цель не должна появиться раньше тика 3 (сейчас {tick}).");
                Assert.AreEqual(SimVector2.Zero, e.Position);
            }

            // Тик 3 — команда применяется, юнит получает цель и начинает движение.
            world.Tick(schedule.Take(world.CurrentTick));
            Assert.IsTrue(e.HasTarget);
            Assert.Greater(e.Position.X, 0, "После применения команды юнит должен сдвинуться к цели.");
        }

        [Test]
        public void Unit_ReachesTarget_AndStops()
        {
            var world = new SimWorld(seed: 1);
            var e = world.Spawn(0, SimVector2.Zero, 100, speedPerTick: 200);
            e.Target = SimVector2.FromMeters(5, 0);
            e.HasTarget = true;

            for (int i = 0; i < 200 && e.HasTarget; i++)
                world.Tick(null);

            Assert.IsFalse(e.HasTarget, "Юнит должен прекратить движение, дойдя до цели.");
            Assert.AreEqual(SimVector2.FromMeters(5, 0), e.Position);
        }

        [Test]
        public void MoveCommand_IgnoresEntitiesOfOtherOwner()
        {
            var world = new SimWorld(seed: 1);
            var mine = world.Spawn(0, SimVector2.Zero, 100, 100);
            var enemy = world.Spawn(1, SimVector2.Zero, 100, 100);

            new MoveCommand(0, new[] { mine.Id, enemy.Id }, SimVector2.FromMeters(10, 0)).Execute(world);

            Assert.IsTrue(mine.HasTarget, "Своему юниту приказ должен примениться.");
            Assert.IsFalse(enemy.HasTarget, "Чужому юниту приказ применяться не должен.");
        }

        [Test]
        public void Enemies_StopAtAttackRange_WithoutOverlapping()
        {
            var world = new SimWorld(seed: 1);
            int range = 5 * SimConstants.UnitsPerMeter; // 5 м

            var a = world.Spawn(0, SimVector2.FromMeters(-10, 0), 100, speedPerTick: 200, attackRange: range);
            var b = world.Spawn(1, SimVector2.FromMeters(10, 0), 100, speedPerTick: 200, attackRange: range);
            a.Target = SimVector2.FromMeters(10, 0); a.HasTarget = true;   // идут друг на друга
            b.Target = SimVector2.FromMeters(-10, 0); b.HasTarget = true;

            for (int i = 0; i < 500; i++) world.Tick(null);

            int dist = (b.Position - a.Position).Length;
            Assert.Greater(dist, range - 450, "Не должны подходить ближе дистанции выстрела (допуск на шаг).");
            Assert.Less(dist, range + 50, "Должны подойти примерно на дистанцию выстрела.");
        }

        [Test]
        public void ZeroAttackRange_DoesNotStopForEnemies()
        {
            // Со старым поведением (range=0) юнит доходит до цели сквозь врага.
            var world = new SimWorld(seed: 1);
            var a = world.Spawn(0, SimVector2.FromMeters(-10, 0), 100, speedPerTick: 200); // attackRange = 0
            world.Spawn(1, SimVector2.Zero, 100, 0);
            a.Target = SimVector2.FromMeters(10, 0); a.HasTarget = true;

            for (int i = 0; i < 500 && a.HasTarget; i++) world.Tick(null);

            Assert.IsFalse(a.HasTarget, "При range=0 остановка по врагу не работает, юнит доходит до цели.");
            Assert.AreEqual(SimVector2.FromMeters(10, 0), a.Position);
        }

        [Test]
        public void IntegerSqrt_IsCorrectFloor()
        {
            Assert.AreEqual(0, SimMath.Sqrt(0));
            Assert.AreEqual(2, SimMath.Sqrt(4));
            Assert.AreEqual(3, SimMath.Sqrt(15)); // floor(3.87)
            Assert.AreEqual(1000, SimMath.Sqrt(1000000));
        }
    }
}
