using System.Collections.Generic;
using Drowned.Resources;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class ResourceManagerTests
    {
        private static ResourceManager NewManager(float energy = 100f, float oxygen = 100f, float food = 80f, int ticksPerDay = 24)
            => new ResourceManager(new ResourceBundle(energy, oxygen, food), ticksPerDay);

        [Test]
        public void ComecaComEstoqueInicialESemFontes()
        {
            var rm = NewManager();
            Assert.AreEqual(new ResourceBundle(100f, 100f, 80f), rm.Stock);
            Assert.AreEqual(0, rm.SourceCount);
            Assert.IsTrue(rm.NetPerDay.IsZero);
            Assert.IsTrue(rm.AllPositive);
        }

        [Test]
        public void ParametrosInvalidosLancam()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ResourceManager(ResourceBundle.Zero, 0));
            Assert.Throws<System.ArgumentException>(() => NewManager().SetSource("", ResourceBundle.Zero, ResourceBundle.Zero));
        }

        [Test]
        public void TickAplicaSaldoDivididoPorTicksPorDia()
        {
            var rm = NewManager(100f, 100f, 100f, ticksPerDay: 10);
            rm.SetSource("gerador", new ResourceBundle(20f, 0f, 0f), new ResourceBundle(0f, 0f, 0f));
            rm.SetSource("fazenda", new ResourceBundle(0f, 0f, 30f), new ResourceBundle(10f, 0f, 0f));

            Assert.AreEqual(new ResourceBundle(10f, 0f, 30f), rm.NetPerDay);

            rm.Tick();
            Assert.AreEqual(new ResourceBundle(101f, 100f, 103f), rm.Stock);

            for (int i = 0; i < 9; i++) rm.Tick();
            Assert.AreEqual(110f, rm.Stock.Energy, 1e-3f);
            Assert.AreEqual(130f, rm.Stock.Food, 1e-3f);
        }

        [Test]
        public void EstoqueNuncaFicaNegativo()
        {
            var rm = NewManager(1f, 100f, 100f, ticksPerDay: 1);
            rm.SetSource("consumidor", ResourceBundle.Zero, new ResourceBundle(50f, 0f, 0f));
            rm.Tick();
            Assert.AreEqual(0f, rm.Stock.Energy);
            Assert.IsTrue(rm.IsDepleted(ResourceType.Energy));
            Assert.IsFalse(rm.AllPositive);
        }

        [Test]
        public void EventosDeZerouEVoltouDisparamSoNaTransicao()
        {
            var rm = NewManager(2f, 100f, 100f, ticksPerDay: 1);
            var depleted = new List<ResourceType>();
            var restored = new List<ResourceType>();
            rm.OnDepleted += depleted.Add;
            rm.OnRestored += restored.Add;

            rm.SetSource("c", ResourceBundle.Zero, new ResourceBundle(1f, 0f, 0f));
            rm.Tick(); // 1
            rm.Tick(); // 0 -> zerou
            rm.Tick(); // continua 0, sem evento novo
            Assert.AreEqual(new[] { ResourceType.Energy }, depleted);
            Assert.IsEmpty(restored);

            rm.SetSource("c", new ResourceBundle(5f, 0f, 0f), ResourceBundle.Zero);
            rm.Tick(); // voltou
            rm.Tick();
            Assert.AreEqual(new[] { ResourceType.Energy }, restored);
            Assert.AreEqual(1, depleted.Count);
        }

        [Test]
        public void RemoverFonteAtualizaTaxas()
        {
            var rm = NewManager();
            int rateEvents = 0;
            rm.OnRatesChanged += () => rateEvents++;

            rm.SetSource("a", new ResourceBundle(5f, 0f, 0f), ResourceBundle.Zero);
            rm.SetSource("b", new ResourceBundle(0f, 7f, 0f), ResourceBundle.Zero);
            Assert.AreEqual(new ResourceBundle(5f, 7f, 0f), rm.ProductionPerDay);

            Assert.IsTrue(rm.RemoveSource("a"));
            Assert.IsFalse(rm.RemoveSource("a"));
            Assert.AreEqual(new ResourceBundle(0f, 7f, 0f), rm.ProductionPerDay);
            Assert.AreEqual(3, rateEvents);
        }

        [Test]
        public void TrySpendDebitaQuandoHaSaldo()
        {
            var rm = NewManager(100f, 100f, 80f);
            var changes = new List<ResourceBundle>();
            rm.OnStockChanged += changes.Add;

            Assert.IsTrue(rm.CanAfford(new ResourceBundle(100f, 0f, 80f)));
            Assert.IsTrue(rm.TrySpend(new ResourceBundle(30f, 10f, 0f), out var missing));
            Assert.IsTrue(missing.IsZero);
            Assert.AreEqual(new ResourceBundle(70f, 90f, 80f), rm.Stock);
            Assert.AreEqual(1, changes.Count);
        }

        [Test]
        public void TrySpendNaoDebitaNadaSemSaldoEInformaOQueFalta()
        {
            var rm = NewManager(10f, 10f, 10f);
            Assert.IsFalse(rm.TrySpend(new ResourceBundle(5f, 25f, 12f), out var missing));
            Assert.AreEqual(new ResourceBundle(0f, 15f, 2f), missing);
            Assert.AreEqual(new ResourceBundle(10f, 10f, 10f), rm.Stock);
        }

        [Test]
        public void EstoqueInicialZeradoNaoDisparaEventoAteVoltar()
        {
            var rm = NewManager(0f, 10f, 10f, ticksPerDay: 1);
            var restored = new List<ResourceType>();
            rm.OnRestored += restored.Add;
            Assert.IsTrue(rm.IsDepleted(ResourceType.Energy));

            rm.Add(new ResourceBundle(1f, 0f, 0f));
            Assert.AreEqual(new[] { ResourceType.Energy }, restored);
        }
    }
}
