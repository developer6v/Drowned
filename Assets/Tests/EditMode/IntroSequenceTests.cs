using System.Collections.Generic;
using Drowned.UI;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class IntroSequenceTests
    {
        private static IntroConfig Config() => new IntroConfig
        {
            FadeSeconds = 1f,
            CharsPerSecond = 10f,
            CaptionDelay = 0.5f,
            ZoomFrom = 1f,
            ZoomTo = 1.2f,
            PanFraction = 0.1f,
        };

        private static IntroSequence Make(params float[] durations)
        {
            var slides = new List<IntroSlide>();
            for (int i = 0; i < durations.Length; i++) slides.Add(new IntroSlide("img" + i, "Legenda " + i, durations[i]));
            return new IntroSequence(slides, Config());
        }

        [Test]
        public void Start_entra_no_primeiro_quadro()
        {
            var s = Make(5f, 5f);
            int changed = -1;
            s.OnSlideChanged += i => changed = i;
            Assert.IsFalse(s.IsRunning);
            s.Start();
            Assert.IsTrue(s.IsRunning);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(0f, s.Alpha, 0.0001f, "começa no preto");
        }

        [Test]
        public void Tempo_passa_de_quadro_levando_a_sobra()
        {
            var s = Make(5f, 5f);
            s.Start();
            s.Update(5.5f);
            Assert.AreEqual(1, s.Index);
            Assert.AreEqual(0.5f, s.LocalTime, 0.0001f);
        }

        [Test]
        public void Ultimo_quadro_termina_e_avisa_uma_vez()
        {
            var s = Make(2f, 2f);
            int finished = 0;
            s.OnFinished += () => finished++;
            s.Start();
            s.Update(10f);
            Assert.IsTrue(s.IsFinished);
            Assert.IsFalse(s.IsRunning);
            Assert.AreEqual(1, finished);
            s.Skip();
            s.Next();
            Assert.AreEqual(1, finished);
        }

        [Test]
        public void Fade_sobe_no_comeco_e_desce_no_fim()
        {
            var s = Make(5f);
            s.Start();
            s.Update(0.5f);
            Assert.AreEqual(0.5f, s.Alpha, 0.0001f);
            s.Update(2f);
            Assert.AreEqual(1f, s.Alpha, 0.0001f);
            s.Update(2f); // t = 4.5, falta 0.5 s
            Assert.AreEqual(0.5f, s.Alpha, 0.0001f);
        }

        [Test]
        public void Legenda_aparece_letra_por_letra_depois_da_espera()
        {
            var s = Make(20f);
            s.Start();
            s.Update(0.4f);
            Assert.AreEqual(0, s.VisibleChars, "antes da espera nada aparece");
            s.Update(0.4f); // t = 0.8 -> 0.3 s escrevendo a 10 letras/s
            Assert.AreEqual(3, s.VisibleChars);
            Assert.AreEqual("Leg", s.VisibleCaption);
            s.Update(10f);
            Assert.AreEqual("Legenda 0".Length, s.VisibleChars, "nunca passa do tamanho");
        }

        [Test]
        public void Next_completa_a_legenda_e_depois_avanca()
        {
            var s = Make(20f, 20f);
            s.Start();
            s.Update(0.6f);
            s.Next();
            Assert.AreEqual(0, s.Index, "primeiro clique só completa a legenda");
            Assert.AreEqual("Legenda 0", s.VisibleCaption);
            s.Next();
            Assert.AreEqual(1, s.Index, "segundo clique vai para o próximo quadro");
            Assert.AreEqual(0f, s.LocalTime, 0.0001f);
        }

        [Test]
        public void Next_no_ultimo_quadro_com_legenda_inteira_termina()
        {
            var s = Make(20f);
            bool finished = false;
            s.OnFinished += () => finished = true;
            s.Start();
            s.Update(5f);
            s.Next();
            Assert.IsTrue(finished);
        }

        [Test]
        public void Next_nao_trava_em_quadro_curto_demais_para_a_legenda()
        {
            // 9 letras a 10/s + 0.5 s de espera = 1.4 s, mas o quadro só tem 1.5 s com 1 s de fade
            var s = Make(1.5f, 5f);
            s.Start();
            s.Next();
            s.Next();
            Assert.AreEqual(1, s.Index, "no máximo dois cliques passam o quadro");
        }

        [Test]
        public void Skip_termina_no_meio()
        {
            var s = Make(5f, 5f, 5f);
            bool finished = false;
            s.OnFinished += () => finished = true;
            s.Start();
            s.Skip();
            Assert.IsTrue(finished);
            Assert.IsFalse(s.IsRunning);
        }

        [Test]
        public void Zoom_vai_do_inicial_ao_final()
        {
            var s = Make(4f);
            s.Start();
            Assert.AreEqual(1f, s.Zoom, 0.0001f);
            s.Update(2f);
            Assert.AreEqual(1.1f, s.Zoom, 0.0001f);
            s.Update(1.99f);
            Assert.AreEqual(1.2f, s.Zoom, 0.01f);
        }

        [Test]
        public void Pan_alterna_de_lado_entre_quadros()
        {
            var s = Make(4f, 4f);
            s.Start();
            s.Update(3.9f);
            float first = s.Pan;
            s.Update(3.9f);
            float second = s.Pan;
            Assert.Less(first, 0f);
            Assert.Greater(second, 0f);
        }

        [Test]
        public void Sem_quadros_termina_ao_comecar_e_duracao_soma()
        {
            var empty = new IntroSequence(new IntroSlide[0]);
            bool finished = false;
            empty.OnFinished += () => finished = true;
            empty.Start();
            Assert.IsTrue(finished);
            Assert.AreEqual(9f, Make(2f, 3f, 4f).TotalDuration, 0.0001f);
        }

        [Test]
        public void Quadros_padrao_tem_legenda_e_duram_entre_30_e_40_segundos()
        {
            var slides = IntroView.DefaultSlides();
            Assert.AreEqual(5, slides.Length);
            foreach (var sl in slides)
            {
                Assert.IsNotEmpty(sl.Caption);
                Assert.IsNotEmpty(sl.Image);
            }
            float total = new IntroSequence(slides).TotalDuration;
            Assert.That(total, Is.InRange(30f, 40f));
            // A legenda inteira cabe no quadro antes do fade de saída
            var cfg = new IntroConfig();
            foreach (var sl in slides)
                Assert.LessOrEqual(cfg.CaptionDelay + sl.Caption.Length / cfg.CharsPerSecond, sl.Duration - cfg.FadeSeconds, sl.Caption);
        }
    }
}
