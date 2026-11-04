using System;
using System.Collections.Generic;
using Drowned.Population;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.Core
{
    /// <summary>
    /// Regras e valores de começo de partida (SCRUM-120 / SCRUM-121). Um único asset em <c>Assets/Data/GameRules.asset</c>,
    /// gerado a partir das abas <c>Tempo</c>, <c>Inicial</c> e <c>População</c> da planilha.
    /// Os defaults são a proposta da IA registrada no Jira, só para a cena rodar antes de a planilha existir.
    /// </summary>
    [CreateAssetMenu(fileName = "GameRules", menuName = "Drowned/Game Rules", order = 1)]
    public sealed class GameRules : ScriptableObject
    {
        /// <summary>Uma estrutura já construída no início da partida.</summary>
        [Serializable]
        public class InitialStructure
        {
            public StructureData data;
            public int x;
            public int y;
            [Min(1)] public int level = 1;

            public InitialStructure() { }

            public InitialStructure(StructureData data, int x, int y, int level = 1)
            {
                this.data = data;
                this.x = x;
                this.y = y;
                this.level = level;
            }
        }

        [Header("Colapso (SCRUM-121, aba Tempo)")]
        [Tooltip("Dias consecutivos com energia OU oxigênio zerados até a derrota.")]
        [SerializeField, Min(1)] private int _daysToCollapse = 3;

        [Header("Começo de partida (SCRUM-120, aba Inicial)")]
        [SerializeField] private ResourceBundle _initialStock = new ResourceBundle(100f, 100f, 80f);
        [SerializeField] private List<InitialStructure> _initialStructures = new List<InitialStructure>();

        [Header("População (SCRUM-132, aba População)")]
        [SerializeField] private PopulationConfig _population = new PopulationConfig();

        [Header("Especialistas (SCRUM-144 / SCRUM-150)")]
        [SerializeField] private List<SpecialistData> _specialists = new List<SpecialistData>();

        public int DaysToCollapse => _daysToCollapse;
        public ResourceBundle InitialStock => _initialStock;
        public IReadOnlyList<InitialStructure> InitialStructures => _initialStructures;
        public PopulationConfig Population => _population;
        public IReadOnlyList<SpecialistData> Specialists => _specialists;

        /// <summary>Define os especialistas em memória. Para testes e para o gerador da planilha.</summary>
        public void SetSpecialists(IEnumerable<SpecialistData> specialists)
        {
            _specialists = specialists != null ? new List<SpecialistData>(specialists) : new List<SpecialistData>();
        }

        /// <summary>Cria regras em memória sem asset. Para testes e para o gerador da planilha.</summary>
        public static GameRules Create(int daysToCollapse, ResourceBundle initialStock, PopulationConfig population, params InitialStructure[] initialStructures)
        {
            if (daysToCollapse < 1) throw new ArgumentOutOfRangeException(nameof(daysToCollapse));
            var rules = CreateInstance<GameRules>();
            rules._daysToCollapse = daysToCollapse;
            rules._initialStock = initialStock;
            rules._population = population ?? throw new ArgumentNullException(nameof(population));
            rules._initialStructures = new List<InitialStructure>(initialStructures);
            return rules;
        }
    }
}
