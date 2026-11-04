using Drowned.Core;
using Drowned.Save;
using Drowned.World;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>Menu de pausa (SCRUM-179): Esc abre/fecha; Continuar, Salvar, Carregar, Menu principal, Sair.</summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        private GameManager _game;
        private GameObject _root;
        private Button _loadButton;
        private bool _wasPaused;
        private ToastView _toast;

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Build(Transform canvas, GameManager game, ToastView toast)
        {
            _game = game;
            _toast = toast;
            var overlay = UiKit.Panel(canvas, "PauseMenu", new Color(0f, 0f, 0f, 0.55f));
            _root = overlay.gameObject;
            UiKit.Stretch(overlay.rectTransform);

            var box = UiKit.Panel(overlay.transform, "Box", UiTheme.PanelBgSolid);
            UiKit.Center(box.rectTransform, new Vector2(420f, 420f));
            var col = UiKit.Vertical(box.transform, "Col", 10f, new RectOffset(24, 24, 20, 20), TextAnchor.UpperCenter);
            UiKit.Stretch(col.GetComponent<RectTransform>());

            UiKit.Label(col.transform, "Pausado", UiTheme.FontLarge, UiTheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Size(UiKit.Button(col.transform, "Continuar", Close), height: 48f);
            UiKit.Size(UiKit.Button(col.transform, "Salvar", () =>
            {
                SaveSystem.Save(_game, 1);
                _toast?.Show("Partida salva!", UiTheme.Positive);
                RefreshLoad();
            }), height: 48f);
            _loadButton = UiKit.Button(col.transform, "Carregar", () =>
            {
                var data = SaveSystem.Load(1);
                if (data == null) return;
                SaveSystem.PendingLoad = data;
                SceneFlow.ReloadCurrent();
            });
            UiKit.Size(_loadButton, height: 48f);
            UiKit.Size(UiKit.Button(col.transform, "Menu principal", SceneFlow.LoadMainMenu), height: 48f);
            UiKit.Size(UiKit.Button(col.transform, "Sair do jogo", SceneFlow.Quit), height: 48f);

            game.OnStateChanged += _ => { if (IsOpen) Close(); };
            _root.SetActive(false);
            RefreshLoad();
        }

        private void RefreshLoad()
        {
            if (_loadButton == null) return;
            bool has = SaveSystem.HasSave(1);
            _loadButton.interactable = has;
            var at = has ? SaveSystem.SavedAt(1) : null;
            UiKit.ButtonText(_loadButton).text = at.HasValue ? $"Carregar ({at.Value:dd/MM HH:mm})" : "Carregar (sem save)";
        }

        public void Open()
        {
            if (_game.IsOver || IsOpen) return;
            _wasPaused = _game.Clock.IsPaused;
            if (!_wasPaused) _game.Clock.SetSpeed(GameSpeed.Paused);
            RefreshLoad();
            _root.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _root.SetActive(false);
            if (!_wasPaused && _game.Clock.IsPaused) _game.Clock.TogglePause();
        }

        private void Update()
        {
            if (_game == null || _game.IsOver) return;
            if (!GameInput.EscapeDown) return;
            if (IsOpen)
            {
                Close();
                return;
            }
            // Esc com modo construção ou seleção ativa é consumido por eles.
            var build = BuildModeController.Instance;
            if (build != null && build.IsActive) return;
            var selector = StructureSelector.Instance;
            if (selector != null && selector.Selected != null) return;
            Open();
        }
    }
}
