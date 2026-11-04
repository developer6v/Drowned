using Drowned.Core;
using Drowned.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>
    /// Barra superior (SCRUM-140): os 3 recursos com valor e taxa por dia, dia atual, relógio do dia
    /// e botões de velocidade. Atualiza por eventos; só o relógio do dia é lido por frame.
    /// </summary>
    public sealed class ResourceBarView : MonoBehaviour
    {
        private sealed class Entry
        {
            public ResourceType Type;
            public Text Value;
            public Text Rate;
            public Image Icon;
        }

        private GameManager _game;
        private readonly Entry[] _entries = new Entry[ResourceTypeExtensions.Count];
        private Text _day;
        private Image _dayFill;
        private Button[] _speedButtons;
        private float _blink;

        public void Build(Transform canvas, GameManager game, Sprite[] icons, System.Action onOpenResearch, System.Action onOpenSpecialists)
        {
            _game = game;
            var panel = UiKit.Panel(canvas, "ResourceBar", UiTheme.PanelBg);
            UiKit.TopStretch(panel.rectTransform, 64f);
            var row = UiKit.Horizontal(panel.transform, "Row", 24f, new RectOffset(16, 16, 6, 6));
            UiKit.Stretch(row.GetComponent<RectTransform>());

            for (int i = 0; i < _entries.Length; i++)
            {
                var type = (ResourceType)i;
                var group = UiKit.Horizontal(row.transform, type.ToString(), 6f, new RectOffset(0, 0, 0, 0));
                var icon = UiKit.Icon(group.transform, icons != null && i < icons.Length ? icons[i] : null, 36f);
                var col = UiKit.Vertical(group.transform, "Col", 0f, new RectOffset(0, 0, 0, 0));
                var name = UiKit.Label(col.transform, type.Label(), UiTheme.FontSmall, UiTheme.TextMuted);
                var valueRow = UiKit.Horizontal(col.transform, "ValueRow", 8f, new RectOffset(0, 0, 0, 0));
                var value = UiKit.Label(valueRow.transform, "0", UiTheme.FontLarge, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                var rate = UiKit.Label(valueRow.transform, "+0/dia", UiTheme.FontSmall, UiTheme.TextMuted);
                UiKit.Size(group, width: 210f);
                _entries[i] = new Entry { Type = type, Value = value, Rate = rate, Icon = icon };
            }

            // Espaço flexível
            var spacer = UiKit.Group(row.transform, "Spacer");
            UiKit.Size(spacer, flexibleWidth: 1f);

            // Painéis
            UiKit.Size(UiKit.Button(row.transform, "Pesquisa", () => onOpenResearch?.Invoke(), UiTheme.FontSmall), width: 120f, height: 40f);
            UiKit.Size(UiKit.Button(row.transform, "Especialistas", () => onOpenSpecialists?.Invoke(), UiTheme.FontSmall), width: 150f, height: 40f);

            // Dia e relógio
            var dayCol = UiKit.Vertical(row.transform, "Day", 2f, new RectOffset(0, 0, 0, 0));
            UiKit.Size(dayCol, width: 150f);
            _day = UiKit.Label(dayCol.transform, "Dia 1", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _dayFill = UiKit.ProgressBar(dayCol.transform, 8f);

            // Velocidade
            var speeds = UiKit.Horizontal(row.transform, "Speed", 4f, new RectOffset(0, 0, 0, 0));
            var values = new[] { GameSpeed.Paused, GameSpeed.Normal, GameSpeed.Fast, GameSpeed.Fastest };
            _speedButtons = new Button[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                var speed = values[i];
                var btn = UiKit.Button(speeds.transform, speed.Label(), () => _game.Clock.SetSpeed(speed), UiTheme.FontSmall);
                UiKit.Size(btn, width: 48f, height: 40f);
                _speedButtons[i] = btn;
            }

            game.Resources.OnStockChanged += _ => RefreshValues();
            game.Resources.OnRatesChanged += RefreshValues;
            game.Clock.OnNewDay += _ => RefreshDay();
            game.Clock.OnSpeedChanged += _ => RefreshSpeed();
            RefreshValues();
            RefreshDay();
            RefreshSpeed();
        }

        private void RefreshValues()
        {
            var stock = _game.Resources.Stock;
            var net = _game.Resources.NetPerDay;
            foreach (var e in _entries)
            {
                e.Value.text = Mathf.FloorToInt(stock[e.Type]).ToString();
                float r = net[e.Type];
                e.Rate.text = UiKit.Signed(r, "/dia");
                e.Rate.color = r > 0.01f ? UiTheme.Positive : r < -0.01f ? UiTheme.Amber : UiTheme.TextMuted;
                e.Value.color = stock[e.Type] <= 0f ? UiTheme.Danger : UiTheme.Text;
            }
        }

        private void RefreshDay() => _day.text = $"Dia {_game.Clock.Day}";

        private void RefreshSpeed()
        {
            var values = new[] { GameSpeed.Paused, GameSpeed.Normal, GameSpeed.Fast, GameSpeed.Fastest };
            for (int i = 0; i < _speedButtons.Length; i++) UiKit.SetSelected(_speedButtons[i], _game.Clock.Speed == values[i]);
        }

        private void Update()
        {
            if (_game == null) return;
            _dayFill.fillAmount = _game.Clock.DayProgress;
            // Pisca o valor zerado
            _blink += Time.unscaledDeltaTime;
            bool on = Mathf.Repeat(_blink, 1f) < 0.6f;
            foreach (var e in _entries)
                if (_game.Resources.IsDepleted(e.Type)) e.Value.color = on ? UiTheme.Danger : UiTheme.TextMuted;
        }
    }
}
