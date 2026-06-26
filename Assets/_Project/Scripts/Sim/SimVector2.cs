using System;

namespace Planet.Sim
{
    /// <summary>
    /// Детерминированный 2D-вектор в целочисленных единицах симуляции (миллиметрах),
    /// в плоскости XZ. Все операции целочисленные.
    /// </summary>
    public readonly struct SimVector2 : IEquatable<SimVector2>
    {
        public readonly int X;
        public readonly int Z;

        public SimVector2(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static readonly SimVector2 Zero = new SimVector2(0, 0);

        /// <summary>Создать вектор из метров (для удобства настройки/тестов).</summary>
        public static SimVector2 FromMeters(int xMeters, int zMeters)
            => new SimVector2(xMeters * SimConstants.UnitsPerMeter, zMeters * SimConstants.UnitsPerMeter);

        public static SimVector2 operator +(SimVector2 a, SimVector2 b) => new SimVector2(a.X + b.X, a.Z + b.Z);
        public static SimVector2 operator -(SimVector2 a, SimVector2 b) => new SimVector2(a.X - b.X, a.Z - b.Z);
        public static SimVector2 operator *(SimVector2 a, int s) => new SimVector2(a.X * s, a.Z * s);

        /// <summary>Квадрат длины (long, чтобы не было переполнения).</summary>
        public long LengthSquared => (long)X * X + (long)Z * Z;

        /// <summary>Длина (floor целочисленного корня).</summary>
        public int Length => (int)SimMath.Sqrt(LengthSquared);

        public bool Equals(SimVector2 other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is SimVector2 v && Equals(v);
        public override int GetHashCode() => unchecked((X * 397) ^ Z);
        public override string ToString() => $"({X}, {Z})";
    }
}
