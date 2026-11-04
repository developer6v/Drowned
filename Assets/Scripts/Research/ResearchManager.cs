using System;
using System.Collections.Generic;
using System.Linq;
using Drowned.Population;
using Drowned.Resources;
using Drowned.World;

namespace Drowned.Research
{
    /// <summary>
    /// Pontos de pesquisa por tick e compra de tecnologias (SCRUM-147), e o que elas liberam (SCRUM-148).
    /// Lógica pura, testável.
    ///
    /// Laboratórios registram <c>ResearchPerDay</c> como fonte (este sistema assina os eventos do
    /// <see cref="StructureManager"/> para isso). A cada tick os pontos vão para a tecnologia em progresso;
    /// sem tecnologia escolhida, ficam guardados em <see cref="StoredPoints"/>.
    /// </summary>
    public sealed class ResearchManager : IUnlockRules
    {
        public event Action<TechnologyData> OnResearched;
        public event Action<TechnologyData> OnCurrentChanged;
        /// <summary>Progresso da tecnologia atual mudou (0 a 1).</summary>
        public event Action<float> OnProgressChanged;
        public event Action OnRatesChanged;

        public IReadOnlyList<TechnologyData> All { get; }
        public IReadOnlyList<TechnologyData> Researched => _researched;
        public TechnologyData Current { get; private set; }

        /// <summary>Pontos já investidos na tecnologia atual.</summary>
        public float CurrentPoints { get; private set; }

        /// <summary>Pontos acumulados sem tecnologia em progresso. Entram na próxima escolhida.</summary>
        public float StoredPoints { get; private set; }

        public float Progress => Current == null ? 0f : Math.Min(1f, CurrentPoints / Current.Cost);

        public int TicksPerDay { get; }

        /// <summary>Soma de ResearchPerDay dos laboratórios, sem bônus.</summary>
        public float PointsPerDay { get; private set; }

        /// <summary>Multiplicador vindo de fora (Cientista, SCRUM-145). Default 1.</summary>
        public float BaseSpeedMultiplier { get; private set; } = 1f;

        /// <summary>Fração somada pelas tecnologias com ResearchBonus.</summary>
        public float TechnologyBonus { get; private set; }

        public float SpeedMultiplier => BaseSpeedMultiplier + TechnologyBonus;
        public float EffectivePointsPerDay => PointsPerDay * SpeedMultiplier;

        private readonly List<TechnologyData> _researched = new List<TechnologyData>();
        private readonly HashSet<string> _researchedIds = new HashSet<string>();
        private readonly HashSet<string> _unlockedStructures = new HashSet<string>();
        private readonly Dictionary<string, int> _unlockedLevels = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _sources = new Dictionary<string, float>();
        private readonly ResourceManager _resources;
        private readonly PopulationManager _population;
        private readonly StructureManager _structures;

        public ResearchManager(IEnumerable<TechnologyData> technologies, int ticksPerDay, ResourceManager resources, PopulationManager population, StructureManager structures)
        {
            if (technologies == null) throw new ArgumentNullException(nameof(technologies));
            if (ticksPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerDay));
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _population = population ?? throw new ArgumentNullException(nameof(population));
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));
            TicksPerDay = ticksPerDay;
            All = technologies.Where(t => t != null).ToList();

            _structures.OnStructureBuilt += HandleStructureChanged;
            _structures.OnStructureChanged += HandleStructureChanged;
            _structures.OnStructureRemoved += HandleStructureRemoved;
        }

        // ------------------------------------------------------------------ fontes

        public void SetSource(string id, float pointsPerDay)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Id da fonte não pode ser vazio.", nameof(id));
            _sources[id] = Math.Max(0f, pointsPerDay);
            RecomputeRates();
        }

        public bool RemoveSource(string id)
        {
            if (!_sources.Remove(id)) return false;
            RecomputeRates();
            return true;
        }

        public void SetBaseSpeedMultiplier(float multiplier)
        {
            BaseSpeedMultiplier = Math.Max(0f, multiplier);
            OnRatesChanged?.Invoke();
        }

        private void HandleStructureChanged(StructureInstance instance)
        {
            float perDay = instance.CurrentLevel.ResearchPerDay;
            if (perDay > 0f) SetSource(instance.ResourceSourceId, perDay);
            else RemoveSource(instance.ResourceSourceId);
        }

        private void HandleStructureRemoved(StructureInstance instance) => RemoveSource(instance.ResourceSourceId);

        private void RecomputeRates()
        {
            float sum = 0f;
            foreach (var v in _sources.Values) sum += v;
            PointsPerDay = sum;
            OnRatesChanged?.Invoke();
        }

        // ------------------------------------------------------------------ pesquisa

        public bool IsResearched(string id) => _researchedIds.Contains(id);
        public bool IsResearched(TechnologyData tech) => tech != null && _researchedIds.Contains(tech.Id);

        /// <summary>Verdadeiro se pode ser escolhida agora: não pesquisada, não é a atual e pré-requisitos prontos.</summary>
        public bool CanResearch(TechnologyData tech)
        {
            if (tech == null || IsResearched(tech) || tech == Current) return false;
            foreach (var p in tech.Prerequisites)
                if (p != null && !IsResearched(p)) return false;
            return true;
        }

        /// <summary>Tecnologias que podem ser escolhidas agora, na ordem da lista.</summary>
        public IEnumerable<TechnologyData> Available => All.Where(CanResearch);

        /// <summary>Escolhe a tecnologia em progresso. Pontos guardados entram na hora. Falso se não pode.</summary>
        public bool StartResearch(TechnologyData tech)
        {
            if (!CanResearch(tech)) return false;
            if (Current != null) Cancel();

            Current = tech;
            CurrentPoints = 0f;
            OnCurrentChanged?.Invoke(Current);
            if (StoredPoints > 0f)
            {
                float stored = StoredPoints;
                StoredPoints = 0f;
                AddPoints(stored);
            }
            else OnProgressChanged?.Invoke(Progress);
            return true;
        }

        /// <summary>Desiste da tecnologia atual; os pontos investidos voltam para o banco.</summary>
        public void Cancel()
        {
            if (Current == null) return;
            StoredPoints += CurrentPoints;
            Current = null;
            CurrentPoints = 0f;
            OnCurrentChanged?.Invoke(null);
            OnProgressChanged?.Invoke(0f);
        }

        /// <summary>Aplica um tick. Ligar em <c>GameClock.OnTick</c>.</summary>
        public void Tick() => AddPoints(EffectivePointsPerDay / TicksPerDay);

        private void AddPoints(float points)
        {
            if (points <= 0f) return;
            if (Current == null)
            {
                StoredPoints += points;
                return;
            }

            CurrentPoints += points;
            if (CurrentPoints < Current.Cost)
            {
                OnProgressChanged?.Invoke(Progress);
                return;
            }

            float excess = CurrentPoints - Current.Cost;
            var done = Current;
            Current = null;
            CurrentPoints = 0f;
            StoredPoints += excess;
            Complete(done, raiseEvents: true);
            OnCurrentChanged?.Invoke(null);
            OnProgressChanged?.Invoke(0f);
        }

        private void Complete(TechnologyData tech, bool raiseEvents)
        {
            _researched.Add(tech);
            _researchedIds.Add(tech.Id);
            foreach (var e in tech.Effects) Apply(e);
            if (raiseEvents) OnResearched?.Invoke(tech);
        }

        private void Apply(TechnologyEffect e)
        {
            switch (e.Type)
            {
                case TechnologyEffectType.UnlockStructure:
                    if (!string.IsNullOrEmpty(e.StructureId)) _unlockedStructures.Add(e.StructureId);
                    break;
                case TechnologyEffectType.UnlockLevel:
                    if (string.IsNullOrEmpty(e.StructureId)) break;
                    _unlockedLevels.TryGetValue(e.StructureId, out int current);
                    _unlockedLevels[e.StructureId] = Math.Max(current, e.Level);
                    break;
                case TechnologyEffectType.ProductionBonus:
                    var m = _resources.ProductionMultiplier;
                    _resources.SetProductionMultiplier(m.With(e.Resource, m[e.Resource] + e.Value));
                    break;
                case TechnologyEffectType.ResearchBonus:
                    TechnologyBonus += e.Value;
                    OnRatesChanged?.Invoke();
                    break;
                case TechnologyEffectType.HousingBonus:
                    _population.SetHousingMultiplier(_population.HousingMultiplier + e.Value);
                    break;
            }
        }

        // ------------------------------------------------------------------ IUnlockRules

        public bool IsStructureUnlocked(StructureData data) =>
            data != null && (!data.RequiresTechnology || _unlockedStructures.Contains(data.Id));

        public bool IsLevelUnlocked(StructureData data, int level)
        {
            if (data == null) return false;
            _unlockedLevels.TryGetValue(data.Id, out int unlocked);
            return level <= Math.Max(data.BaseMaxLevel, unlocked);
        }

        // ------------------------------------------------------------------ save

        /// <summary>Restaura de um save, aplicando efeitos sem disparar eventos de conclusão.</summary>
        public void Restore(IEnumerable<string> researchedIds, string currentId, float currentPoints, float storedPoints)
        {
            if (researchedIds == null) throw new ArgumentNullException(nameof(researchedIds));
            foreach (var id in researchedIds)
            {
                var tech = All.FirstOrDefault(t => t.Id == id);
                if (tech != null && !IsResearched(tech)) Complete(tech, raiseEvents: false);
            }
            Current = string.IsNullOrEmpty(currentId) ? null : All.FirstOrDefault(t => t.Id == currentId && !IsResearched(t));
            CurrentPoints = Current != null ? Math.Max(0f, currentPoints) : 0f;
            StoredPoints = Math.Max(0f, storedPoints);
            OnCurrentChanged?.Invoke(Current);
            OnProgressChanged?.Invoke(Progress);
        }
    }
}
