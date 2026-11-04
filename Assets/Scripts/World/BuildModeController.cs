using System;
using Drowned.Core;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Modo construção (SCRUM-129): a barra inferior chama <see cref="Select"/>, um fantasma segue o mouse no grid
    /// (verde onde pode construir, vermelho onde não), clique esquerdo constrói, botão direito ou Esc cancela.
    /// Continua no modo depois de construir, para colocar várias seguidas.
    /// Coloque no mesmo objeto do GameManager ou em um objeto "BuildMode". Precisa do GameManagerBehaviour.
    /// </summary>
    public sealed class BuildModeController : MonoBehaviour
    {
        public static BuildModeController Instance { get; private set; }

        [Tooltip("Câmera do jogo. Se vazio, usa Camera.main.")]
        [SerializeField] private Camera _camera;

        [SerializeField] private Color _okColor = new Color(0.4f, 1f, 0.5f, 0.6f);
        [SerializeField] private Color _blockedColor = new Color(1f, 0.35f, 0.3f, 0.6f);
        [SerializeField] private string _sortingLayer = "Default";

        /// <summary>Estrutura selecionada mudou (null = saiu do modo).</summary>
        public event Action<StructureData> OnSelectionChanged;
        public event Action<StructureInstance> OnBuilt;
        /// <summary>Tentativa falhou: resultado e, se for recurso, quanto falta. Para mensagem e som de erro.</summary>
        public event Action<BuildResult, ResourceBundle> OnBuildFailed;

        public StructureData Selected { get; private set; }
        public bool IsActive => Selected != null;

        /// <summary>Célula do canto inferior esquerdo da pegada sob o mouse, atualizada a cada frame no modo.</summary>
        public Vector2Int Anchor { get; private set; }
        public BuildResult LastCheck { get; private set; } = BuildResult.InvalidPosition;

        private GameManager _game;
        private TilemapMapBehaviour _map;
        private SpriteRenderer _ghost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var gm = GameManagerBehaviour.Instance;
            if (gm == null || gm.Game == null)
            {
                Debug.LogError("BuildModeController: não há GameManagerBehaviour inicializado na cena.", this);
                enabled = false;
                return;
            }
            _game = gm.Game;
            _map = gm.Map;
            if (_camera == null) _camera = Camera.main;

            var go = new GameObject("BuildGhost");
            go.transform.SetParent(transform, false);
            _ghost = go.AddComponent<SpriteRenderer>();
            _ghost.sortingLayerName = _sortingLayer;
            _ghost.sortingOrder = StructureManagerBehaviour.SortingBase + 10000;
            _ghost.enabled = false;

            _game.OnStateChanged += _ => Cancel();
        }

        public void Select(StructureData data)
        {
            if (data == null || _game == null || _game.IsOver)
            {
                Cancel();
                return;
            }
            if (Selected == data) return;
            Selected = data;
            _ghost.sprite = data.GetLevel(1).Sprite;
            _ghost.enabled = true;
            _map.SetBuildableOverlayVisible(true);
            OnSelectionChanged?.Invoke(Selected);
        }

        /// <summary>Seleciona, ou cancela se já era a selecionada (clique repetido no botão).</summary>
        public void Toggle(StructureData data)
        {
            if (Selected == data) Cancel();
            else Select(data);
        }

        public void Cancel()
        {
            if (Selected == null) return;
            Selected = null;
            if (_ghost != null) _ghost.enabled = false;
            if (_map != null) _map.SetBuildableOverlayVisible(false);
            OnSelectionChanged?.Invoke(null);
        }

        private void Update()
        {
            if (!IsActive) return;
            if (GameInput.RightClickDown || GameInput.EscapeDown)
            {
                Cancel();
                return;
            }

            UpdateGhost();

            if (GameInput.LeftClickDown && !GameInput.IsPointerOverUI) TryBuildAtAnchor();
        }

        private void UpdateGhost()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 world = _camera.ScreenToWorldPoint(GameInput.MousePosition);
            world.z = 0f;
            Vector2Int cell = _map.WorldToGrid(world);
            // A célula sob o mouse fica no centro da pegada
            Anchor = new Vector2Int(cell.x - (Selected.FootprintWidth - 1) / 2, cell.y - (Selected.FootprintHeight - 1) / 2);

            LastCheck = _game.Structures.CanBuild(Selected, Anchor.x, Anchor.y, out _);
            _ghost.color = LastCheck == BuildResult.Ok ? _okColor : _blockedColor;
            _ghost.transform.position = FootprintCenter(Anchor, Selected.FootprintWidth, Selected.FootprintHeight);
            _ghost.sortingOrder = StructureManagerBehaviour.SortingBase + 10000;
        }

        /// <summary>Centro no mundo de uma pegada no grid. Também usado pelos sprites das estruturas.</summary>
        public Vector3 FootprintCenter(Vector2Int anchor, int width, int height)
        {
            Vector3 a = _map.GridToWorldCenter(anchor.x, anchor.y);
            Vector3 b = _map.GridToWorldCenter(anchor.x + width - 1, anchor.y + height - 1);
            return (a + b) * 0.5f;
        }

        private void TryBuildAtAnchor()
        {
            var result = _game.Structures.TryBuild(Selected, Anchor.x, Anchor.y, out var instance, out var missing);
            if (result == BuildResult.Ok) OnBuilt?.Invoke(instance);
            else OnBuildFailed?.Invoke(result, missing);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
