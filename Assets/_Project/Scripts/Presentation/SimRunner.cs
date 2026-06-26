using System;
using Planet.Sim;
using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Мост между Unity и детерминированной симуляцией. Прокручивает <see cref="SimWorld"/>
    /// фиксированным шагом (накопитель по реальному времени), отделяя кадры рендера от тиков логики.
    /// Визуальные компоненты читают состояние мира, но НЕ изменяют его.
    /// </summary>
    public sealed class SimRunner : MonoBehaviour
    {
        public SimWorld World { get; private set; }
        public CommandSchedule Schedule { get; private set; }

        /// <summary>Запущен ли прогон тиков. Пауза/лобби могут его останавливать.</summary>
        public bool Running { get; set; }

        /// <summary>Вызывается после каждого выполненного тика (для views, эффектов, звука).</summary>
        public event Action Ticked;

        private double _accumulator;
        private double TickSeconds => 1.0 / SimConstants.TicksPerSecond;

        /// <summary>Инициализировать мир с заданным сидом. Сид должен быть согласован между клиентами.</summary>
        public void Initialize(uint seed)
        {
            World = new SimWorld(seed);
            Schedule = new CommandSchedule();
            _accumulator = 0;
            Running = true;
        }

        private void Update()
        {
            if (!Running || World == null) return;

            _accumulator += Time.deltaTime;
            // Catch-up: за кадр может пройти несколько тиков. Ограничим, чтобы не «спираль смерти».
            int safety = 0;
            while (_accumulator >= TickSeconds && safety < 8)
            {
                var commands = Schedule.Take(World.CurrentTick);
                World.Tick(commands);
                _accumulator -= TickSeconds;
                safety++;
                Ticked?.Invoke();
            }
        }
    }
}
