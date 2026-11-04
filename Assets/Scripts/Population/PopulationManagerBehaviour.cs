using Drowned.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.Population
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="PopulationManager"/> (SCRUM-132 / SCRUM-133). O sistema em si é criado
    /// pelo <see cref="GameManager"/>, que também chama a regra diária; aqui só os eventos para a HUD.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class PopulationManagerBehaviour : MonoBehaviour
    {
        public static PopulationManagerBehaviour Instance { get; private set; }

        [Header("Eventos para a HUD")]
        public UnityEvent<int> onPopulationChanged;
        public UnityEvent<int> onHousingCapacityChanged;

        public PopulationManager Population { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um PopulationManagerBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var game = GameManagerBehaviour.Instance != null ? GameManagerBehaviour.Instance.Game : null;
            if (game == null)
            {
                Debug.LogError("PopulationManagerBehaviour: não há GameManagerBehaviour inicializado na cena.", this);
                enabled = false;
                return;
            }

            Population = game.Population;
            Population.OnPopulationChanged += HandlePopulationChanged;
            Population.OnHousingCapacityChanged += HandleHousingChanged;
        }

        private void HandlePopulationChanged(int population) => onPopulationChanged?.Invoke(population);
        private void HandleHousingChanged(int capacity) => onHousingCapacityChanged?.Invoke(capacity);

        private void OnDestroy()
        {
            if (Population != null)
            {
                Population.OnPopulationChanged -= HandlePopulationChanged;
                Population.OnHousingCapacityChanged -= HandleHousingChanged;
            }
            if (Instance == this) Instance = null;
        }
    }
}
