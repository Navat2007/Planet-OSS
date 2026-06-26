namespace Planet.Sim
{
    /// <summary>
    /// Глобальные константы детерминированной симуляции.
    /// Симуляция работает в целочисленных единицах, чтобы быть детерминированной
    /// на всех машинах (требование lockstep-сети). 1 метр = <see cref="UnitsPerMeter"/>.
    /// </summary>
    public static class SimConstants
    {
        /// <summary>Сколько тиков симуляции в одной секунде. Вся логика обновляется по тикам.</summary>
        public const int TicksPerSecond = 20;

        /// <summary>Длительность одного тика в миллисекундах (используется в Presentation для накопителя времени).</summary>
        public const int TickMilliseconds = 1000 / TicksPerSecond;

        /// <summary>Внутренние единицы расстояния на один метр (миллиметры). Позиции — целые.</summary>
        public const int UnitsPerMeter = 1000;
    }
}
