using System.Collections.Generic;
using Drowned.Core;
using Drowned.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>Painel do Monumento (SCRUM-170): etapa, barra, entregue/necessário, pendências e botão da obra.</summary>
    public sealed class MonumentPanelView : MonoBehaviour
    {
        private GameManager _game;
        private Text _title;
        private Image _fill;
        private Text _resources;
        private Text _missing;
        private Button _toggle;

        /// <summary>O painel inteiro (alvo da seta do tutorial, SCRUM-197).</summary>
        public RectTransform Panel { get; private set; }

        public void Build(Transform canvas, GameManager game)
        {
            _game = game;
            var panel = UiKit.Panel(canvas, "MonumentPanel", UiTheme.PanelBg);
            Panel = panel.rectTransform;
            UiKit.Anchor(panel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -76f), new Vector2(340f, 230f));
            var col = UiKit.Vertical(panel.transform, "Col", 6f);
            UiKit.Stretch(col.GetComponent<RectTransform>());

            UiKit.Label(col.transform, "Monumento Central", UiTheme.FontSmall, UiTheme.TextMuted);
            _title = UiKit.Label(col.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            _fill = UiKit.ProgressBar(col.transform, 14f, UiTheme.Amber);
            _resources = UiKit.Label(col.transform, "", UiTheme.FontSmall, UiTheme.Text);
            _missing = UiKit.Label(col.transform, "", UiTheme.FontSmall, UiTheme.Amber);
            _toggle = UiKit.Button(col.transform, "", () => _game.Monument.ToggleActive(), UiTheme.FontSmall);
            UiKit.Size(_toggle, height: 36f);

            game.Monument.OnProgressChanged += _ => Refresh();
            game.Monument.OnStageCompleted += _ => Refresh();
            game.Monument.OnActiveChanged += _ => Refresh();
            game.Monument.OnCompleted += Refresh;
            game.Population.OnPopulationChanged += _ => Refresh();
            game.Specialists.OnSpecialistUnlocked += _ => Refresh();
            game.Research.OnResearched += _ => Refresh();
            Refresh();

            if (game.Monument.StageCount == 0) panel.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            var m = _game.Monument;
            if (m.IsComplete)
            {
                _title.text = "Monumento restaurado!";
                _fill.fillAmount = 1f;
                _resources.text = "";
                _missing.text = "";
                _toggle.gameObject.SetActive(false);
                return;
            }
            var stage = m.CurrentStage;
            if (stage == null) return;

            _title.text = $"Etapa {m.CurrentStageIndex + 1}/{m.StageCount}: {stage.DisplayName}";
            _fill.fillAmount = m.Progress;

            var lines = new List<string>();
            for (int i = 0; i < ResourceTypeExtensions.Count; i++)
            {
                var type = (ResourceType)i;
                float need = stage.RequiredResources[type];
                if (need <= 0f) continue;
                lines.Add($"{type.Label()}: {Mathf.FloorToInt(m.Delivered[type])} / {Mathf.CeilToInt(need)}");
            }
            _resources.text = string.Join("   ", lines);

            var missing = m.MissingRequirements();
            _missing.text = missing.Count == 0 ? "" : "Falta: " + string.Join("; ", missing);

            _toggle.gameObject.SetActive(true);
            UiKit.ButtonText(_toggle).text = m.IsActive ? "Obra ligada (clique para pausar)" : "Ligar obra";
            UiKit.SetSelected(_toggle, m.IsActive);
        }
    }
}
