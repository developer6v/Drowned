using Drowned.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.Monument
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="MonumentManager"/> (SCRUM-160 / SCRUM-161). O sistema em si é criado pelo
    /// <see cref="GameManager"/>. Coloque este componente no objeto do Monumento na cena, com um SpriteRenderer:
    /// o sprite troca a cada etapa concluída e, se houver um ParticleSystem no campo, ele é disparado.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonumentBehaviour : MonoBehaviour
    {
        public static MonumentBehaviour Instance { get; private set; }

        [Tooltip("Partículas disparadas ao concluir uma etapa (opcional).")]
        [SerializeField] private ParticleSystem _stageParticles;

        [Header("Eventos para a HUD")]
        public UnityEvent<int> onStageCompleted;
        public UnityEvent onCompleted;
        public UnityEvent<float> onProgressChanged;
        public UnityEvent<bool> onActiveChanged;

        public MonumentManager Monument { get; private set; }

        private SpriteRenderer _renderer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um MonumentBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _renderer = GetComponent<SpriteRenderer>();

            var game = GameManagerBehaviour.Instance != null ? GameManagerBehaviour.Instance.Game : null;
            if (game == null)
            {
                Debug.LogError("MonumentBehaviour: não há GameManagerBehaviour inicializado na cena.", this);
                enabled = false;
                return;
            }

            Monument = game.Monument;
            Monument.OnStageCompleted += HandleStageCompleted;
            Monument.OnCompleted += HandleCompleted;
            Monument.OnProgressChanged += HandleProgress;
            Monument.OnActiveChanged += HandleActive;
            RefreshSprite();
        }

        public void RefreshSprite()
        {
            if (_renderer == null || Monument == null) return;
            var sprite = Monument.CurrentSprite;
            if (sprite != null) _renderer.sprite = sprite;
        }

        private void HandleStageCompleted(int index)
        {
            RefreshSprite();
            if (_stageParticles != null) _stageParticles.Play();
            onStageCompleted?.Invoke(index);
        }

        private void HandleCompleted() => onCompleted?.Invoke();
        private void HandleProgress(float progress) => onProgressChanged?.Invoke(progress);
        private void HandleActive(bool active) => onActiveChanged?.Invoke(active);

        private void OnDestroy()
        {
            if (Monument != null)
            {
                Monument.OnStageCompleted -= HandleStageCompleted;
                Monument.OnCompleted -= HandleCompleted;
                Monument.OnProgressChanged -= HandleProgress;
                Monument.OnActiveChanged -= HandleActive;
            }
            if (Instance == this) Instance = null;
        }
    }
}
