using System.Collections.Generic;
using Drowned.Population;
using Drowned.Resources;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class PopulationManagerTests
    {
        private static PopulationConfig Config(int initial = 5, int growth = 1, int loss = 1, int minimum = 1)
            => new PopulationConfig(initial, growth, loss, minimum, new ResourceBundle(1f, 2f, 3f));

        private static ResourceManager Resources(float energy = 100f, float oxygen = 100f, float food = 100f)
            => new ResourceManager(new ResourceBundle(energy, oxygen, food), 24);

        [Test]
        public void ComecaComPopulacaoInicialERegistraConsumo()
        {
            var rm = Resources();
            var pm = new PopulationManager(Config(initial: 5), rm, initialHousingCapacity: 10);

            Assert.AreEqual(5, pm.Population);
            Assert.AreEqual(10, pm.HousingCapacity);
            Assert.AreEqual(5, pm.FreeHousing);
            Assert.IsTrue(rm.TryGetSource(PopulationManager.ResourceSourceId, out var prod, out var cons));
            Assert.IsTrue(prod.IsZero);
            Assert.AreEqual(new ResourceBundle(5f, 10f, 15f), cons);
        }

        [Test]
        public void ConsumoAcompanhaAPopulacao()
        {
            var rm = Resources();
            var pm = new PopulationManager(Config(initial: 5), rm, 10);
            pm.AdvanceDay(); // 6
            rm.TryGetSource(PopulationManager.ResourceSourceId, out _, out var cons);
            Assert.AreEqual(new ResourceBundle(6f, 12f, 18f), cons);
            Assert.AreEqual(new ResourceBundle(6f, 12f, 18f), rm.ConsumptionPerDay);
        }

        [Test]
        public void CresceAteOTetoDeMoradia()
        {
            var rm = Resources();
            var pm = new PopulationManager(Config(initial: 5, growth: 2), rm, 8);
            var changes = new List<int>();
            pm.OnPopulationChanged += changes.Add;

            pm.AdvanceDay(); // 7
            pm.AdvanceDay(); // 8 (limitado)
            pm.AdvanceDay(); // 8, sem evento
            Assert.AreEqual(8, pm.Population);
            Assert.AreEqual(new[] { 7, 8 }, changes);
        }

        [Test]
        public void NaoCresceSemEnergiaOuOxigenio()
        {
            var pm1 = new PopulationManager(Config(), Resources(energy: 0f), 10);
            pm1.AdvanceDay();
            Assert.AreEqual(5, pm1.Population);

            var pm2 = new PopulationManager(Config(), Resources(oxygen: 0f), 10);
            pm2.AdvanceDay();
            Assert.AreEqual(5, pm2.Population);
        }

        [Test]
        public void DiminuiComFomeAteOMinimo()
        {
            var pm = new PopulationManager(Config(initial: 3, loss: 2, minimum: 1), Resources(food: 0f), 10);
            pm.AdvanceDay(); // 1
            Assert.AreEqual(1, pm.Population);
            pm.AdvanceDay(); // continua 1
            Assert.AreEqual(1, pm.Population);
        }

        [Test]
        public void FomeNaoSobeAPopulacaoAteOMinimo()
        {
            // População já abaixo do mínimo (ex.: carregada de um save) não deve subir por causa da regra da fome.
            var pm = new PopulationManager(Config(initial: 0, loss: 1, minimum: 2), Resources(food: 0f), 10);
            pm.AdvanceDay();
            Assert.AreEqual(0, pm.Population);
        }

        [Test]
        public void CapacidadeDeMoradiaPodeSerSomadaERemovida()
        {
            var pm = new PopulationManager(Config(), Resources(), 0);
            var changes = new List<int>();
            pm.OnHousingCapacityChanged += changes.Add;

            pm.AddHousingCapacity(4);
            pm.AddHousingCapacity(6);
            pm.AddHousingCapacity(-20); // nunca negativo
            Assert.AreEqual(new[] { 4, 10, 0 }, changes);
            Assert.AreEqual(0, pm.FreeHousing);
        }

        [Test]
        public void ParametrosInvalidosLancam()
        {
            Assert.Throws<System.ArgumentNullException>(() => new PopulationManager(null, Resources()));
            Assert.Throws<System.ArgumentNullException>(() => new PopulationManager(Config(), null));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PopulationManager(Config(), Resources(), -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PopulationManager(Config(), Resources()).SetPopulation(-1));
        }
    }
}
