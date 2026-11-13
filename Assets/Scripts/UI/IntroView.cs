using System;
using Drowned.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>
    /// Animação de introdução (SCRUM-198): quadros renderizados no Blender (<c>tools/blender/render_intro.py</c>,
    /// em <c>Resources/Intro/intro_N</c>) com zoom lento, fade e legenda letra por letra. Tela cheia, por cima de tudo.
    /// Clique ou Espaço avança, Esc ou "Pular" encerra. A linha do tempo é a <see cref="IntroSequence"/>.
    /// Use <see cref="Play"/>: cria o objeto, toca e chama o callback no fim (e se destrói).
    /// </summary>
    public sealed class IntroView : MonoBehaviour
    {
        public const string SeenKey = "drowned.intro.seen";

        public static bool IsSeen => PlayerPrefs.GetInt(SeenKey, 0) == 1;

        public static void MarkSeen()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Quadros e legendas. A história vem do GDD: a cidade, o colapso, a ruína, os sobreviventes, o objetivo.</summary>
        public static IntroSlide[] DefaultSlides() => new[]
        {
            new IntroSlide("intro_1", "Há gerações, uma cidade inteira vivia no fundo do oceano, protegida por uma grande cúpula.", 7.5f),
            new IntroSlide("intro_2", "Até que um tremor rachou a cúpula. O mar começou a entrar.", 6.5f),
            new IntroSlide("intro_3", "A cidade foi engolida. O Monumento Central, seu símbolo, virou ruína.", 7f),
            new IntroSlide("intro_4", "Só cinco sobreviventes restaram, abrigados numa pequena cúpula.", 6.5f),
            new IntroSlide("intro_5", "Agora é com você: mantenha energia, oxigênio e comida, faça a cidade crescer e restaure o Monumento.", 8.5f),
        };

        /// <summary>Toca a introdução e chama <paramref name="onDone"/> quando acabar ou for pulada.</summary>
        public static IntroView Play(Action onDone, IntroConfig config = null)
        {
            var go = new GameObject("Intro");
            var view = go.AddComponent<IntroView>();
            view.Begin(new IntroSequence(DefaultSlides(), config), onDone);
            return view;
        }

        public IntroSequence Sequence { get; private set; }

        private Action _onDone;
        private RectTransform _frame;
        private Image _image;
        private Image _fade;
        private Text _caption;
        private Text _dots;
        private Sprite[] _sprites;
        private bool _done;

        private void Begin(IntroSequence sequence, Action onDone)
        {
            Sequence = sequence;
            _onDone = onDone;

            var canvas = UiKit.CreateCanvas("IntroCanvas", 500);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            var bg = UiKit.Flat(UiKit.Panel(root, "Black", Color.black));
            UiKit.Stretch(bg.rectTransform);
            // O fundo inteiro é o botão de avançar: bloqueia o menu por baixo e qualquer clique fora do "Pular" avança
            var clickCatcher = bg.gameObject.AddComponent<Button>();
            clickCatcher.transition = Selectable.Transition.None;
            clickCatcher.onClick.AddListener(() =>
            {
                // Sem deixar o fundo selecionado: senão Espaço/Enter "clicaria" de novo e pularia dois quadros
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                Sequence?.Next();
            });

            // Quadro: um pouco maior que a tela, para o zoom e o deslize não mostrarem borda
            _frame = UiKit.Group(root, "Frame");
            UiKit.Stretch(_frame);
            var imgGo = new GameObject("Image", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(_frame, false);
            _image = imgGo.GetComponent<Image>();
            _image.raycastTarget = false;
            _image.preserveAspect = false;
            UiKit.Stretch(_image.rectTransform);

            // Escurece embaixo para a legenda ler sobre qualquer imagem
            var shade = UiKit.Flat(UiKit.Panel(root, "CaptionShade", new Color(0f, 0f, 0f, 0.55f)));
            shade.raycastTarget = false;
            UiKit.BottomStretch(shade.rectTransform, 210f);

            _caption = UiKit.Label(root, "", 34, UiTheme.Text, TextAnchor.MiddleCenter);
            UiKit.Anchor(_caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 56f), new Vector2(1400f, 130f));
            _caption.GetComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            _dots = UiKit.Label(root, "", UiTheme.FontNormal, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            UiKit.Anchor(_dots.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(300f, 30f));

            var hint = UiKit.Label(root, "clique ou Espaço para continuar", UiTheme.FontSmall, UiTheme.TextMuted, TextAnchor.MiddleRight);
            UiKit.Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-28f, 18f), new Vector2(420f, 30f));

            // Fade para preto por cima da imagem (a legenda fica acima do fade)
            _fade = UiKit.Flat(UiKit.Panel(root, "Fade", Color.black));
            _fade.raycastTarget = false;
            UiKit.Stretch(_fade.rectTransform);
            _fade.transform.SetSiblingIndex(_frame.GetSiblingIndex() + 1);

            var skip = UiKit.Button(root, "Pular  (Esc)", Finish, UiTheme.FontNormal);
            UiKit.Anchor(skip.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(180f, 52f));

            _sprites = new Sprite[sequence.Count];
            var slides = DefaultSlides();
            for (int i = 0; i < _sprites.Length && i < slides.Length; i++)
                _sprites[i] = UnityEngine.Resources.Load<Sprite>("Intro/" + slides[i].Image);

            Sequence.OnSlideChanged += ShowSlide;
            Sequence.OnFinished += Finish;
            Sequence.Start();
        }

        private void ShowSlide(int index)
        {
            var sprite = index < _sprites.Length ? _sprites[index] : null;
            _image.sprite = sprite;
            // Sem imagem (antes do render), um azul escuro: a legenda ainda conta a história
            _image.color = sprite != null ? Color.white : new Color(0.03f, 0.12f, 0.2f, 1f);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Sequence.Count; i++) sb.Append(i == index ? "●  " : "○  ");
            _dots.text = sb.ToString().TrimEnd();
        }

        private void Update()
        {
            if (_done || Sequence == null) return;
            if (GameInput.EscapeDown)
            {
                Finish();
                return;
            }
            if (GameInput.SpaceDown) Sequence.Next();
            Sequence.Update(Time.unscaledDeltaTime);
            if (_done || !Sequence.IsRunning) return;

            float zoom = Sequence.Zoom;
            _frame.localScale = new Vector3(zoom, zoom, 1f);
            float width = ((RectTransform)_frame.parent).rect.width;
            _frame.anchoredPosition = new Vector2(Sequence.Pan * width, 0f);
            var c = _fade.color;
            c.a = 1f - Sequence.Alpha;
            _fade.color = c;
            _caption.text = Sequence.VisibleCaption;
        }

        private void Finish()
        {
            if (_done) return;
            _done = true;
            Sequence?.Skip();
            MarkSeen();
            var cb = _onDone;
            _onDone = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
