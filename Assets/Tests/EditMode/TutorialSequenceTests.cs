using System.Collections.Generic;
using Drowned.UI;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class TutorialSequenceTests
    {
        private static TutorialSequence Make(params TutorialTrigger[] triggers)
        {
            var steps = new List<TutorialStep>();
            for (int i = 0; i < triggers.Length; i++) steps.Add(new TutorialStep("passo " + i, triggers[i]));
            return new TutorialSequence(steps);
        }

        [Test]
        public void Antes_de_Start_nao_esta_rodando()
        {
            var t = Make(TutorialTrigger.Acknowledge);
            Assert.IsFalse(t.IsRunning);
            Assert.AreEqual(-1, t.Index);
            Assert.IsFalse(t.Notify(TutorialTrigger.Acknowledge));
        }

        [Test]
        public void Start_entra_no_primeiro_passo_e_avisa()
        {
            var t = Make(TutorialTrigger.Acknowledge, TutorialTrigger.StructureBuilt);
            int changedTo = -1;
            t.OnStepChanged += (i, _) => changedTo = i;
            t.Start();
            Assert.IsTrue(t.IsRunning);
            Assert.AreEqual(0, changedTo);
            Assert.AreEqual("passo 0", t.Current.Text);
        }

        [Test]
        public void Gatilho_errado_nao_avanca()
        {
            var t = Make(TutorialTrigger.StructureBuilt, TutorialTrigger.Acknowledge);
            t.Start();
            Assert.IsFalse(t.Notify(TutorialTrigger.Acknowledge));
            Assert.IsFalse(t.Notify(TutorialTrigger.SpeedChanged));
            Assert.AreEqual(0, t.Index);
        }

        [Test]
        public void Gatilho_certo_avanca_um_passo()
        {
            var t = Make(TutorialTrigger.StructureBuilt, TutorialTrigger.Acknowledge);
            t.Start();
            Assert.IsTrue(t.Notify(TutorialTrigger.StructureBuilt));
            Assert.AreEqual(1, t.Index);
            Assert.AreEqual(TutorialTrigger.Acknowledge, t.Current.Trigger);
        }

        [Test]
        public void Ultimo_passo_termina_e_avisa_uma_vez()
        {
            var t = Make(TutorialTrigger.Acknowledge, TutorialTrigger.Acknowledge);
            int finished = 0;
            t.OnFinished += () => finished++;
            t.Start();
            t.Notify(TutorialTrigger.Acknowledge);
            t.Notify(TutorialTrigger.Acknowledge);
            Assert.IsTrue(t.IsFinished);
            Assert.IsFalse(t.IsRunning);
            Assert.AreEqual(1, finished);
            Assert.IsFalse(t.Notify(TutorialTrigger.Acknowledge), "depois de terminar, nada avança");
            t.Skip();
            Assert.AreEqual(1, finished, "Skip depois de terminar não avisa de novo");
        }

        [Test]
        public void Skip_termina_no_meio()
        {
            var t = Make(TutorialTrigger.Acknowledge, TutorialTrigger.Acknowledge, TutorialTrigger.Acknowledge);
            bool finished = false;
            t.OnFinished += () => finished = true;
            t.Start();
            t.Skip();
            Assert.IsTrue(finished);
            Assert.IsTrue(t.IsFinished);
            Assert.IsFalse(t.Notify(TutorialTrigger.Acknowledge));
        }

        [Test]
        public void Sem_passos_termina_ao_comecar()
        {
            var t = Make();
            bool finished = false;
            t.OnFinished += () => finished = true;
            t.Start();
            Assert.IsTrue(finished);
            Assert.IsFalse(t.IsRunning);
        }

        [Test]
        public void Passos_padrao_tem_passo_de_construir_e_textos()
        {
            var steps = TutorialView.DefaultSteps(null);
            Assert.Greater(steps.Length, 3);
            Assert.IsTrue(System.Array.Exists(steps, s => s.Trigger == TutorialTrigger.StructureBuilt), "precisa de um passo que pede para construir");
            foreach (var s in steps) Assert.IsNotEmpty(s.Text);
        }

        [Test]
        public void Passos_padrao_ensinam_fazendo_e_apontam_para_algo()
        {
            var steps = TutorialView.DefaultSteps(null);
            var triggers = new HashSet<TutorialTrigger>();
            foreach (var s in steps)
            {
                triggers.Add(s.Trigger);
                Assert.AreNotEqual(TutorialTarget.None, s.Target, $"o passo '{s.Text}' precisa de um alvo para a seta");
                Assert.LessOrEqual(s.Text.Split(' ').Length, 8, $"frase curta: '{s.Text}'");
            }
            Assert.IsTrue(triggers.Contains(TutorialTrigger.CameraMoved));
            Assert.IsTrue(triggers.Contains(TutorialTrigger.BuildModeEntered));
            Assert.IsTrue(triggers.Contains(TutorialTrigger.SpeedChanged));
            // A Moradia vem antes de construir no mapa
            int pick = System.Array.FindIndex(steps, s => s.Trigger == TutorialTrigger.BuildModeEntered);
            int build = System.Array.FindIndex(steps, s => s.Trigger == TutorialTrigger.StructureBuilt);
            Assert.Less(pick, build);
            Assert.AreEqual(TutorialTarget.BuildSpot, steps[build].Target);
        }

        [Test]
        public void Gatilhos_novos_avancam_so_no_passo_certo()
        {
            var t = Make(TutorialTrigger.CameraMoved, TutorialTrigger.BuildModeEntered);
            t.Start();
            Assert.IsFalse(t.Notify(TutorialTrigger.BuildModeEntered));
            Assert.IsTrue(t.Notify(TutorialTrigger.CameraMoved));
            Assert.IsTrue(t.Notify(TutorialTrigger.BuildModeEntered));
            Assert.IsTrue(t.IsFinished);
        }

        [Test]
        public void Passo_guarda_alvo_e_detalhe()
        {
            var s = new TutorialStep("Clique", TutorialTrigger.Acknowledge, TutorialTarget.SpeedButtons, null);
            Assert.AreEqual(TutorialTarget.SpeedButtons, s.Target);
            Assert.AreEqual("", s.Detail);
            Assert.AreEqual(TutorialTarget.None, new TutorialStep("x", TutorialTrigger.Acknowledge).Target);
        }
    }
}
