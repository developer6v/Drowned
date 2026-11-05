using System.Collections.Generic;
using Drowned.Core;
using Drowned.Resources;
using Drowned.World;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>
    /// Barra inferior de construção (SCRUM-141): botões por categoria com custo, cadeado quando bloqueado,
    /// custo em vermelho sem saldo, destaque na selecionada; botão da obra do Monumento; painel da
    /// estrutura selecionada no mapa com Melhorar/Remover (SCRUM-134).
    /// </summary>
    public sealed class BuildBarView : MonoBehaviour
    {
        private sealed class Entry
        {
            public StructureData Data;
            public Button Button;
            public Text Text;
        }

        private GameManager _game;
        private BuildModeController _build;
        private StructureSelector _selector;
        private readonly List<Entry> _entries = new List<Entry>();
        private Button _monumentButton;

        // painel da estrutura selecionada
        private Image _structurePanel;
        private Text _structureTitle;
        private Text _structureInfo;
        private Button _upgradeButton;
        private Button _removeButton;

        public void Build(Transform canvas, GameManager game, BuildModeController build, StructureSelector selector)
        {
            _game = game;
            _build = build;
            _selector = selector;

            var panel = UiKit.Flat(UiKit.Panel(canvas, "BuildBar", UiTheme.PanelBg), keepBorder: true);
            UiKit.BottomStretch(panel.rectTransform, 112f);
            var row = UiKit.Horizontal(panel.transform, "Row", 18f, new RectOffset(16, 16, 8, 8));
            UiKit.Stretch(row.GetComponent<RectTransform>());

            var categories = new[] { StructureCategory.Housing, StructureCategory.Food, StructureCategory.Energy, StructureCategory.Oxygen, StructureCategory.Research };
            foreach (var category in categories)
            {
                var group = UiKit.Vertical(row.transform, category.ToString(), 2f, new RectOffset(0, 0, 0, 0));
                UiKit.Label(group.transform, category.Label(), UiTheme.FontSmall, UiTheme.TextMuted, TextAnchor.MiddleCenter);
                var buttons = UiKit.Horizontal(group.transform, "Buttons", 6f, new RectOffset(0, 0, 0, 0));
                bool any = false;
                foreach (var data in game.Rules.Structures)
                {
                    if (data == null || data.Category != category) continue;
                    any = true;
                    var d = data;
                    var btn = UiKit.Button(buttons.transform, "", () => _build.Toggle(d), UiTheme.FontSmall);
                    UiKit.Size(btn, width: 190f, height: 68f);
                    UiKit.AddButtonIcon(btn, GameIcons.Structure(d.Id), 44f);
                    _entries.Add(new Entry { Data = d, Button = btn, Text = UiKit.ButtonText(btn) });
                }
                if (!any) UiKit.Label(buttons.transform, "(nenhuma)", UiTheme.FontSmall, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            }

            var spacer = UiKit.Group(row.transform, "Spacer");
            UiKit.Size(spacer, flexibleWidth: 1f);

            if (game.Monument.StageCount > 0)
            {
                _monumentButton = UiKit.Button(row.transform, "", () => _game.Monument.ToggleActive(), UiTheme.FontSmall);
                UiKit.Size(_monumentButton, width: 230f, height: 68f);
                UiKit.AddButtonIcon(_monumentButton, GameIcons.Monument, 44f);
                game.Monument.OnActiveChanged += _ => RefreshMonument();
                game.Monument.OnCompleted += RefreshMonument;
                RefreshMonument();
            }

            BuildStructurePanel(canvas);

            game.Resources.OnStockChanged += _ => RefreshButtons();
            game.Research.OnResearched += _ => RefreshButtons();
            game.Specialists.OnSpecialistUnlocked += _ => RefreshButtons();
            _build.OnSelectionChanged += _ => RefreshButtons();
            _selector.OnSelected += RefreshStructurePanel;
            RefreshButtons();
            RefreshStructurePanel(null);
        }

        private void BuildStructurePanel(Transform canvas)
        {
            _structurePanel = UiKit.Panel(canvas, "StructurePanel", UiTheme.PanelBgSolid);
            UiKit.Anchor(_structurePanel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 124f), new Vector2(360f, 190f));
            var col = UiKit.Vertical(_structurePanel.transform, "Col", 6f);
            UiKit.Stretch(col.GetComponent<RectTransform>());
            _structureTitle = UiKit.Label(col.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            _structureInfo = UiKit.Label(col.transform, "", UiTheme.FontSmall, UiTheme.TextMuted);
            var buttons = UiKit.Horizontal(col.transform, "Buttons", 8f, new RectOffset(0, 0, 4, 0));
            _upgradeButton = UiKit.Button(buttons.transform, "Melhorar", () =>
            {
                if (_selector.UpgradeSelected()) RefreshStructurePanel(_selector.Selected);
            }, UiTheme.FontSmall);
            UiKit.Size(_upgradeButton, width: 160f, height: 40f);
            _removeButton = UiKit.Button(buttons.transform, "Remover", () => _selector.RemoveSelected(), UiTheme.FontSmall);
            UiKit.Size(_removeButton, width: 120f, height: 40f);
            _structurePanel.gameObject.SetActive(false);
        }

        public static string CostText(ResourceBundle cost)
        {
            var parts = new List<string>(3);
            if (cost.Energy > 0f) parts.Add($"E {Mathf.CeilToInt(cost.Energy)}");
            if (cost.Oxygen > 0f) parts.Add($"O {Mathf.CeilToInt(cost.Oxygen)}");
            if (cost.Food > 0f) parts.Add($"A {Mathf.CeilToInt(cost.Food)}");
            return parts.Count == 0 ? "grátis" : string.Join("  ", parts);
        }

        private void RefreshButtons()
        {
            foreach (var e in _entries)
            {
                bool unlocked = _game.Structures.IsUnlocked(e.Data);
                var cost = _game.Structures.BuildCostOf(e.Data);
                bool affordable = _game.Resources.CanAfford(cost);
                e.Button.interactable = unlocked;
                string costText = unlocked ? CostText(cost) : "bloqueado (tecnologia)";
                string color = !unlocked ? "#A0B0C0" : affordable ? "#E6F0FF" : "#F24D40";
                e.Text.text = $"{e.Data.DisplayName}\n<color={color}>{costText}</color>";
                e.Text.supportRichText = true;
                UiKit.SetSelected(e.Button, _build.Selected == e.Data);
            }
        }

        private void RefreshMonument()
        {
            if (_monumentButton == null) return;
            var m = _game.Monument;
            var t = UiKit.ButtonText(_monumentButton);
            if (m.IsComplete)
            {
                t.text = "Monumento\nrestaurado";
                _monumentButton.interactable = false;
                return;
            }
            t.text = m.IsActive ? "Obra do Monumento\nLIGADA" : "Obra do Monumento\ndesligada";
            UiKit.SetSelected(_monumentButton, m.IsActive);
        }

        private void RefreshStructurePanel(StructureInstance instance)
        {
            if (instance == null)
            {
                _structurePanel.gameObject.SetActive(false);
                return;
            }
            _structurePanel.gameObject.SetActive(true);
            var level = instance.CurrentLevel;
            _structureTitle.text = $"{instance.Data.DisplayName}  (nível {instance.Level}/{instance.Data.LevelCount})";
            var lines = new List<string>();
            if (!level.ProductionPerDay.IsZero) lines.Add($"Produz: {CostText(level.ProductionPerDay)} /dia");
            if (!level.ConsumptionPerDay.IsZero) lines.Add($"Consome: {CostText(level.ConsumptionPerDay)} /dia");
            if (level.HousingCapacity > 0) lines.Add($"Moradia: {level.HousingCapacity}");
            if (level.ResearchPerDay > 0f) lines.Add($"Pesquisa: {level.ResearchPerDay:0.#} /dia");

            var canUp = _game.Structures.CanUpgrade(instance.Id, out _);
            if (_game.Structures.TryGetNextLevelCost(instance.Id, out var next))
                lines.Add($"Próximo nível: {CostText(next)}" + (canUp == BuildResult.Locked ? " (requer tecnologia)" : ""));
            else lines.Add("Nível máximo");
            _structureInfo.text = string.Join("\n", lines);

            _upgradeButton.interactable = canUp == BuildResult.Ok;
            UiKit.ButtonText(_upgradeButton).text = canUp == BuildResult.Locked ? "Bloqueado" : canUp == BuildResult.MaxLevel ? "Máximo" : "Melhorar";
        }
    }
}
