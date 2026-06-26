namespace Planet.Sim
{
    /// <summary>
    /// Детерминированная целочисленная математика. Никакого <c>float</c>/<c>double</c> —
    /// иначе симуляция может разойтись между машинами и сломать lockstep.
    /// </summary>
    public static class SimMath
    {
        /// <summary>Целочисленный квадратный корень (floor) методом Ньютона. Детерминирован.</summary>
        public static long Sqrt(long value)
        {
            if (value <= 0) return 0;
            long x = value;
            long y = (x + 1) / 2;
            while (y < x)
            {
                x = y;
                y = (x + value / x) / 2;
            }
            return x;
        }
    }
}
