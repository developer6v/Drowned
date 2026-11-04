using System;
using System.Collections.Generic;
using Drowned.Population;
using Drowned.Research;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.Monument
{
    /// <summary>
    /// Restauração do Monumento Central em etapas (SCRUM-160). Lógica pura, testável.
    ///
    /// Com a obra ativa, a cada tick retira do estoque até <c>ContributionPerDay / TicksPerDay</c> de cada recurso
    /// (nunca mais do que há) e soma em <see cref="Delivered"/>. A etapa conclui quando os recursos foram entregues
    /// e população, especialistas e tecnologias atendem. Ao concluir a última, <see cref="OnCompleted"/> (vitória).
    /// </summary>
    public sealed class MonumentManager
    {
        /// <summary>Índice (0-based) da etapa concluída.</summary>
        public event Action<int> OnStageCompleted;
        public event Action OnCompleted;
        /// <summary>Entrega de recursos mudou. Para a barra de progresso.</summary>
        public event Action<float> OnProgressChanged;
        public event Action<bool> OnActiveChanged;

        public MonumentData Data { get; }
        public int TicksPerDay { get; }
        public int StageCount => Data != null ? Data.StageCount : 0;

        /// <summary>Índice da etapa em andamento (0-based). Igual a StageCount quando tudo está pronto.</summary>
        public int CurrentStageIndex { get; private set; }

        public MonumentStage CurrentStage => IsComplete || StageCount == 0 ? null : Data.Stages[CurrentStageIndex];
        public bool IsComplete => StageCount > 0 && CurrentStageIndex >= StageCount;

        /// <summary>A obra está recebendo recursos.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Recursos já entregues à etapa atual.</summary>
        public ResourceBundle Delivered { get; private set; }

        /// <summary>Fração dos recursos da etapa atual já entregues (0 a 1). 1 quando tudo concluído.</summary>
        public float Progress
        {
            get
            {
                if (IsComplete) return 1f;
                var stage = CurrentStage;
                if (stage == null) return 0f;
                float total = stage.RequiredResources.Total;
                return total <= 0f ? 1f : Mathf.Clamp01(Delivered.Total / total);
            }
        }

        /// <summary>Progresso geral: etapas concluídas + fração da atual, sobre o total de etapas.</summary>
        public float OverallProgress => StageCount == 0 ? 0f : Mathf.Clamp01((CurrentStageIndex + (IsComplete ? 0f : Progress)) / StageCount);

        /// <summary>Sprite a mostrar agora: ruína antes da primeira etapa, senão o da última etapa concluída.</summary>
        public Sprite CurrentSprite => Data == null ? null : CurrentStageIndex == 0 ? Data.RuinSprite : Data.Stages[CurrentStageIndex - 1].Sprite;

        private readonly ResourceManager _resources;
        private readonly PopulationManager _population;
        private readonly SpecialistManager _specialists;
        private readonly ResearchManager _research;

        public MonumentManager(MonumentData data, int ticksPerDay, ResourceManager resources, PopulationManager population, SpecialistManager specialists, ResearchManager research)
        {
            if (ticksPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerDay));
            Data = data;
            TicksPerDay = ticksPerDay;
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _population = population ?? throw new ArgumentNullException(nameof(population));
            _specialists = specialists ?? throw new ArgumentNullException(nameof(specialists));
            _research = research ?? throw new ArgumentNullException(nameof(research));

            _population.OnPopulationChanged += _ => TryComplete();
            _specialists.OnSpecialistUnlocked += _ => TryComplete();
            _research.OnResearched += _ => TryComplete();
        }

        public void SetActive(bool active)
        {
            if (IsComplete) active = false;
            if (active == IsActive) return;
            IsActive = active;
            OnActiveChanged?.Invoke(IsActive);
        }

        public void ToggleActive() => SetActive(!IsActive);

        /// <summary>Recursos que ainda faltam entregar na etapa atual.</summary>
        public ResourceBundle Remaining => CurrentStage == null ? ResourceBundle.Zero : (CurrentStage.RequiredResources - Delivered).ClampedToZero();

        public bool ResourcesDelivered => CurrentStage != null && Remaining.IsZero;

        /// <summary>Pendências que não são recurso, em português, para o painel. Vazio se tudo atendido.</summary>
        public List<string> MissingRequirements()
        {
            var list = new List<string>();
            var stage = CurrentStage;
            if (stage == null) return list;
            if (_population.Population < stage.MinPopulation)
                list.Add($"População {stage.MinPopulation} (atual {_population.Population})");
            foreach (var id in stage.RequiredSpecialistIds)
                if (!_specialists.IsUnlocked(id)) list.Add($"Especialista: {SpecialistName(id)}");
            foreach (var id in stage.RequiredTechnologyIds)
                if (!_research.IsResearched(id)) list.Add($"Tecnologia: {TechnologyName(id)}");
            return list;
        }

        public bool RequirementsMet => CurrentStage != null && MissingRequirements().Count == 0;

        /// <summary>Aplica um tick. Ligar em <c>GameClock.OnTick</c>.</summary>
        public void Tick()
        {
            if (!IsActive || IsComplete) return;
            var stage = CurrentStage;
            if (stage == null) return;

            var want = ResourceBundle.Min(stage.ContributionPerDay / TicksPerDay, Remaining);
            var take = ResourceBundle.Min(want, _resources.Stock).ClampedToZero();
            if (!take.IsZero && _resources.TrySpend(take, out _))
            {
                Delivered += take;
                OnProgressChanged?.Invoke(Progress);
            }
            TryComplete();
        }

        private void TryComplete()
        {
            if (IsComplete || CurrentStage == null) return;
            if (!ResourcesDelivered || !RequirementsMet) return;

            int done = CurrentStageIndex;
            CurrentStageIndex++;
            Delivered = ResourceBundle.Zero;
            OnStageCompleted?.Invoke(done);
            OnProgressChanged?.Invoke(Progress);
            if (IsComplete)
            {
                SetActive(false);
                OnCompleted?.Invoke();
            }
            else TryComplete(); // a próxima etapa pode já estar atendida (ex.: só exige população)
        }

        /// <summary>Restaura de um save. Não dispara eventos de conclusão.</summary>
        public void Restore(int currentStageIndex, ResourceBundle delivered, bool active)
        {
            CurrentStageIndex = Mathf.Clamp(currentStageIndex, 0, StageCount);
            Delivered = delivered.ClampedToZero();
            IsActive = active && !IsComplete;
            OnProgressChanged?.Invoke(Progress);
            OnActiveChanged?.Invoke(IsActive);
        }

        private string SpecialistName(string id)
        {
            foreach (var s in _specialists.All) if (s.Id == id) return s.DisplayName;
            return id;
        }

        private string TechnologyName(string id)
        {
            foreach (var t in _research.All) if (t.Id == id) return t.DisplayName;
            return id;
        }
    }
}
