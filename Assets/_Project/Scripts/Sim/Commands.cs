namespace Planet.Sim
{
    /// <summary>Создать сущность в мире (детерминированно, с заранее известным Id).</summary>
    public sealed class SpawnCommand : ISimCommand
    {
        public readonly int OwnerId;
        public readonly SimVector2 Position;
        public readonly int Hp;
        public readonly int SpeedPerTick;
        public readonly int AttackRange;

        public SpawnCommand(int ownerId, SimVector2 position, int hp, int speedPerTick, int attackRange = 0)
        {
            OwnerId = ownerId;
            Position = position;
            Hp = hp;
            SpeedPerTick = speedPerTick;
            AttackRange = attackRange;
        }

        public void Execute(SimWorld world)
        {
            world.Spawn(OwnerId, Position, Hp, SpeedPerTick, AttackRange);
        }
    }

    /// <summary>Приказ движения: задать цель набору сущностей одного владельца.</summary>
    public sealed class MoveCommand : ISimCommand
    {
        public readonly int OwnerId;
        public readonly int[] EntityIds;
        public readonly SimVector2 Target;

        public MoveCommand(int ownerId, int[] entityIds, SimVector2 target)
        {
            OwnerId = ownerId;
            EntityIds = entityIds;
            Target = target;
        }

        public void Execute(SimWorld world)
        {
            if (EntityIds == null) return;
            foreach (int id in EntityIds)
            {
                SimEntity e = world.Find(id);
                // Двигать можно только живые сущности своего владельца (защита от читов/рассинхрона).
                if (e == null || !e.Alive || e.OwnerId != OwnerId) continue;
                e.Target = Target;
                e.HasTarget = true;
            }
        }
    }
}
