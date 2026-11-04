using System.Collections.Generic;
using Drowned.Population;
using Drowned.Resources;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class StructureManagerTests
    {
        private GridMap _grid;
        private ResourceManager _resources;
        private PopulationManager _population;
        private StructureManager _structures;
        private StructureData _farm;
        private StructureData _house;

        [SetUp]
        public void SetUp()
        {
            _grid = new GridMap(10, 10);
            _grid.SetBuildableRect(0, 0, 6, 6, true);
            _resources = new ResourceManager(new ResourceBundle(100f, 100f, 100f), 24);
            _population = new PopulationManager(new PopulationConfig(0, 1, 1, 0, ResourceBundle.Zero), _resources);
            _structures = new StructureManager(_grid, _resources, _population);

            _farm = StructureData.Create("algae_farm", "Fazenda de algas", StructureCategory.Food, 2, 2,
                new StructureLevelData(new ResourceBundle(30f, 0f, 0f), new ResourceBundle(0f, 0f, 10f), new ResourceBundle(2f, 0f, 0f)),
                new StructureLevelData(new ResourceBundle(50f, 0f, 0f), new ResourceBundle(0f, 0f, 25f), new ResourceBundle(4f, 0f, 0f)));

            _house = StructureData.Create("housing", "Moradia", StructureCategory.Housing, 1, 1,
                new StructureLevelData(new ResourceBundle(20f, 0f, 10f), ResourceBundle.Zero, new ResourceBundle(1f, 1f, 0f), housingCapacity: 4),
                new StructureLevelData(new ResourceBundle(40f, 0f, 20f), ResourceBundle.Zero, new ResourceBundle(2f, 2f, 0f), housingCapacity: 10));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_farm);
            UnityEngine.Object.DestroyImmediate(_house);
        }

        [Test]
        public void ConstruirCobraOCustoOcupaOGridERegistraProducao()
        {
            var built = new List<StructureInstance>();
            _structures.OnStructureBuilt += built.Add;

            var result = _structures.TryBuild(_farm, 1, 1, out var instance, out var missing);

            Assert.AreEqual(BuildResult.Ok, result);
            Assert.IsTrue(missing.IsZero);
            Assert.AreEqual(new ResourceBundle(70f, 100f, 100f), _resources.Stock);
            Assert.IsTrue(_grid.IsOccupied(1, 1));
            Assert.IsTrue(_grid.IsOccupied(2, 2));
            Assert.AreEqual(instance.Id, _grid.OccupantAt(2, 1));
            Assert.AreEqual(1, instance.Level);
            Assert.AreEqual(new ResourceBundle(-2f, 0f, 10f), _resources.NetPerDay);
            Assert.AreEqual(new[] { instance }, built);
            Assert.AreEqual(1, _structures.Count);
            Assert.AreEqual(1, _structures.CountOf(_farm));
        }

        [Test]
        public void PosicaoInvalidaNaoCobraNada()
        {
            Assert.AreEqual(BuildResult.InvalidPosition, _structures.TryBuild(_farm, 5, 5, out var a, out _)); // sai da área construível
            Assert.AreEqual(BuildResult.InvalidPosition, _structures.TryBuild(_farm, 9, 9, out var b, out _)); // sai do mapa
            Assert.AreEqual(BuildResult.InvalidPosition, _structures.TryBuild(_farm, -1, 0, out var c, out _));
            Assert.IsNull(a);
            Assert.IsNull(b);
            Assert.IsNull(c);
            Assert.AreEqual(new ResourceBundle(100f, 100f, 100f), _resources.Stock);
            Assert.AreEqual(0, _grid.PlacedCount);
        }

        [Test]
        public void SobreOutraEstruturaEPosicaoInvalida()
        {
            _structures.TryBuild(_farm, 0, 0, out _, out _);
            Assert.AreEqual(BuildResult.InvalidPosition, _structures.TryBuild(_house, 1, 1, out _, out _));
        }

        [Test]
        public void SemRecursoInformaOQueFaltaENaoColoca()
        {
            _resources.SetStock(new ResourceBundle(10f, 50f, 5f));

            var result = _structures.TryBuild(_house, 0, 0, out var instance, out var missing);

            Assert.AreEqual(BuildResult.InsufficientResources, result);
            Assert.IsNull(instance);
            Assert.AreEqual(new ResourceBundle(10f, 0f, 5f), missing);
            Assert.IsFalse(_grid.IsOccupied(0, 0));
            Assert.AreEqual(new ResourceBundle(10f, 50f, 5f), _resources.Stock);
            Assert.AreEqual(0, _population.HousingCapacity);
        }

        [Test]
        public void PosicaoEVerificadaAntesDoCusto()
        {
            _resources.SetStock(ResourceBundle.Zero);
            Assert.AreEqual(BuildResult.InvalidPosition, _structures.CanBuild(_house, 9, 9, out _));
            Assert.AreEqual(BuildResult.InsufficientResources, _structures.CanBuild(_house, 0, 0, out var missing));
            Assert.AreEqual(_house.BuildCost, missing);
        }

        [Test]
        public void MoradiaSomaCapacidadeEPopulacaoCresce()
        {
            _structures.TryBuild(_house, 0, 0, out _, out _);
            Assert.AreEqual(4, _population.HousingCapacity);
            Assert.AreEqual(4, _structures.TotalHousingCapacity);

            _population.AdvanceDay();
            Assert.AreEqual(1, _population.Population);
        }

        [Test]
        public void RemoverLiberaTudoSemReembolso()
        {
            _structures.TryBuild(_house, 0, 0, out var instance, out _);
            var removed = new List<StructureInstance>();
            _structures.OnStructureRemoved += removed.Add;

            Assert.IsTrue(_structures.Remove(instance.Id));
            Assert.IsFalse(_structures.Remove(instance.Id));
            Assert.IsFalse(_grid.IsOccupied(0, 0));
            Assert.IsFalse(_resources.HasSource(instance.ResourceSourceId));
            Assert.AreEqual(0, _population.HousingCapacity);
            Assert.AreEqual(new ResourceBundle(80f, 100f, 90f), _resources.Stock);
            Assert.AreEqual(new[] { instance }, removed);
            Assert.IsNull(_structures.Get(instance.Id));
        }

        [Test]
        public void UpgradeCobraTrocaProducaoEMoradia()
        {
            _structures.TryBuild(_house, 0, 0, out var instance, out _);
            var changed = new List<StructureInstance>();
            _structures.OnStructureChanged += changed.Add;

            Assert.IsTrue(_structures.CanUpgrade(instance.Id));
            Assert.IsTrue(_structures.TryGetNextLevelCost(instance.Id, out var cost));
            Assert.AreEqual(new ResourceBundle(40f, 0f, 20f), cost);

            var result = _structures.TryUpgrade(instance.Id, out var missing);

            Assert.AreEqual(BuildResult.Ok, result);
            Assert.IsTrue(missing.IsZero);
            Assert.AreEqual(2, instance.Level);
            Assert.AreEqual(new ResourceBundle(40f, 100f, 70f), _resources.Stock);
            Assert.AreEqual(10, _population.HousingCapacity);
            Assert.AreEqual(new[] { instance }, changed);

            _resources.TryGetSource(instance.ResourceSourceId, out _, out var consumption);
            Assert.AreEqual(new ResourceBundle(2f, 2f, 0f), consumption);
        }

        [Test]
        public void UpgradeSemSaldoNaoMudaNada()
        {
            _structures.TryBuild(_farm, 0, 0, out var instance, out _); // sobra 70 de energia; nível 2 custa 50
            _resources.SetStock(new ResourceBundle(30f, 100f, 100f));

            Assert.AreEqual(BuildResult.InsufficientResources, _structures.TryUpgrade(instance.Id, out var missing));
            Assert.AreEqual(new ResourceBundle(20f, 0f, 0f), missing);
            Assert.AreEqual(1, instance.Level);
            Assert.AreEqual(new ResourceBundle(30f, 100f, 100f), _resources.Stock);
            Assert.AreEqual(new ResourceBundle(-2f, 0f, 10f), _resources.NetPerDay);
        }

        [Test]
        public void UpgradeNoNivelMaximoEIdDesconhecido()
        {
            _structures.TryBuild(_farm, 0, 0, out var instance, out _);
            Assert.AreEqual(BuildResult.Ok, _structures.TryUpgrade(instance.Id, out _));
            Assert.IsTrue(instance.IsMaxLevel);
            Assert.AreEqual(BuildResult.MaxLevel, _structures.TryUpgrade(instance.Id, out _));
            Assert.IsFalse(_structures.CanUpgrade(instance.Id));
            Assert.IsFalse(_structures.TryGetNextLevelCost(instance.Id, out _));
            Assert.AreEqual(BuildResult.NotFound, _structures.TryUpgrade(999, out _));
        }

        [Test]
        public void PlaceFreeNaoCobraEAceitaNivelInicial()
        {
            var instance = _structures.PlaceFree(_house, 3, 3, level: 2);
            Assert.IsNotNull(instance);
            Assert.AreEqual(2, instance.Level);
            Assert.AreEqual(new ResourceBundle(100f, 100f, 100f), _resources.Stock);
            Assert.AreEqual(10, _population.HousingCapacity);

            Assert.IsNull(_structures.PlaceFree(_house, 3, 3)); // ocupado
            Assert.IsNull(_structures.PlaceFree(_house, 4, 4, level: 3)); // nível inexistente
        }
    }
}
