using System;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.Population
{
    /// <summary>
    /// Números do sistema de população (SCRUM-132 / SCRUM-133). Vêm da aba <c>População</c> da planilha.
    /// Os valores default aqui são só a proposta inicial da IA registrada no Jira, para a cena rodar
    /// antes de a planilha existir.
    /// </summary>
    [Serializable]
    public class PopulationConfig
    {
        [Tooltip("Habitantes no começo da partida (SCRUM-120).")]
        [SerializeField, Min(0)] private int _initialPopulation = 5;

        [Tooltip("Quantos habitantes chegam por dia quando há moradia e os três recursos estão positivos.")]
        [SerializeField, Min(0)] private int _growthPerDay = 1;

        [Tooltip("Quantos habitantes a cidade perde por dia com o alimento zerado.")]
        [SerializeField, Min(0)] private int _starvationLossPerDay = 1;

        [Tooltip("A fome nunca reduz a população abaixo deste valor.")]
        [SerializeField, Min(0)] private int _minimumPopulation = 1;

        [Tooltip("Quanto UM habitante consome de cada recurso por dia simulado.")]
        [SerializeField] private ResourceBundle _consumptionPerCapitaPerDay = new ResourceBundle(1f, 1f, 1f);

        public PopulationConfig() { }

        /// <summary>Construtor para testes e para o gerador a partir da planilha.</summary>
        public PopulationConfig(int initialPopulation, int growthPerDay, int starvationLossPerDay, int minimumPopulation, ResourceBundle consumptionPerCapitaPerDay)
        {
            if (initialPopulation < 0) throw new ArgumentOutOfRangeException(nameof(initialPopulation));
            if (growthPerDay < 0) throw new ArgumentOutOfRangeException(nameof(growthPerDay));
            if (starvationLossPerDay < 0) throw new ArgumentOutOfRangeException(nameof(starvationLossPerDay));
            if (minimumPopulation < 0) throw new ArgumentOutOfRangeException(nameof(minimumPopulation));
            _initialPopulation = initialPopulation;
            _growthPerDay = growthPerDay;
            _starvationLossPerDay = starvationLossPerDay;
            _minimumPopulation = minimumPopulation;
            _consumptionPerCapitaPerDay = consumptionPerCapitaPerDay;
        }

        public int InitialPopulation => _initialPopulation;
        public int GrowthPerDay => _growthPerDay;
        public int StarvationLossPerDay => _starvationLossPerDay;
        public int MinimumPopulation => _minimumPopulation;
        public ResourceBundle ConsumptionPerCapitaPerDay => _consumptionPerCapitaPerDay;
    }
}
