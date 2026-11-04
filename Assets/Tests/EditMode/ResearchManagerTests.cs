using System.Collections.Generic;
using System.Linq;
using Drowned.Population;
using Drowned.Research;
using Drowned.Resources;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class ResearchManagerTests
    {
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        private ResourceManager _resources;
        private PopulationManager _population;
        private StructureManager _structures;
        private StructureData _lab, _house, _reactor;
        private TechnologyData _algae, _modular, _hydro, _computing, _reactorTech;

        [SetUp]
        public void SetUp()
        {
            _resources = new ResourceManager(new ResourceBundle(1000f, 1000f, 1000f), 24);
            _population = new PopulationManager(new PopulationConfig(5, 1, 1, 1, ResourceBundle.Zero), _resources, 0);
            var grid = new GridMap(10, 10);
            grid.SetBuildableRect(0, 0, 10, 10, true);
            _structures = new StructureManager(grid, _resources, _population);

            _lab = Track(StructureData.Create("lab", "Laboratório", StructureCategory.Research, 1, 1,
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero, researchPerDay: 24f),
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero, researchPerDay: 48f)));
            _house = Track(StructureData.Create("housing", "Moradia", StructureCategory.Housing, 1, 1,
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero, 4),
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero, 10)));
            _reactor = Track(StructureData.Create("reactor", "Reator", StructureCategory.Energy, 1, 1,
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), new ResourceBundle(50f, 0f, 0f), ResourceBundle.Zero)));
            _reactor.SetUnlockRequirements(requiresTechnology: true, baseMaxLevel: 1);

            _algae = Track(TechnologyData.Create("algae", "Algas resistentes", 48f, null, TechnologyEffect.ProductionBonus(ResourceType.Food, 0.2f)));
            _modular = Track(TechnologyData.Create("modular", "Engenharia modular", 24f, null, TechnologyEffect.UnlockLevel("housing", 2)));
            _hydro = Track(TechnologyData.Create("hydro", "Hidroponia", 10f, new[] { _algae }, TechnologyEffect.HousingBonus(0.25f)));
            _computing = Track(TechnologyData.Create("computing", "Computação submersa", 10f, new[] { _hydro, _modular }, TechnologyEffect.ResearchBonus(0.3f)));
            _reactorTech = Track(TechnologyData.Create("reactor_tech", "Reator geotérmico", 10f, null, TechnologyEffect.UnlockStructure("reactor")));
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

        private ResearchManager NewManager()
        {
            var rm = new ResearchManager(new[] { _algae, _modular, _hydro, _computing, _reactorTech }, 24, _resources, _population, _structures);
            _structures.SetUnlockRules(rm);
            return rm;
        }

        [Test]
        public void LaboratorioRegistraPontosPorDiaAoConstruirMelhorarERemover()
        {
            var research = NewManager();
            _structures.TryBuild(_lab, 0, 0, out var lab, out _);
            Assert.AreEqual(24f, research.PointsPerDay);

            Assert.AreEqual(BuildResult.Locked, _structures.TryUpgrade(lab.Id, out _)); // BaseMaxLevel default 1
            Assert.AreEqual(24f, research.PointsPerDay);

            _lab.SetUnlockRequirements(requiresTechnology: false, baseMaxLevel: 2);
            Assert.AreEqual(BuildResult.Ok, _structures.TryUpgrade(lab.Id, out _));
            Assert.AreEqual(48f, research.PointsPerDay);

            _structures.Remove(lab.Id);
            Assert.AreEqual(0f, research.PointsPerDay);
        }

        [Test]
        public void SemTecnologiaEscolhidaOsPontosFicamGuardados()
        {
            var research = NewManager();
            _structures.TryBuild(_lab, 0, 0, out _, out _);
            research.Tick();
            Assert.AreEqual(1f, research.StoredPoints, 1e-4f);
            Assert.AreEqual(0f, research.Progress);
        }

        [Test]
        public void AcumulaPorTickEConcluiComExcedente()
        {
            var research = NewManager();
            _structures.TryBuild(_lab, 0, 0, out _, out _); // 1 ponto por tick
            var done = new List<TechnologyData>();
            var progress = new List<float>();
            research.OnResearched += done.Add;
            research.OnProgressChanged += progress.Add;

            Assert.IsTrue(research.StartResearch(_modular)); // custa 24
            for (int i = 0; i < 23; i++) research.Tick();
            Assert.AreEqual(23f / 24f, research.Progress, 1e-4f);
            Assert.IsEmpty(done);

            research.SetSource("extra", 48f); // +2 por tick: total 3 no próximo tick
            research.Tick();
            Assert.AreEqual(new[] { _modular }, done);
            Assert.IsNull(research.Current);
            Assert.AreEqual(2f, research.StoredPoints, 1e-4f); // 23 + 3 = 26, custo 24
            Assert.IsTrue(research.IsResearched("modular"));
            Assert.AreEqual(0f, progress.Last());
        }

        [Test]
        public void PreRequisitoBloqueiaEDepoisLibera()
        {
            var research = NewManager();
            Assert.IsFalse(research.CanResearch(_hydro));
            Assert.IsFalse(research.StartResearch(_hydro));
            Assert.AreEqual(new[] { _algae, _modular, _reactorTech }, research.Available.ToArray());

            research.Restore(new[] { "algae" }, null, 0f, 0f);
            Assert.IsTrue(research.CanResearch(_hydro));
            Assert.IsFalse(research.CanResearch(_computing));
            Assert.IsFalse(research.CanResearch(_algae)); // já pesquisada
        }

        [Test]
        public void PontosGuardadosEntramAoEscolherEPodemConcluirNaHora()
        {
            var research = NewManager();
            research.SetSource("lab", 24f);
            for (int i = 0; i < 30; i++) research.Tick(); // 30 guardados
            Assert.AreEqual(30f, research.StoredPoints, 1e-4f);

            Assert.IsTrue(research.StartResearch(_modular)); // custa 24: conclui na hora
            Assert.IsTrue(research.IsResearched(_modular));
            Assert.AreEqual(6f, research.StoredPoints, 1e-4f);
        }

        [Test]
        public void CancelarDevolvePontosParaOBanco()
        {
            var research = NewManager();
            research.SetSource("lab", 24f);
            research.StartResearch(_algae);
            for (int i = 0; i < 10; i++) research.Tick();
            research.Cancel();
            Assert.IsNull(research.Current);
            Assert.AreEqual(10f, research.StoredPoints, 1e-4f);
        }

        [Test]
        public void MultiplicadoresDeVelocidadeSomam()
        {
            var research = NewManager();
            research.SetSource("lab", 24f);
            research.SetBaseSpeedMultiplier(1.25f);
            Assert.AreEqual(30f, research.EffectivePointsPerDay, 1e-4f);

            research.Restore(new[] { "algae", "modular", "hydro", "computing" }, null, 0f, 0f);
            Assert.AreEqual(1.55f, research.SpeedMultiplier, 1e-5f);
            research.Tick();
            Assert.AreEqual(24f * 1.55f / 24f, research.StoredPoints, 1e-4f);
        }

        [Test]
        public void TecnologiaLiberaEstrutura()
        {
            var research = NewManager();
            Assert.AreEqual(BuildResult.Locked, _structures.TryBuild(_reactor, 0, 0, out _, out _));
            Assert.IsFalse(_structures.IsUnlocked(_reactor));

            research.Restore(new[] { "reactor_tech" }, null, 0f, 0f);
            Assert.AreEqual(BuildResult.Ok, _structures.TryBuild(_reactor, 0, 0, out _, out _));
        }

        [Test]
        public void TecnologiaLiberaNivel()
        {
            var research = NewManager();
            _structures.TryBuild(_house, 0, 0, out var house, out _);
            Assert.AreEqual(BuildResult.Locked, _structures.TryUpgrade(house.Id, out _));
            Assert.AreEqual(4, _population.HousingCapacity);

            research.SetSource("lab", 24f);
            research.StartResearch(_modular);
            for (int i = 0; i < 24; i++) research.Tick();
            Assert.IsTrue(research.IsResearched(_modular));
            Assert.AreEqual(BuildResult.Ok, _structures.TryUpgrade(house.Id, out _));
            Assert.AreEqual(10, _population.HousingCapacity);
        }

        [Test]
        public void BonusDeProducaoEMoradia()
        {
            var research = NewManager();
            _resources.SetSource("farm", new ResourceBundle(0f, 0f, 10f), ResourceBundle.Zero);
            _structures.TryBuild(_house, 0, 0, out _, out _); // 4 de moradia

            research.Restore(new[] { "algae", "hydro" }, null, 0f, 0f);
            Assert.AreEqual(12f, _resources.ProductionPerDay.Food, 1e-4f);
            Assert.AreEqual(1.25f, _population.HousingMultiplier, 1e-5f);
            Assert.AreEqual(5, _population.HousingCapacity); // floor(4 x 1.25)
            Assert.AreEqual(4, _population.BaseHousingCapacity);
        }

        [Test]
        public void RestoreComTecnologiaAtualEmProgresso()
        {
            var research = NewManager();
            research.Restore(new string[0], "algae", 12f, 3f);
            Assert.AreEqual(_algae, research.Current);
            Assert.AreEqual(0.25f, research.Progress, 1e-5f);
            Assert.AreEqual(3f, research.StoredPoints);
        }
    }
}
