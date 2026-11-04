using System.Collections.Generic;
using Drowned.Population;
using Drowned.Resources;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class SpecialistManagerTests
    {
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        private ResourceManager _resources;
        private PopulationManager _population;
        private StructureManager _structures;
        private SpecialistData _engineer, _biologist, _scientist, _architect;

        [SetUp]
        public void SetUp()
        {
            _resources = new ResourceManager(new ResourceBundle(1000f, 1000f, 1000f), 24);
            _population = new PopulationManager(new PopulationConfig(5, 1, 1, 1, ResourceBundle.Zero), _resources, 100);
            var grid = new GridMap(10, 10);
            grid.SetBuildableRect(0, 0, 10, 10, true);
            _structures = new StructureManager(grid, _resources, _population);

            _engineer = Track(SpecialistData.Create("engineer", "Engenheiro", 10, SpecialistBonus.EnergyProduction, 0.2f));
            _biologist = Track(SpecialistData.Create("biologist", "Biólogo", 15, SpecialistBonus.FoodProduction, 0.2f));
            _scientist = Track(SpecialistData.Create("scientist", "Cientista", 20, SpecialistBonus.ResearchSpeed, 0.25f));
            _architect = Track(SpecialistData.Create("architect", "Arquiteto", 30, SpecialistBonus.BuildCostDiscount, 0.15f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var a in _assets) UnityEngine.Object.DestroyImmediate(a);
            _assets.Clear();
        }

        private T Track<T>(T asset) where T : UnityEngine.Object
        {
            _assets.Add(asset);
            return asset;
        }

        private SpecialistManager NewManager(params SpecialistData[] specialists) =>
            new SpecialistManager(specialists, _population, _resources, _structures);

        [Test]
        public void OrdenaPorThresholdEComecaSemNinguem()
        {
            var sm = NewManager(_architect, _engineer, _scientist, _biologist);
            Assert.AreEqual(new[] { _engineer, _biologist, _scientist, _architect }, sm.All);
            Assert.AreEqual(0, sm.UnlockedCount);
            Assert.AreEqual(_engineer, sm.NextToUnlock);
            Assert.AreEqual(5, sm.PopulationToNext);
        }

        [Test]
        public void DesbloqueiaAoAtingirOThresholdUmaUnicaVez()
        {
            var sm = NewManager(_engineer, _biologist);
            var unlocked = new List<SpecialistData>();
            sm.OnSpecialistUnlocked += unlocked.Add;

            _population.SetPopulation(9);
            Assert.IsEmpty(unlocked);
            _population.SetPopulation(10);
            Assert.AreEqual(new[] { _engineer }, unlocked);
            Assert.IsTrue(sm.IsUnlocked("engineer"));
            Assert.AreEqual(_biologist, sm.NextToUnlock);

            _population.SetPopulation(3); // caiu: continua desbloqueado
            _population.SetPopulation(12);
            Assert.AreEqual(1, unlocked.Count);
        }

        [Test]
        public void VariosDeUmaVezEmOrdem()
        {
            var sm = NewManager(_architect, _engineer, _scientist, _biologist);
            var unlocked = new List<SpecialistData>();
            sm.OnSpecialistUnlocked += unlocked.Add;

            _population.SetPopulation(50);
            Assert.AreEqual(new[] { _engineer, _biologist, _scientist, _architect }, unlocked);
            Assert.IsNull(sm.NextToUnlock);
            Assert.AreEqual(0, sm.PopulationToNext);
        }

        [Test]
        public void JaDesbloqueiaNaConstrucaoSeAPopulacaoBasta()
        {
            _population.SetPopulation(12);
            var sm = NewManager(_engineer);
            Assert.IsTrue(sm.IsUnlocked("engineer"));
        }

        [Test]
        public void EngenheiroAumentaProducaoDeEnergia()
        {
            _resources.SetSource("gerador", new ResourceBundle(50f, 0f, 10f), ResourceBundle.Zero);
            NewManager(_engineer);
            _population.SetPopulation(10);

            Assert.AreEqual(1.2f, _resources.ProductionMultiplier.Energy, 1e-5f);
            Assert.AreEqual(1f, _resources.ProductionMultiplier.Food);
            Assert.AreEqual(60f, _resources.ProductionPerDay.Energy, 1e-4f);
            Assert.AreEqual(10f, _resources.ProductionPerDay.Food, 1e-4f);
            Assert.AreEqual(50f, _resources.BaseProductionPerDay.Energy, 1e-4f);
        }

        [Test]
        public void ArquitetoReduzCustoDeConstrucaoEUpgrade()
        {
            var house = Track(StructureData.Create("housing", "Moradia", StructureCategory.Housing, 1, 1,
                new StructureLevelData(new ResourceBundle(100f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero, 4),
                new StructureLevelData(new ResourceBundle(200f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero, 8)));
            NewManager(_architect);
            _population.SetPopulation(30);

            Assert.AreEqual(0.85f, _structures.CostMultiplier, 1e-5f);
            Assert.AreEqual(85f, _structures.BuildCostOf(house).Energy, 1e-4f);

            _structures.TryBuild(house, 0, 0, out var instance, out _);
            Assert.AreEqual(1000f - 85f, _resources.Stock.Energy, 1e-3f);

            _structures.TryGetNextLevelCost(instance.Id, out var next);
            Assert.AreEqual(170f, next.Energy, 1e-4f);
            _structures.TryUpgrade(instance.Id, out _);
            Assert.AreEqual(1000f - 85f - 170f, _resources.Stock.Energy, 1e-3f);
        }

        [Test]
        public void BonusDoMesmoTipoSomam()
        {
            var eng2 = Track(SpecialistData.Create("engineer2", "Engenheiro sênior", 12, SpecialistBonus.EnergyProduction, 0.3f));
            NewManager(_engineer, eng2);
            _population.SetPopulation(12);
            Assert.AreEqual(1.5f, _resources.ProductionMultiplier.Energy, 1e-5f);
        }

        [Test]
        public void CientistaMudaVelocidadeDePesquisa()
        {
            var sm = NewManager(_scientist);
            var changes = new List<float>();
            sm.OnResearchSpeedChanged += changes.Add;
            _population.SetPopulation(20);
            Assert.AreEqual(1.25f, sm.ResearchSpeedMultiplier, 1e-5f);
            Assert.AreEqual(new[] { 1.25f }, changes);
        }

        [Test]
        public void RestoreAplicaBonusSemEvento()
        {
            var sm = NewManager(_engineer, _biologist);
            int events = 0;
            sm.OnSpecialistUnlocked += _ => events++;

            sm.Restore(new[] { "engineer", "desconhecido" });
            Assert.IsTrue(sm.IsUnlocked("engineer"));
            Assert.IsFalse(sm.IsUnlocked("biologist"));
            Assert.AreEqual(0, events);
            Assert.AreEqual(1.2f, _resources.ProductionMultiplier.Energy, 1e-5f);

            sm.Restore(new[] { "engineer" }); // repetido não aplica de novo
            Assert.AreEqual(1.2f, _resources.ProductionMultiplier.Energy, 1e-5f);
        }
    }
}
