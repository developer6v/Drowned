using System.Collections.Generic;
using System.Linq;
using Drowned.Core;
using Drowned.Research;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>Painel de pesquisa (SCRUM-156): lista de tecnologias com estado, custo, efeitos; escolher e cancelar.</summary>
    public sealed class ResearchPanelView : MonoBehaviour
    {
        private sealed class Row
        {
            public TechnologyData Tech;
            public Button Button;
            public Text Title;
            public Text Info;
            public Image Fill;
        }

        private GameManager _game;
        private GameObject _root;
        private Text _header;
        private readonly List<Row> _rows = new List<Row>();

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Build(Transform canvas, GameManager game)
        {
            _game = game;
            var panel = UiKit.Panel(canvas, "ResearchPanel", UiTheme.PanelBgSolid);
            _root = panel.gameObject;
            UiKit.Center(panel.rectTransform, new Vector2(760f, 720f));

            var col = UiKit.Vertical(panel.transform, "Col", 8f, new RectOffset(16, 16, 12, 12));
            UiKit.Stretch(col.GetComponent<RectTransform>());

            var top = UiKit.Horizontal(col.transform, "Top", 8f, new RectOffset(0, 0, 0, 0));
            UiKit.Label(top.transform, "Pesquisa", UiTheme.FontLarge, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            var sp = UiKit.Group(top.transform, "Spacer");
            UiKit.Size(sp, flexibleWidth: 1f);
            UiKit.Size(UiKit.Button(top.transform, "Fechar", Close, UiTheme.FontSmall), width: 100f, height: 36f);

            _header = UiKit.Label(col.transform, "", UiTheme.FontSmall, UiTheme.TextMuted);

            // Lista com rolagem simples (ScrollRect)
            var viewport = UiKit.Panel(col.transform, "Viewport", new Color(0f, 0f, 0f, 0.2f));
            UiKit.Size(viewport, flexibleWidth: 1f);
            var vle = viewport.GetComponent<LayoutElement>();
            vle.flexibleHeight = 1f;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = UiKit.Vertical(viewport.transform, "Content", 6f, new RectOffset(8, 8, 8, 8));
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(0f, 0f);
            crt.offsetMax = new Vector2(0f, 0f);
            UiKit.FitHeight(content);
            scroll.content = crt;
            scroll.viewport = viewport.rectTransform;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;

            foreach (var tech in game.Research.All)
            {
                var t = tech;
                var rowPanel = UiKit.Panel(content.transform, "Row", UiTheme.PanelBg);
                var rowH = UiKit.Horizontal(rowPanel.transform, "H", 10f, new RectOffset(10, 10, 8, 8));
                UiKit.Stretch(rowH.GetComponent<RectTransform>());
                UiKit.FitHeight(rowPanel);

                var textCol = UiKit.Vertical(rowH.transform, "Text", 2f, new RectOffset(0, 0, 0, 0));
                UiKit.Size(textCol, flexibleWidth: 1f);
                var title = UiKit.Label(textCol.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                var info = UiKit.Label(textCol.transform, "", UiTheme.FontSmall, UiTheme.TextMuted);
                var fill = UiKit.ProgressBar(textCol.transform, 6f);

                var btn = UiKit.Button(rowH.transform, "", () => OnRowClick(t), UiTheme.FontSmall);
                UiKit.Size(btn, width: 130f, height: 40f);
                _rows.Add(new Row { Tech = t, Button = btn, Title = title, Info = info, Fill = fill });
            }
            if (game.Research.All.Count == 0)
                UiKit.Label(content.transform, "Nenhuma tecnologia cadastrada (SCRUM-149).", UiTheme.FontNormal, UiTheme.TextMuted, TextAnchor.MiddleCenter);

            game.Research.OnResearched += _ => Refresh();
            game.Research.OnCurrentChanged += _ => Refresh();
            game.Research.OnProgressChanged += _ => RefreshProgress();
            game.Research.OnRatesChanged += RefreshHeader;
            Refresh();
            _root.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
        }

        public void Close() => _root.SetActive(false);
        public void Toggle() { if (IsOpen) Close(); else Open(); }

        private void OnRowClick(TechnologyData tech)
        {
            var r = _game.Research;
            if (r.Current == tech) r.Cancel();
            else r.StartResearch(tech);
            Refresh();
        }

        private void RefreshHeader()
        {
            var r = _game.Research;
            _header.text = $"{r.EffectivePointsPerDay:0.#} pontos/dia  (x{r.SpeedMultiplier:0.##})   Guardados: {r.StoredPoints:0}";
        }

        private void RefreshProgress()
        {
            var r = _game.Research;
            foreach (var row in _rows) row.Fill.fillAmount = row.Tech == r.Current ? r.Progress : r.IsResearched(row.Tech) ? 1f : 0f;
        }

        private void Refresh()
        {
            RefreshHeader();
            var r = _game.Research;
            foreach (var row in _rows)
            {
                var t = row.Tech;
                bool done = r.IsResearched(t);
                bool current = r.Current == t;
                bool can = r.CanResearch(t);
                string effects = string.Join("; ", t.Effects.Select(e => e.Describe(StructureName)));
                string prereq = t.Prerequisites.Count == 0 ? "" : "  Requer: " + string.Join(", ", t.Prerequisites.Where(p => p != null).Select(p => p.DisplayName));
                row.Title.text = $"{t.DisplayName}  ({t.Cost:0} pts)";
                row.Title.color = done ? UiTheme.Positive : current ? UiTheme.Amber : can ? UiTheme.Text : UiTheme.TextMuted;
                row.Info.text = effects + prereq;
                var bt = UiKit.ButtonText(row.Button);
                bt.text = done ? "Pesquisada" : current ? "Cancelar" : can ? "Pesquisar" : "Bloqueada";
                row.Button.interactable = current || can;
                UiKit.SetSelected(row.Button, current);
            }
            RefreshProgress();
        }

        private string StructureName(string id)
        {
            var s = _game.Rules.FindStructure(id);
            return s != null ? s.DisplayName : id;
        }
    }
}
