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

        // ------------------------------------------------------------------ rota em grade (SCRUM-199)

        private static GridMap OpenGrid(int w, int h)
        {
            var g = new GridMap(w, h);
            g.SetBuildableRect(0, 0, w, h, true);
            return g;
        }

        [Test]
        public void Caminho_contorna_estrutura_no_meio()
        {
            var g = OpenGrid(5, 5);
            g.Place(1, 1, 3, 3); // bloco 3x3 no meio
            var path = InhabitantMath.FindPath(g, 0, 2, 4, 2);
            Assert.IsNotNull(path);
            Assert.AreEqual((4, 2), path[path.Count - 1]);
            foreach (var c in path) Assert.IsFalse(g.IsOccupied(c.x, c.y), $"passou por cima da estrutura em {c}");
            // Cada passo é vizinho do anterior (4 direções)
            var prev = (x: 0, y: 2);
            foreach (var c in path)
            {
                Assert.AreEqual(1, System.Math.Abs(c.x - prev.x) + System.Math.Abs(c.y - prev.y));
                prev = c;
            }
            Assert.AreEqual(8, path.Count, "contorno mais curto: sobe 2, anda 4, desce 2");
        }

        [Test]
        public void Caminho_vazio_quando_ja_esta_no_destino_e_null_se_bloqueado()
        {
            var g = OpenGrid(3, 3);
            Assert.AreEqual(0, InhabitantMath.FindPath(g, 1, 1, 1, 1).Count);
            g.Place(2, 2);
            Assert.IsNull(InhabitantMath.FindPath(g, 0, 0, 2, 2), "destino ocupado");
            Assert.IsNull(InhabitantMath.FindPath(g, 0, 0, 5, 5), "fora do mapa");
        }

        [Test]
        public void Caminho_null_quando_isolado()
        {
            var g = OpenGrid(5, 1);
            g.Place(2, 0); // parede no meio de um corredor
            Assert.IsNull(InhabitantMath.FindPath(g, 0, 0, 4, 0));
        }

        [Test]
        public void Habitante_em_cima_de_estrutura_nova_consegue_sair()
        {
            var g = OpenGrid(4, 4);
            g.Place(1, 1, 2, 2); // construíram em cima dele, em (1,1)
            var path = InhabitantMath.FindPath(g, 1, 1, 3, 3);
            Assert.IsNotNull(path);
            foreach (var c in path) Assert.IsTrue(InhabitantMath.IsWalkable(g, c.x, c.y));
        }

        [Test]
        public void Habitante_desenha_entre_as_fileiras_de_estrutura()
        {
            // Estrutura com âncora na fileira 10 (x + y)
            int building = StructureManagerBehaviour.SortingOrderFor(4, 6);
            Assert.Greater(StructureManagerBehaviour.WalkerSortingOrderFor(5, 5), building, "mesma fileira: na frente");
            Assert.Greater(StructureManagerBehaviour.WalkerSortingOrderFor(3, 6), building, "fileira da frente: na frente");
            Assert.Less(StructureManagerBehaviour.WalkerSortingOrderFor(5, 6), building, "fileira de trás: atrás");
            Assert.AreNotEqual(StructureManagerBehaviour.WalkerSortingOrderFor(5, 6), building, "sem empate");
        }
    }
}
