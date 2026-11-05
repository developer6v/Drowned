using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class InhabitantMathTests
    {
        [Test]
        public void Direcao_pelos_quatro_quadrantes_da_tela()
        {
            Assert.AreEqual(InhabitantMath.FacingDownLeft, InhabitantMath.DirectionIndex(-1f, -0.5f));
            Assert.AreEqual(InhabitantMath.FacingDownRight, InhabitantMath.DirectionIndex(1f, -0.5f));
            Assert.AreEqual(InhabitantMath.FacingUpRight, InhabitantMath.DirectionIndex(1f, 0.5f));
            Assert.AreEqual(InhabitantMath.FacingUpLeft, InhabitantMath.DirectionIndex(-1f, 0.5f));
        }

        [Test]
        public void Parado_mantem_a_direcao_atual()
        {
            Assert.AreEqual(InhabitantMath.FacingUpLeft, InhabitantMath.DirectionIndex(0f, 0f, InhabitantMath.FacingUpLeft));
        }

        [Test]
        public void Quantidade_limitada_pelo_maximo_e_nunca_negativa()
        {
            Assert.AreEqual(5, InhabitantMath.WalkerCount(5, 24));
            Assert.AreEqual(24, InhabitantMath.WalkerCount(300, 24));
            Assert.AreEqual(0, InhabitantMath.WalkerCount(-3, 24));
            Assert.AreEqual(0, InhabitantMath.WalkerCount(10, 0));
        }

        [Test]
        public void Balanco_do_passo_fica_entre_zero_e_a_altura()
        {
            for (int i = 0; i < 100; i++)
                Assert.That(InhabitantMath.StepBob(i * 0.13f, 0.3f, 2.2f, 0.035f), Is.InRange(0f, 0.035f + 1e-6f));
        }
    }
}
