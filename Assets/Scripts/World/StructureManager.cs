using System;
using System.Collections.Generic;
using Drowned.Population;
using Drowned.Resources;

namespace Drowned.World
{
    /// <summary>
    /// Único caminho para construir, melhorar e remover estruturas (SCRUM-130 / SCRUM-134). Lógica pura, testável.
    ///
    /// Amarra três sistemas: valida a posição no <see cref="GridMap"/>, cobra o custo no
    /// <see cref="ResourceManager"/>, e registra produção/consumo e capacidade de moradia.
    /// A ordem em <see cref="TryBuild"/> é sempre: posição, depois custo, e só então gasta e coloca.
    /// Nunca cobra sem colocar nem coloca sem cobrar.
    /// </summary>
    public sealed class StructureManager
    {
        public event Action<StructureInstance> OnStructureBuilt;
        /// <summary>Nível mudou. A view troca o sprite.</summary>
        public event Action<StructureInstance> OnStructureChanged;
        public event Action<StructureInstance> OnStructureRemoved;

        public GridMap Grid { get; }
        public ResourceManager Resources { get; }
        public PopulationManager Population { get; }

        /// <summary>Multiplicador aplicado a todo custo de construção e upgrade (desconto do Arquiteto, SCRUM-145). Default 1.</summary>
        public float CostMultiplier { get; private set; } = 1f;

        private readonly Dictionary<int, StructureInstance> _instances = new Dictionary<int, StructureInstance>();

        public StructureManager(GridMap grid, ResourceManager resources, PopulationManager population)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            Population = population ?? throw new ArgumentNullException(nameof(population));
        }

        public int Count => _instances.Count;
        public IEnumerable<StructureInstance> Instances => _instances.Values;
        public StructureInstance Get(int id) => _instances.TryGetValue(id, out var s) ? s : null;

        public int CountOf(StructureData data)
        {
            int n = 0;
            foreach (var s in _instances.Values) if (s.Data == data) n++;
            return n;
        }

        /// <summary>Soma da capacidade de moradia de todas as estruturas, no nível atual de cada uma.</summary>
        public int TotalHousingCapacity
        {
            get
            {
                int total = 0;
                foreach (var s in _instances.Values) total += s.CurrentLevel.HousingCapacity;
                return total;
            }
        }

        /// <summary>Troca o multiplicador de custo. Nunca abaixo de zero.</summary>
        public void SetCostMultiplier(float multiplier) => CostMultiplier = Math.Max(0f, multiplier);

        /// <summary>Custo de construção com o desconto atual. É o que a barra de construção mostra.</summary>
        public ResourceBundle BuildCostOf(StructureData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return data.BuildCost * CostMultiplier;
        }

        /// <summary>
        /// Verifica sem efeito colateral se dá para construir. Serve para o preview: posição em vermelho
        /// ou botão desabilitado por falta de recurso.
        /// </summary>
        public BuildResult CanBuild(StructureData data, int x, int y, out ResourceBundle missing)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            missing = ResourceBundle.Zero;
            if (!Grid.CanPlace(x, y, data.FootprintWidth, data.FootprintHeight)) return BuildResult.InvalidPosition;
            missing = Resources.Stock.Missing(BuildCostOf(data));
            return missing.IsZero ? BuildResult.Ok : BuildResult.InsufficientResources;
        }

        /// <summary>Constrói no nível 1 cobrando o custo. Em caso de falha nada muda.</summary>
        public BuildResult TryBuild(StructureData data, int x, int y, out StructureInstance instance, out ResourceBundle missing)
        {
            instance = null;
            var result = CanBuild(data, x, y, out missing);
            if (result != BuildResult.Ok) return result;

            if (!Resources.TrySpend(BuildCostOf(data), out missing)) return BuildResult.InsufficientResources;
            instance = PlaceAndRegister(data, x, y, level: 1);
            return BuildResult.Ok;
        }

        /// <summary>
        /// Coloca uma estrutura sem cobrar, já em um nível dado. Para as estruturas iniciais da partida
        /// (SCRUM-120) e para o load. Retorna null se a posição for inválida ou o nível não existir.
        /// </summary>
        public StructureInstance PlaceFree(StructureData data, int x, int y, int level = 1)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!data.HasLevel(level)) return null;
            if (!Grid.CanPlace(x, y, data.FootprintWidth, data.FootprintHeight)) return null;
            return PlaceAndRegister(data, x, y, level);
        }

        /// <summary>Remove a estrutura, libera o grid, a fonte de recursos e a moradia. Sem reembolso no MVP.</summary>
        public bool Remove(int id)
        {
            if (!_instances.TryGetValue(id, out var instance)) return false;
            Grid.Remove(id);
            Resources.RemoveSource(instance.ResourceSourceId);
            Population.AddHousingCapacity(-instance.CurrentLevel.HousingCapacity);
            _instances.Remove(id);
            OnStructureRemoved?.Invoke(instance);
            return true;
        }

        public bool CanUpgrade(int id) => CanUpgrade(id, out _) == BuildResult.Ok;

        /// <summary>Verifica sem efeito colateral se a estrutura pode subir de nível.</summary>
        public BuildResult CanUpgrade(int id, out ResourceBundle missing)
        {
            missing = ResourceBundle.Zero;
            if (!_instances.TryGetValue(id, out var instance)) return BuildResult.NotFound;
            if (instance.IsMaxLevel) return BuildResult.MaxLevel;
            missing = Resources.Stock.Missing(NextLevelCostOf(instance));
            return missing.IsZero ? BuildResult.Ok : BuildResult.InsufficientResources;
        }

        /// <summary>Custo do próximo nível com o desconto atual, para a HUD. Falso se não há próximo nível.</summary>
        public bool TryGetNextLevelCost(int id, out ResourceBundle cost)
        {
            cost = ResourceBundle.Zero;
            if (!_instances.TryGetValue(id, out var instance) || instance.IsMaxLevel) return false;
            cost = NextLevelCostOf(instance);
            return true;
        }

        private ResourceBundle NextLevelCostOf(StructureInstance instance) =>
            instance.Data.GetLevel(instance.Level + 1).Cost * CostMultiplier;

        /// <summary>Sobe um nível cobrando o custo do novo nível e trocando produção, consumo e moradia.</summary>
        public BuildResult TryUpgrade(int id, out ResourceBundle missing)
        {
            var result = CanUpgrade(id, out missing);
            if (result != BuildResult.Ok) return result;

            var instance = _instances[id];
            var previous = instance.CurrentLevel;
            var next = instance.Data.GetLevel(instance.Level + 1);
            if (!Resources.TrySpend(NextLevelCostOf(instance), out missing)) return BuildResult.InsufficientResources;

            instance.Level++;
            Register(instance);
            Population.AddHousingCapacity(next.HousingCapacity - previous.HousingCapacity);
            OnStructureChanged?.Invoke(instance);
            return BuildResult.Ok;
        }

        private StructureInstance PlaceAndRegister(StructureData data, int x, int y, int level)
        {
            int id = Grid.Place(x, y, data.FootprintWidth, data.FootprintHeight);
            var instance = new StructureInstance(id, data, level, x, y);
            _instances[id] = instance;
            Register(instance);
            Population.AddHousingCapacity(instance.CurrentLevel.HousingCapacity);
            OnStructureBuilt?.Invoke(instance);
            return instance;
        }

        private void Register(StructureInstance instance)
        {
            var level = instance.CurrentLevel;
            Resources.SetSource(instance.ResourceSourceId, level.ProductionPerDay, level.ConsumptionPerDay);
        }
    }
}
