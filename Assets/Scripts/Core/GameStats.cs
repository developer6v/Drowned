using System;
using System.Collections.Generic;
using Drowned.Population;
using Drowned.World;

namespace Drowned.Core
{
    /// <summary>
    /// Estatísticas da partida para as telas de vitória e derrota (SCRUM-177). Alimentado por eventos,
    /// nunca por polling. Lógica pura, testável.
    /// </summary>
    public sealed class GameStats
    {
        public int PeakPopulation { get; private set; }
        public int StructuresBuilt { get; private set; }
        public int UpgradesDone { get; private set; }
        public int TechnologiesResearched { get; private set; }
        public int MonumentStagesCompleted { get; private set; }

        private readonly GameClock _clock;
        private readonly PopulationManager _population;

        public GameStats(GameClock clock, PopulationManager population, StructureManager structures)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _population = population ?? throw new ArgumentNullException(nameof(population));
            if (structures == null) throw new ArgumentNullException(nameof(structures));

            PeakPopulation = population.Population;
            population.OnPopulationChanged += p => { if (p > PeakPopulation) PeakPopulation = p; };
            structures.OnStructureBuilt += _ => StructuresBuilt++;
            structures.OnStructureChanged += _ => UpgradesDone++;
        }

        public double PlayedSeconds => _clock.PlayedSeconds;
        public int Days => _clock.Day;
        public int FinalPopulation => _population.Population;

        /// <summary>Chamado pelo ResearchManager (SCRUM-147).</summary>
        public void RegisterTechnology() => TechnologiesResearched++;

        /// <summary>Chamado pelo MonumentManager (SCRUM-160).</summary>
        public void RegisterMonumentStage() => MonumentStagesCompleted++;

        /// <summary>Restaura contadores. Usado pelo save/load.</summary>
        public void Restore(int peakPopulation, int structuresBuilt, int upgradesDone, int technologiesResearched, int monumentStagesCompleted)
        {
            PeakPopulation = peakPopulation;
            StructuresBuilt = structuresBuilt;
            UpgradesDone = upgradesDone;
            TechnologiesResearched = technologiesResearched;
            MonumentStagesCompleted = monumentStagesCompleted;
        }

        public static string FormatDuration(double seconds)
        {
            var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes:00}min" : $"{t.Minutes}min {t.Seconds:00}s";
        }

        /// <summary>Linhas em português para a tela final, na ordem de exibição.</summary>
        public IReadOnlyList<(string label, string value)> Summary() => new[]
        {
            ("Tempo jogado", FormatDuration(PlayedSeconds)),
            ("Dias simulados", Days.ToString()),
            ("População final", FinalPopulation.ToString()),
            ("Pico de população", PeakPopulation.ToString()),
            ("Estruturas construídas", StructuresBuilt.ToString()),
            ("Melhorias feitas", UpgradesDone.ToString()),
            ("Tecnologias pesquisadas", TechnologiesResearched.ToString()),
            ("Etapas do Monumento", MonumentStagesCompleted.ToString())
        };
    }
}
