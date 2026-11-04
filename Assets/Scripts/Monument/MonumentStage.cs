using System;
using System.Collections.Generic;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.Monument
{
    /// <summary>
    /// Uma etapa de restauração do Monumento Central (SCRUM-160 / SCRUM-163): Fundação, Estrutura, Sistemas, Acabamento.
    /// Os números vêm da aba Monumento da planilha.
    /// </summary>
    [Serializable]
    public class MonumentStage
    {
        [Tooltip("Nome da etapa em português (ex.: Fundação).")]
        [SerializeField] private string _displayName;

        [Tooltip("Recursos que precisam ser entregues à obra.")]
        [SerializeField] private ResourceBundle _requiredResources;

        [Tooltip("Quanto de cada recurso a obra retira do estoque por dia enquanto está ativa.")]
        [SerializeField] private ResourceBundle _contributionPerDay;

        [Tooltip("População mínima para concluir a etapa.")]
        [SerializeField, Min(0)] private int _minPopulation;

        [Tooltip("Ids dos especialistas necessários.")]
        [SerializeField] private List<string> _requiredSpecialistIds = new List<string>();

        [Tooltip("Ids das tecnologias necessárias.")]
        [SerializeField] private List<string> _requiredTechnologyIds = new List<string>();

        [Tooltip("Sprite do Monumento depois de concluir esta etapa.")]
        [SerializeField] private Sprite _sprite;

        public MonumentStage() { }

        /// <summary>Construtor para testes e para o gerador da planilha.</summary>
        public MonumentStage(string displayName, ResourceBundle requiredResources, ResourceBundle contributionPerDay, int minPopulation = 0,
            IEnumerable<string> requiredSpecialistIds = null, IEnumerable<string> requiredTechnologyIds = null, Sprite sprite = null)
        {
            if (minPopulation < 0) throw new ArgumentOutOfRangeException(nameof(minPopulation));
            _displayName = displayName;
            _requiredResources = requiredResources.ClampedToZero();
            _contributionPerDay = contributionPerDay.ClampedToZero();
            _minPopulation = minPopulation;
            _requiredSpecialistIds = requiredSpecialistIds != null ? new List<string>(requiredSpecialistIds) : new List<string>();
            _requiredTechnologyIds = requiredTechnologyIds != null ? new List<string>(requiredTechnologyIds) : new List<string>();
            _sprite = sprite;
        }

        public string DisplayName => _displayName;
        public ResourceBundle RequiredResources => _requiredResources;
        public ResourceBundle ContributionPerDay => _contributionPerDay;
        public int MinPopulation => _minPopulation;
        public IReadOnlyList<string> RequiredSpecialistIds => _requiredSpecialistIds;
        public IReadOnlyList<string> RequiredTechnologyIds => _requiredTechnologyIds;
        public Sprite Sprite => _sprite;
    }
}
