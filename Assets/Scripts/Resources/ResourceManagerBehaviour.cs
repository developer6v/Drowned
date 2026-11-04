using Drowned.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.Resources
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="ResourceManager"/> (SCRUM-131).
    /// Coloque um único objeto "ResourceManager" na cena. Precisa de um <see cref="GameClockBehaviour"/> na cena.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class ResourceManagerBehaviour : MonoBehaviour
    {
        public static ResourceManagerBehaviour Instance { get; private set; }

        [Header("Estoque inicial (SCRUM-120) — valor final vem da aba Inicial da planilha")]
        [SerializeField] private ResourceBundle _initialStock;

        [Header("Eventos para a HUD e alertas")]
        public UnityEvent<ResourceBundle> onStockChanged;
        public UnityEvent<ResourceType> onDepleted;
        public UnityEvent<ResourceType> onRestored;
        public UnityEvent onRatesChanged;

        public ResourceManager Resources { get; private set; }

        private GameClock _clock;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um ResourceManagerBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var clockBehaviour = GameClockBehaviour.Instance;
            if (clockBehaviour == null)
            {
                Debug.LogError("ResourceManagerBehaviour: não há GameClockBehaviour na cena.", this);
                enabled = false;
                return;
            }
            _clock = clockBehaviour.Clock;

            Resources = new ResourceManager(_initialStock, _clock.TicksPerDay);
            Resources.OnStockChanged += stock => onStockChanged?.Invoke(stock);
            Resources.OnDepleted += type => onDepleted?.Invoke(type);
            Resources.OnRestored += type => onRestored?.Invoke(type);
            Resources.OnRatesChanged += () => onRatesChanged?.Invoke();
        }

        private void OnEnable()
        {
            if (_clock != null) _clock.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            if (_clock != null) _clock.OnTick -= HandleTick;
        }

        private void HandleTick(int tickOfDay) => Resources.Tick();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
