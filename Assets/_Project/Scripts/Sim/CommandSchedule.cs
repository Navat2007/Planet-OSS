using System.Collections.Generic;

namespace Planet.Sim
{
    /// <summary>
    /// Очередь команд, привязанных к будущим тикам. В lockstep команды собираются
    /// со всех игроков и исполняются на согласованном тике (с задержкой ввода).
    /// </summary>
    public sealed class CommandSchedule
    {
        private readonly Dictionary<int, List<ISimCommand>> _byTick = new Dictionary<int, List<ISimCommand>>();

        /// <summary>Запланировать команду на исполнение на указанном тике.</summary>
        public void Add(int tick, ISimCommand command)
        {
            if (!_byTick.TryGetValue(tick, out var list))
            {
                list = new List<ISimCommand>();
                _byTick[tick] = list;
            }
            list.Add(command);
        }

        /// <summary>Забрать (и удалить) команды для тика. Возвращает null, если на тик ничего нет.</summary>
        public List<ISimCommand> Take(int tick)
        {
            if (_byTick.TryGetValue(tick, out var list))
            {
                _byTick.Remove(tick);
                return list;
            }
            return null;
        }

        public bool HasAny => _byTick.Count > 0;
    }
}
