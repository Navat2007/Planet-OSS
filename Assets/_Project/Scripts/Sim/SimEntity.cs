namespace Planet.Sim
{
    /// <summary>
    /// Сущность симуляции (юнит). Только данные и детерминированное поведение —
    /// никакого визуала. Visual-прокси в слое Presentation лишь читает эти поля.
    /// </summary>
    public sealed class SimEntity
    {
        public readonly int Id;
        public readonly int OwnerId;

        public SimVector2 Position;
        public SimVector2 Target;
        public bool HasTarget;

        /// <summary>Скорость в единицах симуляции за один тик (мм/тик).</summary>
        public int SpeedPerTick;

        /// <summary>Дальность выстрела в единицах симуляции (мм). 0 = «остановка на дистанции» отключена.</summary>
        public int AttackRange;

        public int Hp;
        public bool Alive => Hp > 0;

        public SimEntity(int id, int ownerId, SimVector2 position, int hp, int speedPerTick, int attackRange = 0)
        {
            Id = id;
            OwnerId = ownerId;
            Position = position;
            Target = position;
            HasTarget = false;
            Hp = hp;
            SpeedPerTick = speedPerTick;
            AttackRange = attackRange;
        }

        /// <summary>Один шаг движения к цели. Целочисленная арифметика → детерминированно.</summary>
        public void Step()
        {
            if (!Alive || !HasTarget) return;

            SimVector2 delta = Target - Position;
            long speed = SpeedPerTick;
            long distSq = delta.LengthSquared;

            if (distSq <= speed * speed)
            {
                Position = Target; // дошли (или почти) — фиксируемся на цели
                HasTarget = false;
                return;
            }

            long dist = SimMath.Sqrt(distSq);
            int nx = Position.X + (int)((long)delta.X * speed / dist);
            int nz = Position.Z + (int)((long)delta.Z * speed / dist);
            Position = new SimVector2(nx, nz);
        }
    }
}
