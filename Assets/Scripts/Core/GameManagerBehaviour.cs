using Drowned.Save;
using Drowned.World;
using UnityEngine;
using UnityEngine.Events;

namespace Drowned.Core
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="GameManager"/> (SCRUM-175). Um único objeto "GameManager" na cena,
    /// com o asset <see cref="GameRules"/> e o Grid (com <see cref="TilemapMapBehaviour"/>) arrastados.
    /// Roda logo depois do <see cref="GameClockBehaviour"/>; os outros Behaviours pegam os sistemas em <see cref="Game"/>.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public sealed class GameManagerBehaviour : MonoBehaviour
    {
        public static GameManagerBehaviour Instance { get; private set; }

        [SerializeField] private GameRules _rules;
        [SerializeField] private TilemapMapBehaviour _map;

        [Header("Eventos para as telas de fim de jogo")]
        public UnityEvent<GameState> onStateChanged;

        public GameManager Game { get; private set; }
        public TilemapMapBehaviour Map => _map;

        /// <summary>Define regras e mapa por código (bootstrap da demo). Chame antes do Awake (objeto inativo).</summary>
        public void Configure(GameRules rules, TilemapMapBehaviour map)
        {
            _rules = rules;
            _map = map;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um GameManagerBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var clockBehaviour = GameClockBehaviour.Instance;
            if (clockBehaviour == null || _rules == null || _map == null)
            {
                Debug.LogError("GameManagerBehaviour: precisa de GameClockBehaviour na cena e dos campos Rules e Map preenchidos.", this);
                enabled = false;
                return;
            }

            _map.EnsureBuilt();
            if (_map.Map == null)
            {
                Debug.LogError("GameManagerBehaviour: o TilemapMapBehaviour não conseguiu montar o GridMap (Tilemap Ground vazio?).", this);
                enabled = false;
                return;
            }

            Game = new GameManager(_rules, clockBehaviour.Clock, _map.Map);
            Game.OnStateChanged += state => onStateChanged?.Invoke(state);
        }

        private void Start()
        {
            if (Game == null) return;
            // Depois do Awake de todos: os Behaviours já assinaram OnStructureBuilt e vão criar os sprites.
            var pending = SaveSystem.PendingLoad;
            if (pending != null)
            {
                SaveSystem.PendingLoad = null;
                SaveSystem.Apply(Game, pending);
            }
            else Game.PlaceInitialStructures();
        }

        /// <summary>Grava a partida atual no slot. Falso se a partida já acabou ou não há jogo.</summary>
        public bool SaveGame(int slot = 1)
        {
            if (Game == null || Game.IsOver) return false;
            SaveSystem.Save(Game, slot);
            return true;
        }

        private void OnDestroy()
        {
            Game?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
