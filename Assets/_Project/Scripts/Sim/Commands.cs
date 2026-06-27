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

    /// <summary>
    /// Групповой приказ движения: расставляет выделенных в строй вокруг точки.
    ///  - Queue=true (Shift) — добавить точку в очередь маршрута, иначе — заменить приказ;
    ///  - Facing != Zero — задать направление после прибытия (без реверса), иначе авто (с реверсом).
    /// </summary>
    public sealed class MoveOrderCommand : ISimCommand
    {
        public readonly int OwnerId;
        public readonly int[] EntityIds;
        public readonly SimVector2 Target;
        public readonly bool Queue;
        public readonly SimVector2 Facing;

        public MoveOrderCommand(int ownerId, int[] entityIds, SimVector2 target, bool queue = false, SimVector2 facing = default)
        {
            OwnerId = ownerId;
            EntityIds = entityIds;
            Target = target;
            Queue = queue;
            Facing = facing;
        }

        public void Execute(SimWorld world)
        {
            if (EntityIds == null) return;

            var units = new System.Collections.Generic.List<SimEntity>(EntityIds.Length);
            int maxRadius = 0;
            foreach (int id in EntityIds)
            {
                SimEntity e = world.Find(id);
                if (e == null || !e.Alive || e.OwnerId != OwnerId) continue;
                units.Add(e);
                if (e.Radius > maxRadius) maxRadius = e.Radius;
            }
            int n = units.Count;
            if (n == 0) return;

            var slots = new SimVector2[n];
            SimFormation.Fill(n, maxRadius, Target, slots);

            bool hasFacing = Facing.LengthSquared != 0;
            for (int i = 0; i < n; i++)
            {
                SimEntity e = units[i];
                e.DesiredFacing = Facing; // Zero, если не задано

                if (Queue)
                {
                    if (!e.HasTarget && e.Waypoints.Count == 0) e.OrderMoveTo(slots[i], allowReverse: false);
                    else e.Waypoints.Enqueue(slots[i]);
                }
                else
                {
                    e.Waypoints.Clear();
                    e.OrderMoveTo(slots[i], allowReverse: !hasFacing); // явный facing отменяет реверс
                }
            }
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
