using System.Collections.Generic;
using Drowned.Population;
using Drowned.Resources;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.World
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="StructureManager"/> (SCRUM-130 / SCRUM-134).
    /// Cria um GameObject com SpriteRenderer para cada estrutura construída e troca o sprite no upgrade.
    /// O preview de colocação e a entrada do mouse ficam na SCRUM-129.
    ///
    /// Na cena: um objeto "StructureManager" com este componente, arraste o Grid (com
    /// <see cref="TilemapMapBehaviour"/>) no campo. Precisa de ResourceManagerBehaviour e
    /// PopulationManagerBehaviour na cena. Usa Start() porque o TilemapMapBehaviour monta o GridMap no Awake.
    /// </summary>
    public sealed class StructureManagerBehaviour : MonoBehaviour
    {
        public static StructureManagerBehaviour Instance { get; private set; }

        [SerializeField] private TilemapMapBehaviour _map;

        [Tooltip("Pai dos sprites das estruturas. Se vazio, usa este transform.")]
        [SerializeField] private Transform _structuresParent;

        [Tooltip("Sorting layer dos sprites de estrutura.")]
        [SerializeField] private string _sortingLayer = "Default";

        [Header("Eventos para a HUD")]
        public UnityEvent<StructureInstance> onStructureBuilt;
        public UnityEvent<StructureInstance> onStructureChanged;
        public UnityEvent<StructureInstance> onStructureRemoved;

        public StructureManager Structures { get; private set; }

        private readonly Dictionary<int, SpriteRenderer> _views = new Dictionary<int, SpriteRenderer>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um StructureManagerBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var resources = ResourceManagerBehaviour.Instance;
            var population = PopulationManagerBehaviour.Instance;
            if (_map == null || _map.Map == null || resources == null || population == null)
            {
                Debug.LogError("StructureManagerBehaviour: precisa do TilemapMapBehaviour no campo Map e de ResourceManagerBehaviour e PopulationManagerBehaviour na cena.", this);
                enabled = false;
                return;
            }

            Structures = new StructureManager(_map.Map, resources.Resources, population.Population);
            Structures.OnStructureBuilt += HandleBuilt;
            Structures.OnStructureChanged += HandleChanged;
            Structures.OnStructureRemoved += HandleRemoved;
        }

        private void HandleBuilt(StructureInstance instance)
        {
            var go = new GameObject($"{instance.Data.Id}#{instance.Id}");
            go.transform.SetParent(_structuresParent != null ? _structuresParent : transform, worldPositionStays: false);
            go.transform.position = _map.GridToWorldCenter(instance.X, instance.Y);

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
            if (Instance == this) Instance = null;
        }
    }
}
