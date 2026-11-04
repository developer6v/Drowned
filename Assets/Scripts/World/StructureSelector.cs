using System;
using Drowned.Core;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Clique em uma estrutura já construída para abrir o painel de melhorar/remover (SCRUM-134 / SCRUM-141).
    /// Ignora cliques no modo construção e sobre a UI. Botão direito ou Esc limpa a seleção.
    /// </summary>
    public sealed class StructureSelector : MonoBehaviour
    {
        public static StructureSelector Instance { get; private set; }

        [Tooltip("Câmera do jogo. Se vazio, usa Camera.main.")]
        [SerializeField] private Camera _camera;

        [SerializeField] private Color _highlight = new Color(1f, 0.9f, 0.5f, 1f);

        /// <summary>Seleção mudou (null = nada selecionado).</summary>
        public event Action<StructureInstance> OnSelected;
        /// <summary>Upgrade ou remoção falhou. Para mensagem e som de erro.</summary>
        public event Action<BuildResult, ResourceBundle> OnActionFailed;

        public StructureInstance Selected { get; private set; }

        private GameManager _game;
        private TilemapMapBehaviour _map;
        private StructureManagerBehaviour _views;
        private Color _originalColor = Color.white;

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
                Debug.LogError("StructureSelector: não há GameManagerBehaviour inicializado na cena.", this);
                enabled = false;
                return;
            }
            _game = gm.Game;
            _map = gm.Map;
            _views = StructureManagerBehaviour.Instance;
            if (_camera == null) _camera = Camera.main;

            _game.Structures.OnStructureRemoved += removed => { if (Selected == removed) Select(null); };
            _game.Structures.OnStructureChanged += changed => { if (Selected == changed) OnSelected?.Invoke(Selected); };
        }

        private void Update()
        {
            if (_game == null) return;
            var build = BuildModeController.Instance;
            if (build != null && build.IsActive) return;

            if (GameInput.RightClickDown || GameInput.EscapeDown)
            {
                Select(null);
                return;
            }
            if (!GameInput.LeftClickDown || GameInput.IsPointerOverUI) return;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            Vector3 world = _camera.ScreenToWorldPoint(GameInput.MousePosition);
            Vector2Int cell = _map.WorldToGrid(world);
            int id = _game.Grid.OccupantAt(cell.x, cell.y);
            Select(id != 0 ? _game.Structures.Get(id) : null);
        }

        public void Select(StructureInstance instance)
        {
            if (Selected == instance) return;
            SetHighlight(Selected, false);
            Selected = instance;
            SetHighlight(Selected, true);
            OnSelected?.Invoke(Selected);
        }

        /// <summary>Melhora a estrutura selecionada. Falso se não há seleção ou falhou (evento informa o motivo).</summary>
        public bool UpgradeSelected()
        {
            if (Selected == null) return false;
            var result = _game.Structures.TryUpgrade(Selected.Id, out var missing);
            if (result == BuildResult.Ok) return true;
            OnActionFailed?.Invoke(result, missing);
            return false;
        }

        /// <summary>Remove a estrutura selecionada (sem reembolso no MVP).</summary>
        public bool RemoveSelected()
        {
            if (Selected == null) return false;
            int id = Selected.Id;
            return _game.Structures.Remove(id);
        }

        private void SetHighlight(StructureInstance instance, bool on)
        {
            if (instance == null || _views == null) return;
            var view = _views.GetView(instance.Id);
            if (view == null) return;
            if (on)
            {
                _originalColor = view.color;
                view.color = _highlight;
            }
            else view.color = _originalColor;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
