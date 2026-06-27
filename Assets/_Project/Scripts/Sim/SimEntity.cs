using System.Collections.Generic;

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

        /// <summary>Радиус юнита (мм) — для расхождения/подбора места при спавне.</summary>
        public int Radius;

        /// <summary>Направление «лица» юнита (вектор в мм). По нему поворачивается модель.</summary>
        public SimVector2 Heading = new SimVector2(0, SimConstants.UnitsPerMeter);

        /// <summary>Едет ли юнит задним ходом (не разворачиваясь).</summary>
        public bool Reversing;

        /// <summary>Дистанция (мм), в пределах которой цель сзади → юнит пятится, а не разворачивается.</summary>
        public int ReverseDistance;

        /// <summary>Очередь точек маршрута (Shift+ПКМ).</summary>
        public readonly Queue<SimVector2> Waypoints = new Queue<SimVector2>();

        /// <summary>Желаемое направление после прибытия (для facing). Zero = не задано.</summary>
        public SimVector2 DesiredFacing;

        public int Hp;
        public readonly int MaxHp;
        public bool Alive => Hp > 0;

        public SimEntity(int id, int ownerId, SimVector2 position, int hp, int speedPerTick, int attackRange = 0, int radius = 0)
        {
            Id = id;
            OwnerId = ownerId;
            Position = position;
            Target = position;
            HasTarget = false;
            Hp = hp;
            MaxHp = hp;
            SpeedPerTick = speedPerTick;
            AttackRange = attackRange;
            Radius = radius;
        }

        /// <summary>
        /// Приказ движения в точку. Решает реверс (если разрешён и цель близко сзади):
        /// тогда лицо не меняется, юнит пятится; иначе разворачивается к цели.
        /// </summary>
        public void OrderMoveTo(SimVector2 slot, bool allowReverse)
        {
            SimVector2 dir = slot - Position;
            if (dir.LengthSquared == 0)
            {
                HasTarget = false;
                Reversing = false;
                return;
            }

            long dot = (long)dir.X * Heading.X + (long)dir.Z * Heading.Z;
            long revSq = (long)ReverseDistance * ReverseDistance;
            bool reverse = allowReverse && ReverseDistance > 0 && dir.LengthSquared <= revSq && dot < 0;

            Reversing = reverse;
            if (!reverse) Heading = dir; // развернуться к цели; при реверсе лицо сохраняем
            Target = slot;
            HasTarget = true;
        }

        /// <summary>Один шаг движения к цели. Целочисленная арифметика → детерминированно.</summary>
        public void Step()
        {
            if (!Alive) return;

            if (!HasTarget)
            {
                if (Waypoints.Count > 0) OrderMoveTo(Waypoints.Dequeue(), false);
                if (!HasTarget) { ApplyDesiredFacingIfIdle(); return; }
            }

            SimVector2 delta = Target - Position;
            long speed = SpeedPerTick;
            long distSq = delta.LengthSquared;

            if (distSq <= speed * speed)
            {
                Position = Target; // дошли (или почти) — фиксируемся на цели
                HasTarget = false;
                if (Waypoints.Count > 0) OrderMoveTo(Waypoints.Dequeue(), false);
                else ApplyDesiredFacingIfIdle();
                return;
            }

            long dist = SimMath.Sqrt(distSq);
            int nx = Position.X + (int)((long)delta.X * speed / dist);
            int nz = Position.Z + (int)((long)delta.Z * speed / dist);
            Position = new SimVector2(nx, nz);
        }

        /// <summary>Если маршрут пройден и задано желаемое направление — повернуть лицо к нему.</summary>
        private void ApplyDesiredFacingIfIdle()
        {
            if (HasTarget || Waypoints.Count > 0) return;
            if (DesiredFacing.LengthSquared == 0) return;

            Heading = DesiredFacing;
            Reversing = false;
            DesiredFacing = SimVector2.Zero;
        }
    }
}
