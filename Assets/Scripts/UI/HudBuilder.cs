using Drowned.Core;
using Drowned.Save;
using Drowned.World;
using UnityEngine;

namespace Drowned.UI
{
    /// <summary>
    /// Monta toda a HUD por código ao iniciar (SCRUM-140/141/142/156/157/170/171/179/181/182).
    /// Coloque em um objeto "HUD" na cena de jogo. Precisa do GameManagerBehaviour, BuildModeController e
    /// StructureSelector na cena. Os ícones dos recursos (SCRUM-127) vão no campo, na ordem energia, oxigênio, alimento.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class HudBuilder : MonoBehaviour
    {
        [Tooltip("Ícones na ordem: energia, oxigênio, alimento.")]
        [SerializeField] private Sprite[] _resourceIcons = new Sprite[3];

        public Canvas Canvas { get; private set; }
        public ToastView Toast { get; private set; }
        public ResearchPanelView ResearchPanel { get; private set; }
        public SpecialistPanelView SpecialistPanel { get; private set; }
        public PauseMenuView PauseMenu { get; private set; }

        private GameManager _game;

        /// <summary>Define os ícones por código (bootstrap da demo).</summary>
        public void Configure(Sprite[] resourceIcons) => _resourceIcons = resourceIcons ?? new Sprite[3];

        private void Start()
        {
            var gm = GameManagerBehaviour.Instance;
            if (gm == null || gm.Game == null)
            {
                Debug.LogError("HudBuilder: não há GameManagerBehaviour inicializado na cena.", this);
                enabled = false;
                return;
            }
            _game = gm.Game;
            var build = BuildModeController.Instance;
            var selector = StructureSelector.Instance;
            if (build == null || selector == null)
            {
                Debug.LogError("HudBuilder: precisa de BuildModeController e StructureSelector na cena.", this);
                enabled = false;
                return;
            }

            Canvas = UiKit.CreateCanvas("HUD");
            Canvas.transform.SetParent(transform, false);
            var root = Canvas.transform;

            // Ordem de criação = ordem de desenho (último por cima)
            ResearchPanel = Add<ResearchPanelView>("ResearchPanel");
            SpecialistPanel = Add<SpecialistPanelView>("SpecialistPanel");

            Add<ResourceBarView>("ResourceBar").Build(root, _game, _resourceIcons, ResearchPanel.Toggle, SpecialistPanel.Toggle);
            Add<PopulationView>("Population").Build(root, _game);
            Add<MonumentPanelView>("MonumentPanel").Build(root, _game);
            Add<BuildBarView>("BuildBar").Build(root, _game, build, selector);
            ResearchPanel.Build(root, _game);
            SpecialistPanel.Build(root, _game);
            Add<CrisisAlertView>("CrisisAlert").Build(root, _game);
            Toast = Add<ToastView>("Toasts");
            Toast.Build(root);
            PauseMenu = Add<PauseMenuView>("PauseMenu");
            PauseMenu.Build(root, _game, Toast);
            Add<EndScreenView>("EndScreen").Build(root, _game);

            // Mensagens rápidas
            build.OnBuildFailed += (result, missing) =>
                Toast.Show(result == BuildResult.InsufficientResources ? $"Recursos insuficientes. Falta: {BuildBarView.CostText(missing)}" : result.Message(), UiTheme.Danger);
            selector.OnActionFailed += (result, missing) =>
                Toast.Show(result == BuildResult.InsufficientResources ? $"Recursos insuficientes. Falta: {BuildBarView.CostText(missing)}" : result.Message(), UiTheme.Danger);
            _game.Specialists.OnSpecialistUnlocked += s => Toast.Show($"Novo especialista: {s.DisplayName} ({s.BonusText})", UiTheme.Amber, 4f);
            _game.Research.OnResearched += t => Toast.Show($"Tecnologia concluída: {t.DisplayName}", UiTheme.Positive, 4f);
            _game.Monument.OnStageCompleted += i => Toast.Show($"Etapa do Monumento concluída: {_game.Monument.Data.Stages[i].DisplayName}", UiTheme.Amber, 4f);
            _game.Population.OnPopulationChanged += _ => { };
        }

        private T Add<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private void Update()
        {
            if (_game == null || _game.IsOver) return;
            if (GameInput.F5Down)
            {
                SaveSystem.Save(_game, 1);
                Toast?.Show("Partida salva (F5)", UiTheme.Positive);
            }
        }
    }
}
