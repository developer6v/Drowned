using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class GridMapTests
    {
        private static GridMap MapaTodoConstruivel(int w, int h)
        {
            var m = new GridMap(w, h);
            m.SetBuildableRect(0, 0, w, h, true);
            return m;
        }

        [Test]
        public void MapaNovoNaoTemCelulasConstruiveis()
        {
            var m = new GridMap(4, 3);
            Assert.AreEqual(0, m.BuildableCount);
            Assert.IsFalse(m.IsBuildable(0, 0));
            Assert.IsFalse(m.CanPlace(0, 0));
        }

        [Test]
        public void DimensoesInvalidasLancam()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GridMap(0, 5));
        }

        [Test]
        public void ForaDoMapaNuncaEConstruivelNemOcupado()
        {
            var m = MapaTodoConstruivel(2, 2);
            Assert.IsFalse(m.InBounds(-1, 0));
            Assert.IsFalse(m.IsBuildable(2, 0));
            Assert.IsFalse(m.IsOccupied(0, 2));
            Assert.AreEqual(0, m.OccupantAt(5, 5));
        }

        [Test]
        public void SetBuildableRectRespeitaOsLimites()
        {
            var m = new GridMap(3, 3);
            m.SetBuildableRect(1, 1, 10, 10, true); // extrapola, mas não lança
            Assert.AreEqual(4, m.BuildableCount);
            Assert.IsTrue(m.IsBuildable(2, 2));
            Assert.IsFalse(m.IsBuildable(0, 0));
        }

        [Test]
        public void ColocarOcupaAsCelulasDaPegada()
        {
            var m = MapaTodoConstruivel(5, 5);
            int id = m.Place(1, 1, 2, 2);

            Assert.Greater(id, 0);
            Assert.IsTrue(m.IsOccupied(1, 1));
            Assert.IsTrue(m.IsOccupied(2, 2));
            Assert.IsFalse(m.IsOccupied(3, 3));
            Assert.AreEqual(id, m.OccupantAt(2, 1));
            Assert.AreEqual(1, m.PlacedCount);
        }

        [Test]
        public void NaoColocaSobreOutraEstrutura()
        {
            var m = MapaTodoConstruivel(5, 5);
            m.Place(1, 1, 2, 2);

            Assert.IsFalse(m.CanPlace(2, 2));          // canto sobreposto
            Assert.IsFalse(m.CanPlace(0, 0, 2, 2));    // encosta em (1,1)
            Assert.IsTrue(m.CanPlace(3, 3, 2, 2));     // livre
            Assert.Throws<System.InvalidOperationException>(() => m.Place(1, 1));
        }

        [Test]
        public void NaoColocaEmCelulaNaoConstruivel()
        {
            var m = MapaTodoConstruivel(4, 4);
            m.SetBuildable(2, 2, false);
            Assert.IsFalse(m.CanPlace(1, 1, 2, 2));
            Assert.IsTrue(m.CanPlace(0, 0, 2, 2));
        }

        [Test]
        public void NaoColocaParcialmenteForaDoMapa()
        {
            var m = MapaTodoConstruivel(3, 3);
            Assert.IsFalse(m.CanPlace(2, 2, 2, 2));
            Assert.IsFalse(m.CanPlace(-1, 0));
        }

        [Test]
        public void PegadaInvalidaNaoCabe()
        {
            var m = MapaTodoConstruivel(3, 3);
            Assert.IsFalse(m.CanPlace(0, 0, 0, 1));
        }

        [Test]
        public void BlockedCellsListaSoAsCelulasProblema()
        {
            var m = MapaTodoConstruivel(4, 4);
            m.SetBuildable(0, 0, false);
            m.Place(1, 1);

            var blocked = m.BlockedCells(0, 0, 2, 2);
            CollectionAssert.AreEquivalent(new[] { (0, 0), (1, 1) }, blocked);
        }

        [Test]
        public void RemoverLiberaAsCelulas()
        {
            var m = MapaTodoConstruivel(4, 4);
            int id = m.Place(0, 0, 2, 2);

            Assert.IsTrue(m.Remove(id));
            Assert.IsFalse(m.IsOccupied(1, 1));
            Assert.IsTrue(m.CanPlace(0, 0, 2, 2));
            Assert.AreEqual(0, m.PlacedCount);
            Assert.IsFalse(m.Remove(id)); // já removido
            Assert.IsFalse(m.TryGetFootprint(id, out _));
        }

        [Test]
        public void IdsSaoUnicosMesmoDepoisDeRemover()
        {
            var m = MapaTodoConstruivel(4, 4);
            int a = m.Place(0, 0);
            m.Remove(a);
            int b = m.Place(0, 0);
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void TryGetFootprintDevolveAPegada()
        {
            var m = MapaTodoConstruivel(5, 5);
            int id = m.Place(2, 3, 2, 1);
            Assert.IsTrue(m.TryGetFootprint(id, out var fp));
            Assert.AreEqual(2, fp.X);
            Assert.AreEqual(3, fp.Y);
            Assert.AreEqual(2, fp.Width);
            Assert.AreEqual(1, fp.Height);
        }
    }
}
