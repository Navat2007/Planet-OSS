using System.Collections.Generic;

namespace Planet.Sim
{
    /// <summary>
    /// Детерминированный игровой мир. Сердце lockstep-архитектуры:
    /// одинаковый сид + одинаковый поток команд → идентичное состояние на всех машинах.
    ///
    /// Правила детерминизма:
    ///  - продвижение только через <see cref="Tick"/> (фиксированный шаг);
    ///  - стабильный порядок обхода сущностей (порядок добавления);
    ///  - случайность только через <see cref="Rng"/>;
    ///  - никаких float/Time/Random из UnityEngine.
    /// </summary>
    public sealed class SimWorld
    {
        private readonly List<SimEntity> _entities = new List<SimEntity>();
        private readonly Dictionary<int, SimEntity> _byId = new Dictionary<int, SimEntity>();
        private int _nextEntityId = 1;

        public DeterministicRandom Rng { get; }
        public int CurrentTick { get; private set; }
        public IReadOnlyList<SimEntity> Entities => _entities;

        public SimWorld(uint seed)
        {
            Rng = new DeterministicRandom(seed);
            CurrentTick = 0;
        }

        public SimEntity Find(int id) => _byId.TryGetValue(id, out var e) ? e : null;

        /// <summary>Создать сущность. Id выдаётся детерминированно по счётчику.</summary>
        public SimEntity Spawn(int ownerId, SimVector2 position, int hp, int speedPerTick, int attackRange = 0, int radius = 0)
        {
            var e = new SimEntity(_nextEntityId++, ownerId, position, hp, speedPerTick, attackRange, radius);
            _entities.Add(e);
            _byId[e.Id] = e;
            return e;
        }

        /// <summary>
        /// Один тик симуляции: команды → решение «кто стоит на дистанции выстрела» → шаг движения.
        /// </summary>
        public void Tick(IReadOnlyList<ISimCommand> commands)
        {
            if (commands != null)
            {
                for (int i = 0; i < commands.Count; i++)
                    commands[i].Execute(this);
            }

            int n = _entities.Count;
            EnsureHoldBuffer(n);

            // Фаза A: решаем, кто держит позицию, по снапшоту ДО движения (порядок-независимо).
            for (int i = 0; i < n; i++)
            {
                SimEntity e = _entities[i];
                _holdBuffer[i] = e.Alive && e.AttackRange > 0 && HasEnemyWithinRange(e);
            }

            // Фаза B: двигаем тех, кто не держит позицию.
            for (int i = 0; i < n; i++)
            {
                if (!_holdBuffer[i])
                    _entities[i].Step();
            }

            CurrentTick++;
        }

        private bool[] _holdBuffer;

        private void EnsureHoldBuffer(int n)
        {
            if (_holdBuffer == null || _holdBuffer.Length < n)
                _holdBuffer = new bool[n];
        }

        /// <summary>Есть ли живой враг в пределах дальности выстрела сущности. O(n) на сущность.</summary>
        private bool HasEnemyWithinRange(SimEntity e)
        {
            long r = (long)e.AttackRange * e.AttackRange;
            for (int i = 0; i < _entities.Count; i++)
            {
                SimEntity other = _entities[i];
                if (other == e || !other.Alive || other.OwnerId == e.OwnerId) continue;
                long dSq = (other.Position - e.Position).LengthSquared;
                if (dSq <= r) return true;
            }
            return false;
        }

        /// <summary>
        /// Детерминированный хеш состояния (FNV-1a). Сравнение хешей между клиентами на одном тике
        /// — основной детектор рассинхрона (desync).
        /// </summary>
        public ulong StateHash()
        {
            ulong h = 1469598103934665603UL;

            void Mix(int v)
            {
                for (int i = 0; i < 4; i++)
                {
                    h ^= (byte)(v >> (i * 8));
                    h *= 1099511628211UL;
                }
            }

            Mix(CurrentTick);
            Mix(unchecked((int)Rng.State));
            for (int i = 0; i < _entities.Count; i++)
            {
                SimEntity e = _entities[i];
                Mix(e.Id);
                Mix(e.OwnerId);
                Mix(e.Position.X);
                Mix(e.Position.Z);
                Mix(e.Hp);
                Mix(e.Alive ? 1 : 0);
            }
            return h;
        }
    }
}
