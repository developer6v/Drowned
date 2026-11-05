using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class StructureAnimMathTests
    {
        [Test]
        public void BuildPop_comeca_em_zero_e_termina_em_um()
        {
            Assert.AreEqual(0f, StructureAnimMath.BuildPop(0f, 1.7f), 1e-5f);
            Assert.AreEqual(1f, StructureAnimMath.BuildPop(1f, 1.7f), 1e-5f);
            Assert.AreEqual(0f, StructureAnimMath.BuildPop(-3f, 1.7f), 1e-5f);
            Assert.AreEqual(1f, StructureAnimMath.BuildPop(9f, 1.7f), 1e-5f);
        }

        [Test]
        public void BuildPop_com_exagero_passa_de_um_antes_de_assentar()
        {
            float max = 0f;
            for (int i = 1; i < 100; i++) max = System.Math.Max(max, StructureAnimMath.BuildPop(i / 100f, 1.7f));
            Assert.Greater(max, 1.02f);
            Assert.Less(max, 1.2f, "exagero exagerado");
        }

        [Test]
        public void BuildPop_sem_exagero_nunca_passa_de_um()
        {
            for (int i = 0; i <= 100; i++) Assert.LessOrEqual(StructureAnimMath.BuildPop(i / 100f, 0f), 1f + 1e-5f);
        }

        [Test]
        public void Breathe_fica_dentro_da_amplitude()
        {
            for (int i = 0; i < 200; i++)
            {
                float v = StructureAnimMath.Breathe(i * 0.37f, 1.1f, 0.02f, 0.35f);
                Assert.That(v, Is.InRange(0.98f - 1e-5f, 1.02f + 1e-5f));
            }
        }

        [Test]
        public void Pulse_fica_entre_zero_e_um()
        {
            for (int i = 0; i < 200; i++)
                Assert.That(StructureAnimMath.Pulse01(i * 0.21f, 0.4f, 0.6f), Is.InRange(-1e-5f, 1f + 1e-5f));
        }

        [Test]
        public void Fase_e_estavel_e_varia_entre_ids()
        {
            Assert.AreEqual(StructureAnimMath.PhaseFor(7), StructureAnimMath.PhaseFor(7));
            Assert.AreNotEqual(StructureAnimMath.PhaseFor(7), StructureAnimMath.PhaseFor(8));
            for (int id = 0; id < 50; id++) Assert.That(StructureAnimMath.PhaseFor(id), Is.InRange(0f, 6.3f));
        }
    }
}
