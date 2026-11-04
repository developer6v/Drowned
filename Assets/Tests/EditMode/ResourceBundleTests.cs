using Drowned.Resources;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class ResourceBundleTests
    {
        [Test]
        public void IndexadorDevolveCadaRecurso()
        {
            var b = new ResourceBundle(1f, 2f, 3f);
            Assert.AreEqual(1f, b[ResourceType.Energy]);
            Assert.AreEqual(2f, b[ResourceType.Oxygen]);
            Assert.AreEqual(3f, b[ResourceType.Food]);
        }

        [Test]
        public void OperacoesAritmeticas()
        {
            var a = new ResourceBundle(10f, 20f, 30f);
            var b = new ResourceBundle(1f, 2f, 3f);
            Assert.AreEqual(new ResourceBundle(11f, 22f, 33f), a + b);
            Assert.AreEqual(new ResourceBundle(9f, 18f, 27f), a - b);
            Assert.AreEqual(new ResourceBundle(2f, 4f, 6f), b * 2f);
            Assert.AreEqual(new ResourceBundle(2f, 4f, 6f), 2f * b);
            Assert.AreEqual(new ResourceBundle(5f, 10f, 15f), a / 2f);
        }

        [Test]
        public void CoversExigeTodosOsRecursos()
        {
            var stock = new ResourceBundle(10f, 10f, 10f);
            Assert.IsTrue(stock.Covers(new ResourceBundle(10f, 5f, 0f)));
            Assert.IsFalse(stock.Covers(new ResourceBundle(11f, 0f, 0f)));
            Assert.IsFalse(stock.Covers(new ResourceBundle(0f, 0f, 10.5f)));
        }

        [Test]
        public void MissingNuncaENegativo()
        {
            var stock = new ResourceBundle(10f, 0f, 5f);
            var cost = new ResourceBundle(4f, 3f, 9f);
            Assert.AreEqual(new ResourceBundle(0f, 3f, 4f), stock.Missing(cost));
            Assert.IsTrue(stock.Missing(ResourceBundle.Zero).IsZero);
        }

        [Test]
        public void ClampedToZeroZeraNegativos()
        {
            var b = new ResourceBundle(-1f, 0f, 2f).ClampedToZero();
            Assert.AreEqual(new ResourceBundle(0f, 0f, 2f), b);
        }

        [Test]
        public void WithTrocaSoUmRecurso()
        {
            var b = new ResourceBundle(1f, 2f, 3f).With(ResourceType.Oxygen, 9f);
            Assert.AreEqual(new ResourceBundle(1f, 9f, 3f), b);
        }

        [Test]
        public void StructureDataExpoeNiveis()
        {
            var n1 = new StructureLevelData(new ResourceBundle(10f, 0f, 0f), new ResourceBundle(0f, 0f, 5f), new ResourceBundle(1f, 0f, 0f));
            var n2 = new StructureLevelData(new ResourceBundle(20f, 0f, 0f), new ResourceBundle(0f, 0f, 12f), new ResourceBundle(2f, 0f, 0f));
            var data = StructureData.Create("algae_farm", "Fazenda de algas", StructureCategory.Food, 2, 2, n1, n2);

            Assert.AreEqual(2, data.LevelCount);
            Assert.IsTrue(data.HasLevel(2));
            Assert.IsFalse(data.HasLevel(3));
            Assert.AreEqual(new ResourceBundle(10f, 0f, 0f), data.BuildCost);
            Assert.AreEqual(new ResourceBundle(-2f, 0f, 12f), data.GetLevel(2).NetPerDay);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => data.GetLevel(0));
        }
    }
}
