using Drowned.Core;
using Drowned.Resources;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.Population
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="PopulationManager"/> (SCRUM-132 / SCRUM-133).
    /// Coloque um único objeto "PopulationManager" na cena. Precisa de <see cref="GameClockBehaviour"/>
    /// e <see cref="ResourceManagerBehaviour"/> na cena.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class PopulationManagerBehaviour : MonoBehaviour
    {
        public static PopulationManagerBehaviour Instance { get; private set; }

        [Header("Números (aba População da planilha)")]
        [SerializeField] private PopulationConfig _config = new PopulationConfig();

        [Header("Eventos para a HUD")]
        public UnityEvent<int> onPopulationChanged;
        public UnityEvent<int> onHousingCapacityChanged;

        public PopulationManager Population { get; private set; }

        private GameClock _clock;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um PopulationManagerBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var clockBehaviour = GameClockBehaviour.Instance;
            var resourcesBehaviour = ResourceManagerBehaviour.Instance;
            if (clockBehaviour == null || resourcesBehaviour == null || resourcesBehaviour.Resources == null)
            {
                Debug.LogError("PopulationManagerBehaviour: precisa de GameClockBehaviour e ResourceManagerBehaviour na cena.", this);
                enabled = false;
                return;
            }
            _clock = clockBehaviour.Clock;

            Population = new PopulationManager(_config, resourcesBehaviour.Resources);
            Population.OnPopulationChanged += p => onPopulationChanged?.Invoke(p);
            Population.OnHousingCapacityChanged += c => onHousingCapacityChanged?.Invoke(c);
        }

        private void OnEnable()
        {
            if (_clock != null) _clock.OnNewDay += HandleNewDay;
        }

        private void OnDisable()
        {
            if (_clock != null) _clock.OnNewDay -= HandleNewDay;
        }

        private void HandleNewDay(int day) => Population.AdvanceDay();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
