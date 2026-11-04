using System;
using Drowned.Resources;

namespace Drowned.Population
{
    /// <summary>
    /// População da cidade (SCRUM-132) e seu consumo de recursos (SCRUM-133). Lógica pura, testável.
    ///
    /// Regras, avaliadas uma vez por dia em <see cref="AdvanceDay"/>:
    /// - Alimento zerado: perde <c>StarvationLossPerDay</c> habitantes, nunca abaixo de <c>MinimumPopulation</c>.
    /// - Senão, se energia e oxigênio estão positivos e há moradia livre: ganha <c>GrowthPerDay</c>, limitado pela capacidade.
    /// Energia/oxigênio zerados não matam ninguém aqui: isso é o colapso (SCRUM-121), tratado em outro sistema.
    ///
    /// Sempre que a população muda, a fonte <see cref="ResourceSourceId"/> no <see cref="ResourceManager"/>
    /// é atualizada com consumo = per capita x população.
    /// </summary>
    public sealed class PopulationManager
    {
        /// <summary>Id da fonte de consumo da população no ResourceManager.</summary>
        public const string ResourceSourceId = "population";

        public event Action<int> OnPopulationChanged;
        public event Action<int> OnHousingCapacityChanged;

        public PopulationConfig Config { get; }
        public int Population { get; private set; }

        /// <summary>Capacidade de moradia efetiva: soma das estruturas x <see cref="HousingMultiplier"/>, arredondada para baixo.</summary>
        public int HousingCapacity { get; private set; }

        /// <summary>Soma da capacidade de moradia das estruturas construídas, sem bônus.</summary>
        public int BaseHousingCapacity { get; private set; }

        /// <summary>Multiplicador de moradia (tecnologias, SCRUM-148). Default 1.</summary>
        public float HousingMultiplier { get; private set; } = 1f;

        public int FreeHousing => Math.Max(0, HousingCapacity - Population);

        private readonly ResourceManager _resources;

        public PopulationManager(PopulationConfig config, ResourceManager resources, int initialHousingCapacity = 0)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            if (initialHousingCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialHousingCapacity));
            BaseHousingCapacity = initialHousingCapacity;
            HousingCapacity = initialHousingCapacity;
            Population = config.InitialPopulation;
            RegisterConsumption();
        }

        /// <summary>Define a capacidade base (soma das estruturas).</summary>
        public void SetHousingCapacity(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            BaseHousingCapacity = capacity;
            RecomputeHousing();
        }

        /// <summary>Soma (ou subtrai, se negativo) capacidade base. Usado ao construir, melhorar ou remover moradias.</summary>
        public void AddHousingCapacity(int delta) => SetHousingCapacity(Math.Max(0, BaseHousingCapacity + delta));

        public void SetHousingMultiplier(float multiplier)
        {
            HousingMultiplier = Math.Max(0f, multiplier);
            RecomputeHousing();
        }

        private void RecomputeHousing()
        {
            int effective = (int)Math.Floor(BaseHousingCapacity * HousingMultiplier + 1e-4);
            if (effective == HousingCapacity) return;
            HousingCapacity = effective;
            OnHousingCapacityChanged?.Invoke(HousingCapacity);
        }

        /// <summary>Aplica a regra diária. Ligar em <c>GameClock.OnNewDay</c>.</summary>
        public void AdvanceDay()
        {
            if (_resources.IsDepleted(ResourceType.Food))
            {
                int floor = Math.Min(Config.MinimumPopulation, Population);
                SetPopulation(Math.Max(floor, Population - Config.StarvationLossPerDay));
                return;
            }

            if (_resources.IsDepleted(ResourceType.Energy) || _resources.IsDepleted(ResourceType.Oxygen)) return;
            if (Population >= HousingCapacity) return;

            SetPopulation(Math.Min(HousingCapacity, Population + Config.GrowthPerDay));
        }

        /// <summary>Define a população diretamente. Usado pelo save/load e por eventos.</summary>
        public void SetPopulation(int population)
        {
            if (population < 0) throw new ArgumentOutOfRangeException(nameof(population));
            if (population == Population) return;
            Population = population;
            RegisterConsumption();
            OnPopulationChanged?.Invoke(Population);
        }

        private void RegisterConsumption() =>
            _resources.SetSource(ResourceSourceId, ResourceBundle.Zero, Config.ConsumptionPerCapitaPerDay * Population);
    }
}
