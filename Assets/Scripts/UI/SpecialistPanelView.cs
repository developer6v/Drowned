using System.Collections.Generic;
using Drowned.Core;
using Drowned.Population;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>Painel de especialistas (SCRUM-157): ícone, nome, bônus e estado.</summary>
    public sealed class SpecialistPanelView : MonoBehaviour
    {
        private sealed class Row
        {
            public SpecialistData Data;
            public Text Title;
            public Text Info;
            public Image Icon;
        }

        private GameManager _game;
        private GameObject _root;
        private readonly List<Row> _rows = new List<Row>();

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Build(Transform canvas, GameManager game)
        {
            _game = game;
            var panel = UiKit.Panel(canvas, "SpecialistPanel", UiTheme.PanelBgSolid);
            _root = panel.gameObject;
            UiKit.Center(panel.rectTransform, new Vector2(620f, 520f));

            var col = UiKit.Vertical(panel.transform, "Col", 8f, new RectOffset(16, 16, 12, 12));
            UiKit.Stretch(col.GetComponent<RectTransform>());
            var top = UiKit.Horizontal(col.transform, "Top", 8f, new RectOffset(0, 0, 0, 0));
            UiKit.Label(top.transform, "Especialistas", UiTheme.FontLarge, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            var sp = UiKit.Group(top.transform, "Spacer");
            UiKit.Size(sp, flexibleWidth: 1f);
            UiKit.Size(UiKit.Button(top.transform, "Fechar", Close, UiTheme.FontSmall), width: 100f, height: 36f);

            foreach (var s in game.Specialists.All)
            {
                var rowPanel = UiKit.Panel(col.transform, "Row", UiTheme.PanelBg);
                var h = UiKit.Horizontal(rowPanel.transform, "H", 12f, new RectOffset(10, 10, 8, 8));
                UiKit.Stretch(h.GetComponent<RectTransform>());
                UiKit.FitHeight(rowPanel);
                var icon = UiKit.Icon(h.transform, s.Icon != null ? s.Icon : GameIcons.Specialist(s.Id), 48f);
                var textCol = UiKit.Vertical(h.transform, "Text", 2f, new RectOffset(0, 0, 0, 0));
                UiKit.Size(textCol, flexibleWidth: 1f);
                var title = UiKit.Label(textCol.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                var info = UiKit.Label(textCol.transform, "", UiTheme.FontSmall, UiTheme.TextMuted);
                _rows.Add(new Row { Data = s, Title = title, Info = info, Icon = icon });
            }
            if (game.Specialists.All.Count == 0)
                UiKit.Label(col.transform, "Nenhum especialista cadastrado (SCRUM-150).", UiTheme.FontNormal, UiTheme.TextMuted, TextAnchor.MiddleCenter);

            game.Specialists.OnSpecialistUnlocked += _ => Refresh();
            game.Population.OnPopulationChanged += _ => Refresh();
            Refresh();
            _root.SetActive(false);
        }

        public void Open() { Refresh(); _root.SetActive(true); }
        public void Close() => _root.SetActive(false);
        public void Toggle() { if (IsOpen) Close(); else Open(); }

        private void Refresh()
        {
            foreach (var row in _rows)
            {
                bool unlocked = _game.Specialists.IsUnlocked(row.Data.Id);
                row.Title.text = row.Data.DisplayName + (unlocked ? "  (desbloqueado)" : $"  (aos {row.Data.PopulationThreshold} habitantes)");
                row.Title.color = unlocked ? UiTheme.Positive : UiTheme.TextMuted;
                row.Info.text = row.Data.BonusText + (string.IsNullOrEmpty(row.Data.Description) ? "" : "\n" + row.Data.Description);
                var c = row.Icon.color;
                c.a = unlocked ? 1f : 0.35f;
                row.Icon.color = c;
            }
        }
    }
}
