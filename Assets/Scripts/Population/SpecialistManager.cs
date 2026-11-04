using System;
using System.Collections.Generic;
using System.Linq;
using Drowned.Resources;
using Drowned.World;

namespace Drowned.Population
{
    /// <summary>
    /// Desbloqueia especialistas por threshold de população e aplica seus bônus (SCRUM-144 / SCRUM-145).
    /// Lógica pura, testável. Um especialista desbloqueado não é perdido se a população cair depois.
    /// </summary>
    public sealed class SpecialistManager
    {
        public event Action<SpecialistData> OnSpecialistUnlocked;

        /// <summary>Multiplicador de pesquisa mudou (Cientista). O ResearchManager lê <see cref="ResearchSpeedMultiplier"/>.</summary>
        public event Action<float> OnResearchSpeedChanged;

        /// <summary>Todos os especialistas, em ordem crescente de threshold.</summary>
        public IReadOnlyList<SpecialistData> All { get; }

        public IReadOnlyList<SpecialistData> Unlocked => _unlocked;
        public int UnlockedCount => _unlocked.Count;

        /// <summary>Multiplicador acumulado de velocidade de pesquisa. Default 1.</summary>
        public float ResearchSpeedMultiplier { get; private set; } = 1f;

        /// <summary>Próximo especialista a desbloquear (menor threshold ainda bloqueado), ou null.</summary>
        public SpecialistData NextToUnlock => All.FirstOrDefault(s => !_unlockedIds.Contains(s.Id));

        private readonly List<SpecialistData> _unlocked = new List<SpecialistData>();
        private readonly HashSet<string> _unlockedIds = new HashSet<string>();
        private readonly PopulationManager _population;
        private readonly ResourceManager _resources;
        private readonly StructureManager _structures;

        public SpecialistManager(IEnumerable<SpecialistData> specialists, PopulationManager population, ResourceManager resources, StructureManager structures)
        {
            if (specialists == null) throw new ArgumentNullException(nameof(specialists));
            _population = population ?? throw new ArgumentNullException(nameof(population));
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));

            All = specialists.Where(s => s != null).OrderBy(s => s.PopulationThreshold).ToList();
            _population.OnPopulationChanged += Evaluate;
            Evaluate(_population.Population);
        }

        public bool IsUnlocked(string id) => _unlockedIds.Contains(id);

        /// <summary>Habitantes que faltam para o próximo especialista. Zero se não há próximo.</summary>
        public int PopulationToNext
        {
            get
            {
                var next = NextToUnlock;
                return next == null ? 0 : Math.Max(0, next.PopulationThreshold - _population.Population);
            }
        }

        /// <summary>Restaura os desbloqueados de um save, aplicando os bônus sem disparar eventos de desbloqueio.</summary>
        public void Restore(IEnumerable<string> unlockedIds)
        {
            if (unlockedIds == null) throw new ArgumentNullException(nameof(unlockedIds));
            foreach (var id in unlockedIds)
            {
                var data = All.FirstOrDefault(s => s.Id == id);
                if (data != null && !_unlockedIds.Contains(id)) Unlock(data, raiseEvent: false);
            }
        }

        private void Evaluate(int population)
        {
            foreach (var s in All)
            {
                if (_unlockedIds.Contains(s.Id) || population < s.PopulationThreshold) continue;
                Unlock(s, raiseEvent: true);
            }
        }

        private void Unlock(SpecialistData data, bool raiseEvent)
        {
            _unlocked.Add(data);
            _unlockedIds.Add(data.Id);
            ApplyBonus(data);
            if (raiseEvent) OnSpecialistUnlocked?.Invoke(data);
        }

        private void ApplyBonus(SpecialistData data)
        {
            float v = data.BonusValue;
            switch (data.Bonus)
            {
                case SpecialistBonus.EnergyProduction:
                    _resources.SetProductionMultiplier(_resources.ProductionMultiplier.With(ResourceType.Energy, _resources.ProductionMultiplier.Energy + v));
                    break;
                case SpecialistBonus.FoodProduction:
                    _resources.SetProductionMultiplier(_resources.ProductionMultiplier.With(ResourceType.Food, _resources.ProductionMultiplier.Food + v));
                    break;
                case SpecialistBonus.ResearchSpeed:
                    ResearchSpeedMultiplier += v;
                    OnResearchSpeedChanged?.Invoke(ResearchSpeedMultiplier);
                    break;
                case SpecialistBonus.BuildCostDiscount:
                    _structures.SetCostMultiplier(_structures.CostMultiplier - v);
                    break;
            }
        }
    }
}
