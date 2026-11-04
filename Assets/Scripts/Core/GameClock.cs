using System;

namespace Drowned.Core
{
    /// <summary>
    /// Relógio da simulação (SCRUM-124). Lógica pura, sem dependência de Unity, para ser testável.
    ///
    /// Converte tempo real em <b>ticks</b> e <b>dias simulados</b>:
    /// - Um dia dura <see cref="SecondsPerDay"/> segundos reais na velocidade 1x.
    /// - Cada dia é dividido em <see cref="TicksPerDay"/> ticks; os sistemas (recursos, população)
    ///   atualizam a cada tick, não a cada frame.
    /// - A velocidade (<see cref="GameSpeed"/>) multiplica o tempo real. Pausado = nada avança.
    ///
    /// Uso: chamar <see cref="Advance"/> uma vez por frame com o deltaTime real (não escalado).
    /// </summary>
    public sealed class GameClock
    {
        /// <summary>Disparado a cada tick de simulação, com o índice do tick dentro do dia (0..TicksPerDay-1).</summary>
        public event Action<int> OnTick;

        /// <summary>Disparado quando um novo dia começa, com o número do novo dia (começa em 1).</summary>
        public event Action<int> OnNewDay;

        /// <summary>Disparado quando a velocidade muda.</summary>
        public event Action<GameSpeed> OnSpeedChanged;

        public float SecondsPerDay { get; }
        public int TicksPerDay { get; }

        /// <summary>Dia atual, começando em 1.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>Tick atual dentro do dia (0..TicksPerDay-1).</summary>
        public int TickOfDay { get; private set; }

        /// <summary>Total de ticks disparados desde o início da partida.</summary>
        public long TotalTicks { get; private set; }

        /// <summary>Tempo simulado total, em segundos de jogo (já multiplicado pela velocidade).</summary>
        public double SimulatedSeconds { get; private set; }

        /// <summary>Tempo real jogado, em segundos, descontando pausas.</summary>
        public double PlayedSeconds { get; private set; }

        public GameSpeed Speed { get; private set; } = GameSpeed.Normal;
        public bool IsPaused => Speed == GameSpeed.Paused;

        /// <summary>Progresso do dia atual, de 0 a 1. Útil para um relógio na HUD.</summary>
        public float DayProgress => (float)(_secondsIntoDay / SecondsPerDay);

        private double _secondsIntoDay;
        private GameSpeed _speedBeforePause = GameSpeed.Normal;

        /// <param name="secondsPerDay">Segundos reais por dia simulado a 1x. Vem da planilha de balanceamento (SCRUM-119).</param>
        /// <param name="ticksPerDay">Quantos ticks de simulação por dia. 24 = um por "hora".</param>
        public GameClock(float secondsPerDay = 60f, int ticksPerDay = 24)
        {
            if (secondsPerDay <= 0f) throw new ArgumentOutOfRangeException(nameof(secondsPerDay), "Deve ser maior que zero.");
            if (ticksPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerDay), "Deve ser maior que zero.");
            SecondsPerDay = secondsPerDay;
            TicksPerDay = ticksPerDay;
        }

        public float SecondsPerTick => SecondsPerDay / TicksPerDay;

        /// <summary>
        /// Avança o relógio com o tempo real decorrido no frame. Dispara OnTick e OnNewDay quantas vezes
        /// forem necessárias (um frame longo a 3x pode conter vários ticks).
        /// </summary>
        public void Advance(float realDeltaSeconds)
        {
            if (realDeltaSeconds <= 0f || IsPaused) return;

            PlayedSeconds += realDeltaSeconds;
            double simDelta = realDeltaSeconds * Speed.Multiplier();
            SimulatedSeconds += simDelta;
            _secondsIntoDay += simDelta;

            double secondsPerTick = SecondsPerTick;
            // Quantos ticks já deveriam ter acontecido neste dia, dado o tempo acumulado
            while (true)
            {
                int expectedTick = (int)Math.Floor(_secondsIntoDay / secondsPerTick);
                if (expectedTick >= TicksPerDay)
                {
                    // Completa os ticks que faltam do dia antes de virar
                    while (TickOfDay < TicksPerDay)
                    {
                        FireTick();
                    }
                    _secondsIntoDay -= SecondsPerDay;
                    TickOfDay = 0;
                    Day++;
                    OnNewDay?.Invoke(Day);
                    continue;
                }
                if (TickOfDay < expectedTick)
                {
                    FireTick();
                    continue;
                }
                break;
            }
        }

        private void FireTick()
        {
            // TickOfDay é o índice do tick que está sendo disparado agora
            int index = TickOfDay;
            TickOfDay++;
            TotalTicks++;
            OnTick?.Invoke(index);
        }

        public void SetSpeed(GameSpeed speed)
        {
            if (speed == Speed) return;
            if (speed != GameSpeed.Paused) _speedBeforePause = speed;
            Speed = speed;
            OnSpeedChanged?.Invoke(Speed);
        }

        /// <summary>Pausa, ou retoma na última velocidade usada antes da pausa.</summary>
        public void TogglePause()
        {
            SetSpeed(IsPaused ? _speedBeforePause : GameSpeed.Paused);
        }

        /// <summary>Restaura o estado a partir de um save (SCRUM-176).</summary>
        public void Restore(int day, int tickOfDay, double simulatedSeconds, double playedSeconds)
        {
            if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
            if (tickOfDay < 0 || tickOfDay >= TicksPerDay) throw new ArgumentOutOfRangeException(nameof(tickOfDay));
            Day = day;
            TickOfDay = tickOfDay;
            SimulatedSeconds = simulatedSeconds;
            PlayedSeconds = playedSeconds;
            _secondsIntoDay = tickOfDay * (double)SecondsPerTick;
            TotalTicks = (long)(day - 1) * TicksPerDay + tickOfDay;
        }
    }
}
