using Drowned.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>Tela de vitória (SCRUM-181) e derrota (SCRUM-182) com o resumo de GameStats.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        private GameManager _game;
        private GameObject _root;
        private Text _title;
        private Text _reason;
        private Text _summary;
        private Button _again;

        public void Build(Transform canvas, GameManager game)
        {
            _game = game;
            var overlay = UiKit.Panel(canvas, "EndScreen", new Color(0.01f, 0.04f, 0.10f, 0.92f));
            _root = overlay.gameObject;
            UiKit.Stretch(overlay.rectTransform);

            var box = UiKit.Panel(overlay.transform, "Box", UiTheme.PanelBgSolid);
            UiKit.Center(box.rectTransform, new Vector2(640f, 640f));
            var col = UiKit.Vertical(box.transform, "Col", 12f, new RectOffset(32, 32, 28, 28), TextAnchor.UpperCenter);
            UiKit.Stretch(col.GetComponent<RectTransform>());

            _title = UiKit.Label(col.transform, "", UiTheme.FontTitle, UiTheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _reason = UiKit.Label(col.transform, "", UiTheme.FontNormal, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            _summary = UiKit.Label(col.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.UpperLeft);
            UiKit.Size(_summary, flexibleWidth: 1f).flexibleHeight = 1f;
            _again = UiKit.Button(col.transform, "", SceneFlow.ReloadCurrent);
            UiKit.Size(_again, height: 48f);
            UiKit.Size(UiKit.Button(col.transform, "Menu principal", SceneFlow.LoadMainMenu), height: 48f);

            game.OnStateChanged += Show;
            _root.SetActive(false);
        }

        private void Show(GameState state)
        {
            if (state == GameState.Playing) return;
            bool victory = state == GameState.Victory;
            _title.text = victory ? "Monumento restaurado!" : "A cidade colapsou";
            _title.color = victory ? UiTheme.Amber : UiTheme.Danger;
            _reason.text = victory
                ? "A comunidade submarina está salva."
                : $"{_game.Collapse.ConsecutiveDays} dias seguidos sem energia ou oxigênio.";

            var sb = new System.Text.StringBuilder();
            foreach (var (label, value) in _game.Stats.Summary()) sb.AppendLine($"{label}: {value}");
            _summary.text = sb.ToString();
            UiKit.ButtonText(_again).text = victory ? "Jogar de novo" : "Tentar de novo";
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }
    }
}
