using Drowned.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.Resources
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="ResourceManager"/> (SCRUM-131). O sistema em si é criado pelo
    /// <see cref="GameManager"/>; este componente só expõe os eventos para a HUD ligar pelo inspetor.
    /// Coloque um único objeto "ResourceManager" na cena, depois do GameManager.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class ResourceManagerBehaviour : MonoBehaviour
    {
        public static ResourceManagerBehaviour Instance { get; private set; }

        [Header("Eventos para a HUD e alertas")]
        public UnityEvent<ResourceBundle> onStockChanged;
        public UnityEvent<ResourceType> onDepleted;
        public UnityEvent<ResourceType> onRestored;
        public UnityEvent onRatesChanged;

        public ResourceManager Resources { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um ResourceManagerBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var game = GameManagerBehaviour.Instance != null ? GameManagerBehaviour.Instance.Game : null;
            if (game == null)
            {
                Debug.LogError("ResourceManagerBehaviour: não há GameManagerBehaviour inicializado na cena.", this);
                enabled = false;
                return;
            }

            Resources = game.Resources;
            Resources.OnStockChanged += HandleStockChanged;
            Resources.OnDepleted += HandleDepleted;
            Resources.OnRestored += HandleRestored;
            Resources.OnRatesChanged += HandleRatesChanged;
        }

        private void HandleStockChanged(ResourceBundle stock) => onStockChanged?.Invoke(stock);
        private void HandleDepleted(ResourceType type) => onDepleted?.Invoke(type);
        private void HandleRestored(ResourceType type) => onRestored?.Invoke(type);
        private void HandleRatesChanged() => onRatesChanged?.Invoke();

        private void OnDestroy()
        {
            if (Resources != null)
            {
                Resources.OnStockChanged -= HandleStockChanged;
                Resources.OnDepleted -= HandleDepleted;
                Resources.OnRestored -= HandleRestored;
                Resources.OnRatesChanged -= HandleRatesChanged;
            }
            if (Instance == this) Instance = null;
        }
    }
}
