using Drowned.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>
    /// Painel de dicas da primeira partida (SCRUM-193), montado pelo <see cref="HudBuilder"/>.
    /// Mostra um passo de <see cref="TutorialSequence"/> por vez, logo abaixo da barra de recursos, com
    /// "Próximo" (passos de confirmação) e "Pular tutorial". Ao terminar ou pular grava <see cref="DoneKey"/>
    /// no PlayerPrefs e não aparece mais; "Rever tutorial" nas Opções do menu chama <see cref="ResetDone"/>.
    /// Não aparece em partida carregada de um save.
    /// </summary>
    public sealed class TutorialView : MonoBehaviour
    {
        public const string DoneKey = "drowned.tutorial.done";

        public static bool IsDone => PlayerPrefs.GetInt(DoneKey, 0) == 1;

        public static void ResetDone()
        {
            PlayerPrefs.DeleteKey(DoneKey);
            PlayerPrefs.Save();
        }

        private static void MarkDone()
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
        }

        public TutorialSequence Sequence { get; private set; }

        private GameManager _game;
        private GameObject _root;
        private Text _text;
        private Text _counter;
        private Button _next;
        private bool _decided;

        public void Build(Transform canvas, GameManager game)
        {
            _game = game;

            var panel = UiKit.Panel(canvas, "Tutorial", UiTheme.PanelBgSolid);
            _root = panel.gameObject;
            UiKit.Anchor(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -110f), new Vector2(760f, 150f));

            var col = UiKit.Vertical(panel.transform, "Col", 8f, new RectOffset(20, 20, 14, 14), TextAnchor.UpperLeft);
            UiKit.Stretch(col.GetComponent<RectTransform>());

            var header = UiKit.Horizontal(col.transform, "Header", 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
            UiKit.Label(header.transform, "Dica", UiTheme.FontSmall, UiTheme.Amber, TextAnchor.MiddleLeft, FontStyle.Bold);
            _counter = UiKit.Label(header.transform, "", UiTheme.FontSmall, UiTheme.TextMuted, TextAnchor.MiddleRight);
            UiKit.Size(_counter, flexibleWidth: 1f);

            _text = UiKit.Label(col.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.UpperLeft);
            UiKit.Size(_text, height: 56f, flexibleWidth: 1f);

            var buttons = UiKit.Horizontal(col.transform, "Buttons", 10f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleRight);
            UiKit.Size(UiKit.Button(buttons.transform, "Pular tutorial", () => Sequence?.Skip(), UiTheme.FontSmall), width: 170f, height: 36f);
            _next = UiKit.Button(buttons.transform, "Próximo", () => Sequence?.Notify(TutorialTrigger.Acknowledge), UiTheme.FontSmall);
            UiKit.Size(_next, width: 130f, height: 36f);

            _root.SetActive(false);
        }

        // A decisão de mostrar fica para o primeiro Update: aí o GameManagerBehaviour já aplicou (ou não) o save.
        private void Update()
        {
            if (_decided || _game == null) return;
            _decided = true;

            var gm = GameManagerBehaviour.Instance;
            bool fromSave = gm != null && gm.StartedFromSave;
            if (IsDone || fromSave) return;

            Sequence = new TutorialSequence(DefaultSteps(_game));
            Sequence.OnStepChanged += (i, step) => Show(i, step);
            Sequence.OnFinished += () =>
            {
                MarkDone();
                if (_root != null) _root.SetActive(false);
            };
            _game.Structures.OnStructureBuilt += _ => Sequence.Notify(TutorialTrigger.StructureBuilt);
            _game.Clock.OnSpeedChanged += _ => Sequence.Notify(TutorialTrigger.SpeedChanged);
            _game.OnStateChanged += _ => Sequence.Skip();
            Sequence.Start();
        }

        private void Show(int index, TutorialStep step)
        {
            _root.SetActive(true);
            _text.text = step.Text;
            _counter.text = $"{index + 1} de {Sequence.Count}";
            _next.gameObject.SetActive(step.Trigger == TutorialTrigger.Acknowledge);
        }

        /// <summary>Passos padrão. Números de regra (dias até o colapso) vêm do <see cref="GameRules"/>.</summary>
        public static TutorialStep[] DefaultSteps(GameManager game)
        {
            int days = game != null && game.Rules != null ? game.Rules.DaysToCollapse : 0;
            string collapse = days > 0
                ? $"Cuidado: se energia ou oxigênio ficarem zerados por {days} dias seguidos, a cidade colapsa. A borda vermelha avisa."
                : "Cuidado: se energia ou oxigênio ficarem zerados por alguns dias seguidos, a cidade colapsa. A borda vermelha avisa.";
            return new[]
            {
                new TutorialStep("Bem-vindo a Drowned! A cidade vive de energia, oxigênio e alimento. A barra de cima mostra o estoque e quanto entra ou sai por dia.", TutorialTrigger.Acknowledge),
                new TutorialStep("Mova a câmera com WASD, setas ou arrastando com o botão direito. A roda do mouse dá zoom.", TutorialTrigger.Acknowledge),
                new TutorialStep("Construa algo: clique em uma estrutura na barra de baixo (comece por uma Moradia) e depois em um lugar verde do mapa.", TutorialTrigger.StructureBuilt),
                new TutorialStep("A população cresce até o limite das moradias, se houver os 3 recursos. Aperte 2 ou 3 para acelerar o tempo (Espaço pausa).", TutorialTrigger.SpeedChanged),
                new TutorialStep(collapse, TutorialTrigger.Acknowledge),
                new TutorialStep("Com mais habitantes chegam especialistas, e o Laboratório gera pesquisa. O objetivo é restaurar o Monumento no centro do mapa. Boa sorte!", TutorialTrigger.Acknowledge),
            };
        }
    }
}
