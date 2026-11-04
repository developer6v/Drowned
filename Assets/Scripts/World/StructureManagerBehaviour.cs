using System.Collections.Generic;
using Drowned.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.World
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="StructureManager"/> (SCRUM-130 / SCRUM-134). O sistema em si é criado
    /// pelo <see cref="GameManager"/>. Este componente cria um GameObject com SpriteRenderer para cada
    /// estrutura e troca o sprite no upgrade. O preview de colocação e a entrada do mouse ficam na SCRUM-129.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public sealed class StructureManagerBehaviour : MonoBehaviour
    {
        public static StructureManagerBehaviour Instance { get; private set; }

        [Tooltip("Pai dos sprites das estruturas. Se vazio, usa este transform.")]
        [SerializeField] private Transform _structuresParent;

        [Tooltip("Sorting layer dos sprites de estrutura.")]
        [SerializeField] private string _sortingLayer = "Default";

        [Header("Eventos para a HUD")]
        public UnityEvent<StructureInstance> onStructureBuilt;
        public UnityEvent<StructureInstance> onStructureChanged;
        public UnityEvent<StructureInstance> onStructureRemoved;

        public StructureManager Structures { get; private set; }

        private TilemapMapBehaviour _map;
        private readonly Dictionary<int, SpriteRenderer> _views = new Dictionary<int, SpriteRenderer>();

        /// <summary>SpriteRenderer de uma estrutura, para efeitos e seleção. Null se não existe.</summary>
        public SpriteRenderer GetView(int structureId) => _views.TryGetValue(structureId, out var r) ? r : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um StructureManagerBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var gm = GameManagerBehaviour.Instance;
            if (gm == null || gm.Game == null)
            {
                Debug.LogError("StructureManagerBehaviour: não há GameManagerBehaviour inicializado na cena.", this);
                enabled = false;
                return;
            }

            _map = gm.Map;
            Structures = gm.Game.Structures;
            Structures.OnStructureBuilt += HandleBuilt;
            Structures.OnStructureChanged += HandleChanged;
            Structures.OnStructureRemoved += HandleRemoved;
        }

        private void HandleBuilt(StructureInstance instance)
        {
            var go = new GameObject($"{instance.Data.Id}#{instance.Id}");
            go.transform.SetParent(_structuresParent != null ? _structuresParent : transform, worldPositionStays: false);
            // Centro da pegada (para 2x2 fica entre as quatro células)
            Vector3 a = _map.GridToWorldCenter(instance.X, instance.Y);
            Vector3 b = _map.GridToWorldCenter(instance.X + instance.Data.FootprintWidth - 1, instance.Y + instance.Data.FootprintHeight - 1);
            go.transform.position = (a + b) * 0.5f;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = _sortingLayer;
            // Em isométrico, quem está mais "abaixo" na tela (y menor) desenha por cima.
            renderer.sortingOrder = -(instance.X + instance.Y);
            renderer.sprite = instance.CurrentLevel.Sprite;
            _views[instance.Id] = renderer;

            onStructureBuilt?.Invoke(instance);
        }

        private void HandleChanged(StructureInstance instance)
        {
            if (_views.TryGetValue(instance.Id, out var renderer)) renderer.sprite = instance.CurrentLevel.Sprite;
            onStructureChanged?.Invoke(instance);
        }

        private void HandleRemoved(StructureInstance instance)
        {
            if (_views.TryGetValue(instance.Id, out var renderer))
            {
                Destroy(renderer.gameObject);
                _views.Remove(instance.Id);
            }
            onStructureRemoved?.Invoke(instance);
        }

        private void OnDestroy()
        {
            if (Structures != null)
            {
                Structures.OnStructureBuilt -= HandleBuilt;
                Structures.OnStructureChanged -= HandleChanged;
                Structures.OnStructureRemoved -= HandleRemoved;
            }
            if (Instance == this) Instance = null;
        }
    }
}
