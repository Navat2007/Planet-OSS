using System;

namespace Planet.Sim
{
    /// <summary>
    /// Детерминированный подбор свободной позиции при спавне: если центр занят,
    /// ищем ближайшую свободную клетку по расширяющейся сетке. Шаг — по размеру юнита + зазор.
    /// </summary>
    public static class SimPlacement
    {
        /// <summary>Свободна ли точка для юнита радиуса <paramref name="radius"/> с зазором <paramref name="gap"/>.</summary>
        public static bool IsFree(SimWorld world, SimVector2 pos, int radius, int gap)
        {
            var ents = world.Entities;
            for (int i = 0; i < ents.Count; i++)
            {
                SimEntity e = ents[i];
                if (!e.Alive) continue;
                long minDist = radius + e.Radius + gap;
                if ((e.Position - pos).LengthSquared < minDist * minDist)
                    return false;
            }
            return true;
        }

        /// <summary>Найти свободную позицию рядом с центром. Сетка-спираль, целочисленно — детерминированно.</summary>
        public static SimVector2 FindFreeSpot(SimWorld world, SimVector2 center, int radius, int gap)
        {
            if (IsFree(world, center, radius, gap)) return center;

            int spacing = Math.Max(radius * 2 + gap, SimConstants.UnitsPerMeter / 2);
            for (int ring = 1; ring <= 48; ring++)
            {
                for (int dz = -ring; dz <= ring; dz++)
                for (int dx = -ring; dx <= ring; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != ring) continue; // только периметр кольца
                    var pos = new SimVector2(center.X + dx * spacing, center.Z + dz * spacing);
                    if (IsFree(world, pos, radius, gap)) return pos;
                }
            }
            return center; // крайний случай
        }
    }
}
