using Drowned.Core;
using Drowned.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>
    /// Alerta de crise (SCRUM-171): borda vermelha pulsando enquanto energia ou oxigênio está zerado,
    /// com os dias que faltam para o colapso. Alimento zerado só mostra um aviso âmbar.
    /// </summary>
    public sealed class CrisisAlertView : MonoBehaviour
    {
        private GameManager _game;
        private Image[] _borders;
        private Text _message;
        private Image _messageBg;
        private bool _inCrisis;
        private bool _alarmPlayed;

        public void Build(Transform canvas, GameManager game)
        {
            _game = game;
            const float thickness = 18f;
            var root = UiKit.Group(canvas, "CrisisAlert");
            UiKit.Stretch(root);
            _borders = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var img = UiKit.Flat(UiKit.Panel(root, "Border" + i, UiTheme.Danger));
                img.raycastTarget = false;
                var rt = img.rectTransform;
                switch (i)
                {
                    case 0: UiKit.TopStretch(rt, thickness); break;
                    case 1: UiKit.BottomStretch(rt, thickness); break;
                    case 2: UiKit.Anchor(rt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(thickness, 0f)); break;
                    default: UiKit.Anchor(rt, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(thickness, 0f)); break;
                }
                _borders[i] = img;
            }

            _messageBg = UiKit.Panel(root, "Message", UiTheme.PanelBgSolid);
            _messageBg.raycastTarget = false;
            UiKit.Anchor(_messageBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(760f, 44f));
            _message = UiKit.Label(_messageBg.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Stretch(_message.rectTransform, 12f, 4f);
            var alertIcon = GameIcons.Alert;
            if (alertIcon != null)
            {
                var icon = UiKit.Icon(_messageBg.transform, alertIcon, 32f);
                icon.GetComponent<LayoutElement>().ignoreLayout = true;
                UiKit.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(12f, 0f), new Vector2(32f, 32f));
                _message.rectTransform.offsetMin = new Vector2(52f, _message.rectTransform.offsetMin.y);
            }

            game.Resources.OnDepleted += _ => Refresh();
            game.Resources.OnRestored += _ => Refresh();
            game.Collapse.OnDangerChanged += _ => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            bool energy = _game.Resources.IsDepleted(ResourceType.Energy);
            bool oxygen = _game.Resources.IsDepleted(ResourceType.Oxygen);
            bool food = _game.Resources.IsDepleted(ResourceType.Food);
            _inCrisis = energy || oxygen;

            foreach (var b in _borders) b.gameObject.SetActive(_inCrisis);

            if (_inCrisis)
            {
                string what = energy && oxygen ? "Energia e oxigênio esgotados!" : energy ? "Energia esgotada!" : "Oxigênio esgotado!";
                int days = _game.Collapse.DaysRemaining;
                _message.text = $"{what} {days} {(days == 1 ? "dia" : "dias")} até o colapso";
                _message.color = UiTheme.Danger;
                _messageBg.gameObject.SetActive(true);
                if (!_alarmPlayed)
                {
                    _alarmPlayed = true;
                    AudioManager.PlayIfAvailable(GameSound.CrisisAlarm);
                }
            }
            else
            {
                _alarmPlayed = false;
                if (food)
                {
                    _message.text = "Sem alimento: a população está diminuindo";
                    _message.color = UiTheme.Amber;
                    _messageBg.gameObject.SetActive(true);
                }
                else _messageBg.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_inCrisis) return;
            float a = 0.45f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
            var c = UiTheme.Danger;
            c.a = a;
            foreach (var b in _borders) b.color = c;
        }
    }
}
