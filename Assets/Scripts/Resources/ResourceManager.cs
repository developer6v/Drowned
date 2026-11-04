using System;
using System.Collections.Generic;

namespace Drowned.Resources
{
    /// <summary>
    /// Estoque de energia, oxigênio e alimento e sua variação por tick (SCRUM-131). Lógica pura, testável.
    ///
    /// Quem produz ou consome (estruturas, população) registra uma <b>fonte</b> por id com valores
    /// <b>por dia</b>, como na planilha. A cada <see cref="Tick"/> o estoque recebe
    /// <c>(produção - consumo) / TicksPerDay</c> e nunca fica negativo.
    /// </summary>
    public sealed class ResourceManager
    {
        /// <summary>Estoque mudou (por tick, gasto ou carga de save).</summary>
        public event Action<ResourceBundle> OnStockChanged;

        /// <summary>Um recurso chegou a zero. Base dos alertas de crise e da checagem de colapso.</summary>
        public event Action<ResourceType> OnDepleted;

        /// <summary>Um recurso que estava zerado voltou a ficar positivo.</summary>
        public event Action<ResourceType> OnRestored;

        /// <summary>Produção ou consumo total mudou (fonte registrada, alterada ou removida). Para a HUD atualizar a taxa.</summary>
        public event Action OnRatesChanged;

        public ResourceBundle Stock { get; private set; }
        public int TicksPerDay { get; }

        /// <summary>Soma da produção de todas as fontes, por dia.</summary>
        public ResourceBundle ProductionPerDay { get; private set; }

        /// <summary>Soma do consumo de todas as fontes, por dia.</summary>
        public ResourceBundle ConsumptionPerDay { get; private set; }

        /// <summary>Saldo por dia. É o que a barra superior mostra ao lado do valor atual.</summary>
        public ResourceBundle NetPerDay => ProductionPerDay - ConsumptionPerDay;

        /// <summary>Saldo aplicado a cada tick.</summary>
        public ResourceBundle NetPerTick => NetPerDay / TicksPerDay;

        public int SourceCount => _sources.Count;

        private readonly Dictionary<string, (ResourceBundle production, ResourceBundle consumption)> _sources =
            new Dictionary<string, (ResourceBundle, ResourceBundle)>();

        private readonly bool[] _depleted = new bool[ResourceTypeExtensions.Count];

        /// <param name="initialStock">Estoque de começo de partida (aba Inicial da planilha, SCRUM-120).</param>
        /// <param name="ticksPerDay">Mesmo valor do GameClock.</param>
        public ResourceManager(ResourceBundle initialStock, int ticksPerDay)
        {
            if (ticksPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerDay), "Deve ser maior que zero.");
            TicksPerDay = ticksPerDay;
            Stock = initialStock.ClampedToZero();
            for (int i = 0; i < _depleted.Length; i++) _depleted[i] = Stock[(ResourceType)i] <= 0f;
        }

        public bool IsDepleted(ResourceType type) => Stock[type] <= 0f;

        /// <summary>Verdadeiro se energia, oxigênio e alimento estão todos acima de zero.</summary>
        public bool AllPositive => !IsDepleted(ResourceType.Energy) && !IsDepleted(ResourceType.Oxygen) && !IsDepleted(ResourceType.Food);

        /// <summary>Registra ou atualiza uma fonte de produção/consumo (valores por dia).</summary>
        public void SetSource(string id, ResourceBundle productionPerDay, ResourceBundle consumptionPerDay)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Id da fonte não pode ser vazio.", nameof(id));
            _sources[id] = (productionPerDay, consumptionPerDay);
            RecomputeRates();
        }

        public bool RemoveSource(string id)
        {
            if (!_sources.Remove(id)) return false;
            RecomputeRates();
            return true;
        }

        public bool HasSource(string id) => _sources.ContainsKey(id);

        public bool TryGetSource(string id, out ResourceBundle productionPerDay, out ResourceBundle consumptionPerDay)
        {
            if (_sources.TryGetValue(id, out var s))
            {
                productionPerDay = s.production;
                consumptionPerDay = s.consumption;
                return true;
            }
            productionPerDay = ResourceBundle.Zero;
            consumptionPerDay = ResourceBundle.Zero;
            return false;
        }

        /// <summary>Aplica um tick de simulação. Ligar em <c>GameClock.OnTick</c>.</summary>
        public void Tick() => ApplyStock(Stock + NetPerTick);

        public bool CanAfford(ResourceBundle cost) => Stock.Covers(cost);

        /// <summary>
        /// Debita <paramref name="cost"/> se houver saldo. Se não houver, não debita nada e devolve
        /// em <paramref name="missing"/> quanto falta de cada recurso (para a mensagem de erro).
        /// </summary>
        public bool TrySpend(ResourceBundle cost, out ResourceBundle missing)
        {
            missing = Stock.Missing(cost);
            if (!missing.IsZero) return false;
            ApplyStock(Stock - cost);
            return true;
        }

        /// <summary>Adiciona ao estoque (reembolso, evento, bônus).</summary>
        public void Add(ResourceBundle amount) => ApplyStock(Stock + amount);

        /// <summary>Substitui o estoque. Usado pelo save/load.</summary>
        public void SetStock(ResourceBundle stock) => ApplyStock(stock);

        private void ApplyStock(ResourceBundle next)
        {
            next = next.ClampedToZero();
            if (next == Stock) return;
            Stock = next;
            OnStockChanged?.Invoke(Stock);

            for (int i = 0; i < _depleted.Length; i++)
            {
                var type = (ResourceType)i;
                bool depletedNow = Stock[type] <= 0f;
                if (depletedNow == _depleted[i]) continue;
                _depleted[i] = depletedNow;
                if (depletedNow) OnDepleted?.Invoke(type);
                else OnRestored?.Invoke(type);
            }
        }

        private void RecomputeRates()
        {
            var prod = ResourceBundle.Zero;
            var cons = ResourceBundle.Zero;
            foreach (var s in _sources.Values)
            {
                prod += s.production;
                cons += s.consumption;
            }
            ProductionPerDay = prod;
            ConsumptionPerDay = cons;
            OnRatesChanged?.Invoke();
        }
    }
}
