using System.Linq;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class DecorLayoutTests
    {
        private static GridMap MapWithBorder(int size, int border)
        {
            var grid = new GridMap(size, size);
            grid.SetBuildableRect(border, border, size - border * 2, size - border * 2, true);
            return grid;
        }

        [Test]
        public void So_coloca_em_celulas_nao_construiveis()
        {
            var grid = MapWithBorder(12, 2);
            var picks = DecorLayout.Pick(grid, 5, 1f, 1);
            Assert.IsNotEmpty(picks);
            foreach (var p in picks) Assert.IsFalse(grid.IsBuildable(p.X, p.Y), $"({p.X},{p.Y}) é construível");
        }

        [Test]
        public void Densidade_um_preenche_toda_a_borda()
        {
            var grid = MapWithBorder(12, 2);
            int border = 12 * 12 - 8 * 8;
            Assert.AreEqual(border, DecorLayout.Pick(grid, 3, 1f, 7).Count);
        }

        [Test]
        public void Densidade_zero_ou_sem_variacoes_nao_coloca_nada()
        {
            var grid = MapWithBorder(12, 2);
            Assert.IsEmpty(DecorLayout.Pick(grid, 3, 0f, 7));
            Assert.IsEmpty(DecorLayout.Pick(grid, 0, 1f, 7));
        }

        [Test]
        public void Respeita_a_exclusao()
        {
            var grid = new GridMap(10, 10); // nada construível
            var picks = DecorLayout.Pick(grid, 2, 1f, 3, (x, y) => x >= 4 && x < 6 && y >= 4 && y < 6);
            Assert.IsFalse(picks.Any(p => p.X >= 4 && p.X < 6 && p.Y >= 4 && p.Y < 6));
            Assert.AreEqual(100 - 4, picks.Count);
        }

        [Test]
        public void Nao_coloca_em_celula_ocupada()
        {
            var grid = new GridMap(6, 6);
            grid.SetBuildableRect(0, 0, 2, 2, true);
            grid.Place(0, 0, 2, 2);
            grid.SetBuildableRect(0, 0, 2, 2, false); // ocupada e não construível
            var picks = DecorLayout.Pick(grid, 2, 1f, 3);
            Assert.IsFalse(picks.Any(p => p.X < 2 && p.Y < 2));
        }

        [Test]
        public void Mesma_semente_mesmo_resultado()
        {
            var grid = MapWithBorder(16, 3);
            var a = DecorLayout.Pick(grid, 6, 0.4f, 99);
            var b = DecorLayout.Pick(grid, 6, 0.4f, 99);
            CollectionAssert.AreEqual(a.Select(p => (p.X, p.Y, p.Variant, p.FlipX)).ToList(),
                                      b.Select(p => (p.X, p.Y, p.Variant, p.FlipX)).ToList());
        }

        [Test]
        public void Variacao_fica_dentro_da_lista()
        {
            var grid = MapWithBorder(16, 3);
            foreach (var p in DecorLayout.Pick(grid, 4, 1f, 5)) Assert.That(p.Variant, Is.InRange(0, 3));
        }
    }
}
