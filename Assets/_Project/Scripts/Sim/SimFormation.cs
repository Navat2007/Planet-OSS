namespace Planet.Sim
{
    /// <summary>
    /// Детерминированный расчёт позиций строя вокруг точки. Используется и командой движения,
    /// и превью-призраком (чтобы предпросмотр совпадал с реальной расстановкой).
    /// </summary>
    public static class SimFormation
    {
        /// <summary>Заполнить <paramref name="slots"/> (длиной count) позициями строя вокруг центра.</summary>
        public static void Fill(int count, int maxRadius, SimVector2 center, SimVector2[] slots)
        {
            if (count <= 0) return;

            int spacing = maxRadius * 2 + (int)(0.6f * SimConstants.UnitsPerMeter);
            int cols = (int)SimMath.Sqrt(count);
            if (cols * cols < count) cols++;
            int rows = (count + cols - 1) / cols;

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int ox = col * spacing - (cols - 1) * spacing / 2;
                int oz = row * spacing - (rows - 1) * spacing / 2;
                slots[i] = new SimVector2(center.X + ox, center.Z + oz);
            }
        }
    }
}
