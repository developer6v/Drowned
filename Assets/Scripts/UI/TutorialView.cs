using System;
using Drowned.Core;
using Drowned.World;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>
    /// Tutorial visual da primeira partida (SCRUM-193, refeito no SCRUM-197 para entender o jogo sem ler):
    /// cada passo escurece a tela em volta do alvo, desenha uma moldura pulsando e uma seta balançando que aponta
    /// para ele, com uma frase curta num balão ao lado. Os passos avançam fazendo (mover a câmera, clicar na Moradia,
    /// construir, soltar o tempo); só os informativos usam "Próximo". O jogo começa pausado durante o tutorial.
    /// A ordem dos passos e os gatilhos ficam na <see cref="TutorialSequence"/>; as contas de posição, no
    /// <see cref="TutorialLayout"/>. Montado pelo <see cref="HudBuilder"/>, que diz onde fica cada alvo.
    /// Ao terminar ou pular grava <see cref="DoneKey"/>; "Rever tutorial" nas Opções chama <see cref="ResetDone"/>.
    /// Não aparece em partida carregada de um save.
    /// </summary>
    public sealed class TutorialView : MonoBehaviour
    {
        // Chave nova no SCRUM-197: quem já tinha feito o tutorial antigo vê o novo uma vez.
        public const string DoneKey = "drowned.tutorial2.done";

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

        // Medidas em pixels da resolução de referência
        private const float DimAlpha = 0.62f;
        private const float HolePadding = 10f;
        private const float ArrowWidth = 64f;
        private const float ArrowLength = 96f;
        private const float ArrowGap = 12f;
        private const float BobAmplitude = 18f;
        private const float BobFrequency = 1.4f;
        private const float PulseFrequency = 1.2f;
        private const float ScreenMargin = 16f;
        private const float CameraArrowRadius = 150f;
        private static readonly Vector2 BubbleSize = new Vector2(620f, 180f);
        private static readonly Vector2 BuildSpotSize = new Vector2(110f, 70f);
        // Câmera conta como movida depois de andar isso (unidades do mundo) ou mudar o zoom nisso
        private const float CameraMinMove = 1.5f;
        private const float CameraMinZoom = 0.6f;

        public TutorialSequence Sequence { get; private set; }

        private GameManager _game;
        private BuildModeController _build;
        private Func<TutorialTarget, RectTransform[]> _targets;
        private RectTransform _root;
        private readonly Image[] _dims = new Image[4];
        private Image _frame;
        private Image _arrow;
        private readonly Image[] _cameraArrows = new Image[4];
        private RectTransform _bubble;
        private Text _text;
        private Text _detail;
        private Text _dots;
        private Button _next;
        private bool _decided;
        private float _stepTime;
        private Vector2 _camStartPos;
        private float _camStartSize;
        private bool _hasBuildSpot;
        private Vector2Int _buildSpot;
        private Drowned.Resources.StructureData _buildSpotFor;
        private float _buildSpotCheck;
        private bool _pausedByTutorial;
        private readonly Vector3[] _corners = new Vector3[4];

        public void Build(Transform canvas, GameManager game, BuildModeController build, Func<TutorialTarget, RectTransform[]> targets)
        {
            _game = game;
            _build = build;
            _targets = targets;

            _root = UiKit.Group(canvas, "Tutorial");
            UiKit.Stretch(_root);

            for (int i = 0; i < _dims.Length; i++)
            {
                var d = UiKit.Flat(UiKit.Panel(_root, "Dim" + i, new Color(0f, 0f, 0f, DimAlpha)));
                d.raycastTarget = false; // só escurece: o jogador clica no alvo normalmente
                BottomLeft(d.rectTransform, Vector2.zero);
                _dims[i] = d;
            }

            _frame = NewImage("Frame", UiKit.ThickRingSprite(14, 4), UiTheme.Amber);
            _frame.type = Image.Type.Sliced;
            _frame.fillCenter = false;
            BottomLeft(_frame.rectTransform, new Vector2(0.5f, 0.5f));

            var arrowSprite = ArrowSprite;
            _arrow = NewImage("Arrow", arrowSprite, UiTheme.Amber);
            BottomLeft(_arrow.rectTransform, new Vector2(0.5f, 0f)); // pivô na ponta
            _arrow.rectTransform.sizeDelta = new Vector2(ArrowWidth, ArrowLength);
            for (int i = 0; i < _cameraArrows.Length; i++)
            {
                _cameraArrows[i] = NewImage("CameraArrow" + i, arrowSprite, UiTheme.Amber);
                BottomLeft(_cameraArrows[i].rectTransform, new Vector2(0.5f, 0f));
                _cameraArrows[i].rectTransform.sizeDelta = new Vector2(ArrowWidth, ArrowLength);
            }

            // Balão
            var panel = UiKit.Panel(_root, "Bubble", UiTheme.PanelBgSolid);
            _bubble = panel.rectTransform;
            BottomLeft(_bubble, new Vector2(0.5f, 0.5f));
            _bubble.sizeDelta = BubbleSize;
            var col = UiKit.Vertical(panel.transform, "Col", 6f, new RectOffset(22, 22, 16, 14), TextAnchor.UpperLeft);
            UiKit.Stretch(col.GetComponent<RectTransform>());
            _text = UiKit.Label(col.transform, "", 30, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Size(_text, height: 44f, flexibleWidth: 1f);
            _detail = UiKit.Label(col.transform, "", UiTheme.FontSmall, UiTheme.TextMuted, TextAnchor.UpperLeft);
            UiKit.Size(_detail, height: 48f, flexibleWidth: 1f);
            var buttons = UiKit.Horizontal(col.transform, "Buttons", 10f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
            _dots = UiKit.Label(buttons.transform, "", UiTheme.FontNormal, UiTheme.Amber, TextAnchor.MiddleLeft);
            UiKit.Size(_dots, flexibleWidth: 1f);
            UiKit.Size(UiKit.Button(buttons.transform, "Pular tutorial", () => Sequence?.Skip(), UiTheme.FontSmall), width: 160f, height: 36f);
            _next = UiKit.Button(buttons.transform, "Próximo", () => Sequence?.Notify(TutorialTrigger.Acknowledge), UiTheme.FontSmall);
            UiKit.Size(_next, width: 120f, height: 36f);

            _root.gameObject.SetActive(false);
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
            Sequence.OnStepChanged += (i, step) => EnterStep(i, step);
            Sequence.OnFinished += OnFinished;

            // Pausa antes de assinar a velocidade, para essa mudança não contar como passo
            _game.Clock.SetSpeed(GameSpeed.Paused);
            _pausedByTutorial = true;
            _game.Structures.OnStructureBuilt += _ => Sequence.Notify(TutorialTrigger.StructureBuilt);
            _game.Clock.OnSpeedChanged += speed =>
            {
                if (speed != GameSpeed.Paused) Sequence.Notify(TutorialTrigger.SpeedChanged);
            };
            if (_build != null) _build.OnSelectionChanged += data =>
            {
                if (data != null) Sequence.Notify(TutorialTrigger.BuildModeEntered);
            };
            _game.OnStateChanged += _ => Sequence.Skip();
            Sequence.Start();
        }

        private void EnterStep(int index, TutorialStep step)
        {
            _root.gameObject.SetActive(true);
            _stepTime = 0f;
            _hasBuildSpot = false;
            _buildSpotFor = null;
            _next.gameObject.SetActive(step.Trigger == TutorialTrigger.Acknowledge);
            SetTexts(step.Text, step.Detail);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Sequence.Count; i++) sb.Append(i <= index ? "● " : "○ ");
            _dots.text = sb.ToString().TrimEnd();

            var cam = Camera.main;
            if (cam != null)
            {
                _camStartPos = cam.transform.position;
                _camStartSize = cam.orthographicSize;
            }
            // O passo da velocidade só faz sentido com o jogo parado
            if (step.Trigger == TutorialTrigger.SpeedChanged && _game.Clock.Speed != GameSpeed.Paused)
                _game.Clock.SetSpeed(GameSpeed.Paused);
        }

        private void OnFinished()
        {
            MarkDone();
            if (_root != null) _root.gameObject.SetActive(false);
            // Pulou antes de soltar o tempo: não deixa o jogo parado sem explicação
            if (_pausedByTutorial && !_game.IsOver && _game.Clock.Speed == GameSpeed.Paused)
                _game.Clock.SetSpeed(GameSpeed.Normal);
            _pausedByTutorial = false;
        }

        private void SetTexts(string text, string detail)
        {
            _text.text = text;
            _detail.text = detail;
        }

        private void LateUpdate()
        {
            if (Sequence == null || !Sequence.IsRunning) return;
            _stepTime += Time.unscaledDeltaTime;
            var step = Sequence.Current;
            var screen = new Rect(Vector2.zero, _root.rect.size);
            var cam = Camera.main;

            if (step.Trigger == TutorialTrigger.CameraMoved && cam != null &&
                TutorialLayout.CameraMoved(_camStartPos, _camStartSize, cam.transform.position, cam.orthographicSize, CameraMinMove, CameraMinZoom))
            {
                Sequence.Notify(TutorialTrigger.CameraMoved);
                return;
            }

            var target = step.Target;
            // Saiu do modo construção no passo de construir: volta a apontar para a Moradia
            if (target == TutorialTarget.BuildSpot && (_build == null || !_build.IsActive))
            {
                target = TutorialTarget.HousingButton;
                SetTexts("Clique na Moradia", "Escolha a estrutura de novo para construir.");
            }
            else SetTexts(step.Text, step.Detail);

            Rect rect = default;
            bool hasRect = false;
            bool dim = false;
            switch (target)
            {
                case TutorialTarget.BuildSpot:
                    hasRect = TryBuildSpotRect(cam, out rect);
                    break;
                case TutorialTarget.None:
                case TutorialTarget.CameraArrows:
                    break;
                default:
                    hasRect = TryUiRect(target, out rect);
                    dim = hasRect;
                    break;
            }

            // Escurecimento em volta do alvo
            if (dim)
            {
                var parts = TutorialLayout.DimRects(screen, rect, HolePadding);
                for (int i = 0; i < _dims.Length; i++)
                {
                    _dims[i].gameObject.SetActive(true);
                    _dims[i].rectTransform.anchoredPosition = parts[i].position;
                    _dims[i].rectTransform.sizeDelta = parts[i].size;
                }
            }
            else foreach (var d in _dims) d.gameObject.SetActive(false);

            // Moldura + seta + balão
            _frame.gameObject.SetActive(hasRect);
            _arrow.gameObject.SetActive(hasRect);
            bool cameraArrows = target == TutorialTarget.CameraArrows;
            foreach (var a in _cameraArrows) a.gameObject.SetActive(cameraArrows);

            if (hasRect)
            {
                var c = _frame.color;
                c.a = TutorialLayout.Pulse(_stepTime, PulseFrequency, 0.35f);
                _frame.color = c;
                _frame.rectTransform.anchoredPosition = rect.center;
                _frame.rectTransform.sizeDelta = rect.size + Vector2.one * (HolePadding * 2f);

                var place = TutorialLayout.PlaceArrow(screen, rect, BubbleSize, ArrowLength + BobAmplitude, ArrowGap + HolePadding, ScreenMargin);
                float bob = TutorialLayout.Bob(_stepTime, BobAmplitude, BobFrequency);
                var away = place.Side == ArrowSide.Above ? Vector2.up : Vector2.down;
                _arrow.rectTransform.anchoredPosition = place.Tip + away * bob;
                _arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, place.Angle);
                _bubble.anchoredPosition = place.BubbleCenter;
            }
            else if (cameraArrows)
            {
                var center = screen.center;
                float bob = TutorialLayout.Bob(_stepTime, BobAmplitude, BobFrequency);
                // A seta é desenhada apontando para baixo: 180 = cima, 0 = baixo, 90 = direita, -90 = esquerda
                var dirs = new[] { Vector2.up, Vector2.down, Vector2.right, Vector2.left };
                var angles = new[] { 180f, 0f, 90f, -90f };
                for (int i = 0; i < _cameraArrows.Length; i++)
                {
                    _cameraArrows[i].rectTransform.anchoredPosition = center + dirs[i] * (CameraArrowRadius + bob);
                    _cameraArrows[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, angles[i]);
                }
                _bubble.anchoredPosition = TutorialLayout.ClampInside(screen,
                    new Vector2(center.x, center.y + CameraArrowRadius + ArrowLength + BubbleSize.y * 0.5f + 30f), BubbleSize, ScreenMargin);
            }
            else
            {
                _bubble.anchoredPosition = TutorialLayout.ClampInside(screen, screen.center + Vector2.up * 140f, BubbleSize, ScreenMargin);
            }
        }

        /// <summary>Retângulo (em pixels de referência, origem embaixo à esquerda) dos elementos de UI do alvo.</summary>
        private bool TryUiRect(TutorialTarget target, out Rect rect)
        {
            rect = default;
            var elements = _targets?.Invoke(target);
            if (elements == null) return false;
            var rects = new System.Collections.Generic.List<Rect>(elements.Length);
            foreach (var e in elements)
            {
                if (e == null || !e.gameObject.activeInHierarchy) continue;
                e.GetWorldCorners(_corners);
                Vector2 a = ToRoot(_corners[0]), b = ToRoot(_corners[2]);
                rects.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)));
            }
            if (rects.Count == 0) return false;
            rect = TutorialLayout.Union(rects.ToArray());
            return true;
        }

        private Vector2 ToRoot(Vector3 world)
        {
            Vector2 local = _root.InverseTransformPoint(world);
            return local - _root.rect.min;
        }

        /// <summary>Retângulo na tela em volta de um lugar livre no mapa para a estrutura escolhida.</summary>
        private bool TryBuildSpotRect(Camera cam, out Rect rect)
        {
            rect = default;
            if (cam == null || _build == null || _build.Selected == null) return false;
            var data = _build.Selected;
            _buildSpotCheck -= Time.unscaledDeltaTime;
            // Recalcula ao trocar de estrutura e de vez em quando (o lugar pode ter sido ocupado)
            if (!_hasBuildSpot || _buildSpotFor != data || _buildSpotCheck <= 0f)
            {
                _buildSpotCheck = 0.5f;
                _buildSpotFor = data;
                var grid = _game.Grid;
                _hasBuildSpot = TutorialLayout.TryFindBuildSpot(grid, data.FootprintWidth, data.FootprintHeight,
                    grid.Width / 2, grid.Height / 2, out _buildSpot);
            }
            if (!_hasBuildSpot) return false;

            Vector3 world = _build.FootprintCenter(_buildSpot, data.FootprintWidth, data.FootprintHeight);
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            // Overlay (jogo) não usa câmera; Screen Space Camera (prints em batch) usa a do canvas
            var canvas = _root.GetComponentInParent<Canvas>();
            var uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, sp, uiCam, out var local)) return false;
            var center = local - _root.rect.min;
            // O tamanho acompanha o zoom: uma célula do grid tem 1 unidade de largura
            float unitPx = _root.rect.height / Mathf.Max(0.01f, cam.orthographicSize * 2f);
            var size = new Vector2(Mathf.Max(BuildSpotSize.x, unitPx * data.FootprintWidth),
                Mathf.Max(BuildSpotSize.y, unitPx * 0.5f * data.FootprintHeight));
            rect = new Rect(center - size * 0.5f, size);
            return true;
        }

        private Image NewImage(string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void BottomLeft(RectTransform rt, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
        }

        private static Sprite _arrowSprite;

        /// <summary>Seta branca apontando para baixo (haste + ponta), gerada por código, com borda suave.</summary>
        private static Sprite ArrowSprite
        {
            get
            {
                if (_arrowSprite != null) return _arrowSprite;
                const int w = 64, h = 96, ss = 4;
                const float headH = 46f, headHalf = 31f, shaftHalf = 10f;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "TutorialArrow",
                    hideFlags = HideFlags.DontSave,
                };
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < ss; sy++)
                            for (int sx = 0; sx < ss; sx++)
                            {
                                float fx = x + (sx + 0.5f) / ss - w * 0.5f;
                                float fy = y + (sy + 0.5f) / ss; // 0 = ponta (embaixo)
                                bool inHead = fy <= headH && Mathf.Abs(fx) <= headHalf * (fy / headH);
                                bool inShaft = fy > headH - 2f && fy <= h - 2f && Mathf.Abs(fx) <= shaftHalf;
                                if (inHead || inShaft) hits++;
                            }
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
                    }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _arrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
                _arrowSprite.name = "TutorialArrow";
                _arrowSprite.hideFlags = HideFlags.DontSave;
                return _arrowSprite;
            }
        }

        /// <summary>Passos padrão: frase curta + detalhe. Números de regra (dias até o colapso) vêm do <see cref="GameRules"/>.</summary>
        public static TutorialStep[] DefaultSteps(GameManager game)
        {
            int days = game != null && game.Rules != null ? game.Rules.DaysToCollapse : 0;
            string collapse = days > 0
                ? $"{days} dias seguidos sem energia ou oxigênio e a cidade colapsa. A borda vermelha avisa."
                : "Alguns dias seguidos sem energia ou oxigênio e a cidade colapsa. A borda vermelha avisa.";
            return new[]
            {
                new TutorialStep("Estes são seus recursos", TutorialTrigger.Acknowledge, TutorialTarget.ResourceBar,
                    "Energia, oxigênio e comida: quanto tem e quanto entra ou sai por dia."),
                new TutorialStep("Mova a câmera", TutorialTrigger.CameraMoved, TutorialTarget.CameraArrows,
                    "WASD, setas ou arraste com o botão direito. A roda do mouse dá zoom."),
                new TutorialStep("Clique na Moradia", TutorialTrigger.BuildModeEntered, TutorialTarget.HousingButton,
                    "Mais moradias, mais habitantes."),
                new TutorialStep("Agora clique aqui no mapa", TutorialTrigger.StructureBuilt, TutorialTarget.BuildSpot,
                    "Verde = pode construir. Botão direito cancela."),
                new TutorialStep("Solte o tempo", TutorialTrigger.SpeedChanged, TutorialTarget.SpeedButtons,
                    "O jogo está pausado. Clique em 1 (2 e 3 aceleram). Espaço pausa."),
                new TutorialStep("Não deixe zerar!", TutorialTrigger.Acknowledge, TutorialTarget.ResourceBar, collapse),
                new TutorialStep("Objetivo: restaurar o Monumento", TutorialTrigger.Acknowledge, TutorialTarget.MonumentPanel,
                    "Cresça a cidade, junte recursos e ligue a obra. Boa sorte!"),
            };
        }
    }
}
