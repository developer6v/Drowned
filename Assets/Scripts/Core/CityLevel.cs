using System;
using Drowned.Population;
using Drowned.Research;
using Drowned.World;
using UnityEngine;

namespace Drowned.Core
{
    /// <summary>Pesos e faixas do nível de desenvolvimento (SCRUM-162). Vêm da planilha.</summary>
    [Serializable]
    public class CityLevelConfig
    {
        [SerializeField, Min(0f)] private float _populationWeight = 1f;
        [SerializeField, Min(0f)] private float _structureLevelWeight = 3f;
        [SerializeField, Min(0f)] private float _technologyWeight = 10f;
        [Tooltip("Pontos necessários para cada nível acima do primeiro.")]
        [SerializeField, Min(1f)] private float _pointsPerLevel = 50f;
        [SerializeField, Min(1)] private int _maxLevel = 5;

        public CityLevelConfig() { }

        public CityLevelConfig(float populationWeight, float structureLevelWeight, float technologyWeight, float pointsPerLevel, int maxLevel)
        {
            _populationWeight = Mathf.Max(0f, populationWeight);
            _structureLevelWeight = Mathf.Max(0f, structureLevelWeight);
            _technologyWeight = Mathf.Max(0f, technologyWeight);
            _pointsPerLevel = Mathf.Max(1f, pointsPerLevel);
            _maxLevel = Mathf.Max(1, maxLevel);
        }

        public float PopulationWeight => _populationWeight;
        public float StructureLevelWeight => _structureLevelWeight;
        public float TechnologyWeight => _technologyWeight;
        public float PointsPerLevel => _pointsPerLevel;
        public int MaxLevel => _maxLevel;
    }

    /// <summary>
    /// Nível de desenvolvimento da cidade (SCRUM-162): população + soma dos níveis das estruturas + tecnologias,
    /// com pesos. Recalculado por eventos. Lógica pura, testável.
    /// </summary>
    public sealed class CityLevel
    {
        public event Action<int> OnLevelChanged;

        public CityLevelConfig Config { get; }
        public float Score { get; private set; }
        public int Level { get; private set; } = 1;

        /// <summary>Fração do caminho até o próximo nível (0 a 1). 1 no nível máximo.</summary>
        public float ProgressToNext
        {
            get
            {
                if (Level >= Config.MaxLevel) return 1f;
                float into = Score - (Level - 1) * Config.PointsPerLevel;
                return Mathf.Clamp01(into / Config.PointsPerLevel);
            }
        }

        private readonly PopulationManager _population;
        private readonly StructureManager _structures;
        private readonly ResearchManager _research;

        public CityLevel(CityLevelConfig config, PopulationManager population, StructureManager structures, ResearchManager research)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _population = population ?? throw new ArgumentNullException(nameof(population));
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));
            _research = research ?? throw new ArgumentNullException(nameof(research));

            _population.OnPopulationChanged += _ => Recompute();
            _structures.OnStructureBuilt += _ => Recompute();
            _structures.OnStructureChanged += _ => Recompute();
            _structures.OnStructureRemoved += _ => Recompute();
            _research.OnResearched += _ => Recompute();
            Recompute();
        }

        public void Recompute()
        {
            int structureLevels = 0;
            foreach (var s in _structures.Instances) structureLevels += s.Level;

            Score = _population.Population * Config.PopulationWeight
                  + structureLevels * Config.StructureLevelWeight
                  + _research.Researched.Count * Config.TechnologyWeight;

            int level = Mathf.Clamp(1 + (int)Math.Floor(Score / Config.PointsPerLevel), 1, Config.MaxLevel);
            if (level == Level) return;
            Level = level;
            OnLevelChanged?.Invoke(Level);
        }
    }
}
