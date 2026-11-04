using System;
using Drowned.Resources;

namespace Drowned.Core
{
    /// <summary>
    /// Conta dias consecutivos com energia <b>ou</b> oxigênio zerados e dispara o colapso (SCRUM-174 / SCRUM-121).
    /// Alimento zerado não conta: só reduz a população (PopulationManager). Lógica pura, testável.
    /// Avaliar uma vez por dia, em <c>GameClock.OnNewDay</c>, antes de qualquer outra regra diária.
    /// </summary>
    public sealed class CollapseMonitor
    {
        /// <summary>Número de dias consecutivos em perigo mudou (inclusive voltou a zero). Para o alerta de crise.</summary>
        public event Action<int> OnDangerChanged;

        /// <summary>Disparado uma única vez quando a cidade colapsa.</summary>
        public event Action OnCollapse;

        public int DaysToCollapse { get; }
        public int ConsecutiveDays { get; private set; }
        public bool Collapsed { get; private set; }

        /// <summary>Dias que faltam para o colapso se nada mudar. Zero quando já colapsou.</summary>
        public int DaysRemaining => Math.Max(0, DaysToCollapse - ConsecutiveDays);

        /// <summary>Verdadeiro enquanto energia ou oxigênio está zerado agora (antes mesmo de fechar o dia).</summary>
        public bool InDanger => _resources.IsDepleted(ResourceType.Energy) || _resources.IsDepleted(ResourceType.Oxygen);

        private readonly ResourceManager _resources;

        public CollapseMonitor(int daysToCollapse, ResourceManager resources)
        {
            if (daysToCollapse < 1) throw new ArgumentOutOfRangeException(nameof(daysToCollapse), "Deve ser pelo menos 1.");
            DaysToCollapse = daysToCollapse;
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        }

        /// <summary>Fecha um dia: incrementa ou zera a contagem e dispara o colapso se chegou ao limite.</summary>
        public void EvaluateDay()
        {
            if (Collapsed) return;

            int next = InDanger ? ConsecutiveDays + 1 : 0;
            if (next != ConsecutiveDays)
            {
                ConsecutiveDays = next;
                OnDangerChanged?.Invoke(ConsecutiveDays);
            }

            if (ConsecutiveDays >= DaysToCollapse)
            {
                Collapsed = true;
                OnCollapse?.Invoke();
            }
        }

        /// <summary>Restaura a contagem. Usado pelo save/load.</summary>
        public void Restore(int consecutiveDays, bool collapsed)
        {
            if (consecutiveDays < 0) throw new ArgumentOutOfRangeException(nameof(consecutiveDays));
            ConsecutiveDays = consecutiveDays;
            Collapsed = collapsed;
        }
    }
}
