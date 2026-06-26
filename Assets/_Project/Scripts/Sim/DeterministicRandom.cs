namespace Planet.Sim
{
    /// <summary>
    /// Детерминированный ГПСЧ (xorshift32). ОБЯЗАТЕЛЕН вместо UnityEngine.Random/System.Random
    /// в любой логике симуляции: одинаковый сид → одинаковая последовательность на всех машинах.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(uint seed)
        {
            _state = seed == 0u ? 1u : seed; // 0 — вырожденное состояние для xorshift
        }

        /// <summary>Текущее внутреннее состояние (входит в хеш состояния мира для детект-рассинхрона).</summary>
        public uint State => _state;

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Целое в диапазоне [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }
    }
}
