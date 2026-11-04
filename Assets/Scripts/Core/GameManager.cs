using System;
using Drowned.Population;
using Drowned.Resources;
using Drowned.World;

namespace Drowned.Core
{
    /// <summary>
    /// Raiz de composição da simulação (SCRUM-175). Cria e liga os sistemas puros a partir de <see cref="GameRules"/>,
    /// do <see cref="GameClock"/> e do <see cref="GridMap"/>, e controla o estado da partida.
    /// Os Behaviours de cada sistema só pegam as referências daqui e fazem a ponte com a cena.
    ///
    /// Ordem por dia (OnNewDay): colapso avalia o dia que fechou, depois a população cresce ou encolhe.
    /// </summary>
    public sealed class GameManager : IDisposable
    {
        public event Action<GameState> OnStateChanged;

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsOver => State != GameState.Playing;

        public GameRules Rules { get; }
        public GameClock Clock { get; }
        public GridMap Grid { get; }
        public ResourceManager Resources { get; }
        public PopulationManager Population { get; }
        public StructureManager Structures { get; }
        public SpecialistManager Specialists { get; }
        public CollapseMonitor Collapse { get; }
        public GameStats Stats { get; }

        public GameManager(GameRules rules, GameClock clock, GridMap grid)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));

            Resources = new ResourceManager(rules.InitialStock, clock.TicksPerDay);
            Population = new PopulationManager(rules.Population, Resources);
            Structures = new StructureManager(Grid, Resources, Population);
            Specialists = new SpecialistManager(rules.Specialists, Population, Resources, Structures);
            Collapse = new CollapseMonitor(rules.DaysToCollapse, Resources);
            Stats = new GameStats(Clock, Population, Structures);

            Clock.OnTick += HandleTick;
            Clock.OnNewDay += HandleNewDay;
            Collapse.OnCollapse += HandleCollapse;
        }

        /// <summary>
        /// Coloca as estruturas iniciais das regras sem cobrar. Chame depois de os Behaviours terem assinado
        /// <c>Structures.OnStructureBuilt</c>, para os sprites aparecerem. Devolve quantas foram colocadas.
        /// </summary>
        public int PlaceInitialStructures()
        {
            int placed = 0;
            foreach (var s in Rules.InitialStructures)
            {
                if (s.data == null) continue;
                if (Structures.PlaceFree(s.data, s.x, s.y, s.level) != null) placed++;
            }
            return placed;
        }

        /// <summary>Chamado pelo MonumentManager quando a última etapa é concluída (SCRUM-175).</summary>
        public void ReportVictory() => End(GameState.Victory);

        private void HandleTick(int tickOfDay)
        {
            if (IsOver) return;
            Resources.Tick();
        }

        private void HandleNewDay(int day)
        {
            if (IsOver) return;
            Collapse.EvaluateDay();
            if (IsOver) return;
            Population.AdvanceDay();
        }

        private void HandleCollapse() => End(GameState.Defeat);

        private void End(GameState state)
        {
            if (IsOver) return;
            State = state;
            Clock.SetSpeed(GameSpeed.Paused);
            OnStateChanged?.Invoke(State);
        }

        public void Dispose()
        {
            Clock.OnTick -= HandleTick;
            Clock.OnNewDay -= HandleNewDay;
            Collapse.OnCollapse -= HandleCollapse;
        }
    }
}
