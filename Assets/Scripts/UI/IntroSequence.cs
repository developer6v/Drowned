using System;
using System.Collections.Generic;
using UnityEngine;

namespace Drowned.UI
{
    /// <summary>Um quadro da introdução: imagem (nome em Resources/Intro), legenda e quanto tempo fica na tela.</summary>
    public readonly struct IntroSlide
    {
        public string Image { get; }
        public string Caption { get; }
        public float Duration { get; }

        public IntroSlide(string image, string caption, float duration)
        {
            Image = image ?? "";
            Caption = caption ?? "";
            Duration = Mathf.Max(0.1f, duration);
        }
    }

    /// <summary>
    /// Ritmo da introdução (SCRUM-198). Tudo que é tempo ou movimento fica aqui, num lugar só, em vez de espalhado
    /// pela view. Valores padrão escolhidos para ~35 s no total.
    /// </summary>
    [Serializable]
    public sealed class IntroConfig
    {
        /// <summary>Segundos de fade de entrada e de saída de cada quadro.</summary>
        public float FadeSeconds = 0.9f;
        /// <summary>Letras da legenda por segundo (efeito máquina de escrever).</summary>
        public float CharsPerSecond = 34f;
        /// <summary>Espera antes da legenda começar, depois que o quadro aparece.</summary>
        public float CaptionDelay = 0.6f;
        /// <summary>Zoom no começo e no fim de cada quadro (zoom lento, estilo Ken Burns).</summary>
        public float ZoomFrom = 1.04f;
        public float ZoomTo = 1.14f;
        /// <summary>Quanto a imagem desliza de lado durante o quadro, em fração da largura.</summary>
        public float PanFraction = 0.035f;
    }

    /// <summary>
    /// Linha do tempo da introdução (SCRUM-198). Lógica pura: recebe o tempo por <see cref="Update"/> e diz qual quadro
    /// está na tela, a opacidade, o zoom, quantas letras da legenda aparecem e quando acabou. A view
    /// (<see cref="IntroView"/>) só desenha. Clique/Espaço chama <see cref="Next"/>: se a legenda ainda está sendo
    /// escrita, mostra ela inteira; senão vai para o próximo quadro. <see cref="Skip"/> encerra.
    /// </summary>
    public sealed class IntroSequence
    {
        public event Action<int> OnSlideChanged;
        public event Action OnFinished;

        private readonly List<IntroSlide> _slides;
        private readonly IntroConfig _config;

        public int Count => _slides.Count;
        /// <summary>Quadro atual; -1 antes de <see cref="Start"/>.</summary>
        public int Index { get; private set; } = -1;
        /// <summary>Segundos desde que o quadro atual entrou.</summary>
        public float LocalTime { get; private set; }
        public bool IsFinished { get; private set; }
        public bool IsRunning => Index >= 0 && !IsFinished;
        public IntroSlide Current => IsRunning ? _slides[Index] : default;
        public IntroConfig Config => _config;

        public IntroSequence(IEnumerable<IntroSlide> slides, IntroConfig config = null)
        {
            if (slides == null) throw new ArgumentNullException(nameof(slides));
            _slides = new List<IntroSlide>(slides);
            _config = config ?? new IntroConfig();
        }

        /// <summary>Duração total, somando os quadros.</summary>
        public float TotalDuration
        {
            get
            {
                float t = 0f;
                foreach (var s in _slides) t += s.Duration;
                return t;
            }
        }

        public void Start()
        {
            if (Index >= 0 || IsFinished) return;
            if (_slides.Count == 0)
            {
                Finish();
                return;
            }
            EnterSlide(0);
        }

        /// <summary>Avança o tempo. Passa para o próximo quadro quando o atual acaba (o que sobrar do tempo é levado junto).</summary>
        public void Update(float deltaSeconds)
        {
            if (!IsRunning || deltaSeconds <= 0f) return;
            LocalTime += deltaSeconds;
            while (IsRunning && LocalTime >= Current.Duration)
            {
                float extra = LocalTime - Current.Duration;
                if (Index + 1 >= _slides.Count)
                {
                    LocalTime = Current.Duration;
                    Finish();
                    return;
                }
                EnterSlide(Index + 1);
                LocalTime = extra;
            }
        }

        /// <summary>Clique do jogador: completa a legenda; se ela já está inteira, vai para o próximo quadro (ou termina).</summary>
        public void Next()
        {
            if (!IsRunning) return;
            if (VisibleChars < Current.Caption.Length)
            {
                // Pula para o instante em que a legenda fica completa (sem entrar no fade de saída)
                float target = Mathf.Min(CaptionCompleteTime, Current.Duration - _config.FadeSeconds);
                if (target > LocalTime)
                {
                    LocalTime = target;
                    return;
                }
            }
            if (Index + 1 >= _slides.Count) Finish();
            else EnterSlide(Index + 1);
        }

        public void Skip()
        {
            if (IsFinished) return;
            Finish();
        }

        /// <summary>0 no começo do quadro, 1 no fim.</summary>
        public float Progress => IsRunning ? Mathf.Clamp01(LocalTime / Current.Duration) : (IsFinished ? 1f : 0f);

        /// <summary>Opacidade do quadro: sobe no fade de entrada, desce no de saída. O último também some no fim.</summary>
        public float Alpha
        {
            get
            {
                if (!IsRunning) return 0f;
                float fade = Mathf.Max(0.0001f, _config.FadeSeconds);
                float a = Mathf.Min(LocalTime / fade, (Current.Duration - LocalTime) / fade);
                return Mathf.Clamp01(a);
            }
        }

        /// <summary>Zoom do quadro (cresce devagar, com começo e fim suaves).</summary>
        public float Zoom => Mathf.Lerp(_config.ZoomFrom, _config.ZoomTo, Smooth(Progress));

        /// <summary>Deslocamento lateral em fração da largura: quadros pares vão para a esquerda, ímpares para a direita.</summary>
        public float Pan
        {
            get
            {
                if (!IsRunning) return 0f;
                float dir = Index % 2 == 0 ? -1f : 1f;
                return dir * _config.PanFraction * (Smooth(Progress) - 0.5f);
            }
        }

        private float CaptionCompleteTime =>
            _config.CaptionDelay + Current.Caption.Length / Mathf.Max(1f, _config.CharsPerSecond);

        /// <summary>Quantas letras da legenda já aparecem.</summary>
        public int VisibleChars
        {
            get
            {
                if (!IsRunning) return 0;
                float t = LocalTime - _config.CaptionDelay;
                if (t <= 0f) return 0;
                int n = Mathf.FloorToInt(t * Mathf.Max(1f, _config.CharsPerSecond) + 0.0001f);
                return Mathf.Min(n, Current.Caption.Length);
            }
        }

        /// <summary>A legenda até a letra visível.</summary>
        public string VisibleCaption => IsRunning ? Current.Caption.Substring(0, VisibleChars) : "";

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void EnterSlide(int index)
        {
            Index = index;
            LocalTime = 0f;
            OnSlideChanged?.Invoke(Index);
        }

        private void Finish()
        {
            IsFinished = true;
            OnFinished?.Invoke();
        }
    }
}
