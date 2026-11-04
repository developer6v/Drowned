using System.Collections.Generic;
using Drowned.Monument;
using Drowned.Population;
using Drowned.Research;
using Drowned.Resources;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class MonumentManagerTests
    {
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        private ResourceManager _resources;
        private PopulationManager _population;
        private StructureManager _structures;
        private SpecialistManager _specialists;
        private ResearchManager _research;
        private SpecialistData _engineer;
        private TechnologyData _modular;

        [SetUp]
        public void SetUp()
        {
            _resources = new ResourceManager(new ResourceBundle(100f, 100f, 100f), 10);
            _population = new PopulationManager(new PopulationConfig(5, 1, 1, 1, ResourceBundle.Zero), _resources, 100);
            var grid = new GridMap(5, 5);
            grid.SetBuildableRect(0, 0, 5, 5, true);
            _structures = new StructureManager(grid, _resources, _population);
            _engineer = Track(SpecialistData.Create("engineer", "Engenheiro", 10, SpecialistBonus.EnergyProduction, 0.2f));
            _specialists = new SpecialistManager(new[] { _engineer }, _population, _resources, _structures);
            _modular = Track(TechnologyData.Create("modular", "Engenharia modular", 10f, null));
            _research = new ResearchManager(new[] { _modular }, 10, _resources, _population, _structures);
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

        private MonumentManager NewMonument(params MonumentStage[] stages)
        {
            var data = Track(MonumentData.Create(stages));
            return new MonumentManager(data, 10, _resources, _population, _specialists, _research);
        }

        private static MonumentStage Stage(string name, ResourceBundle required, ResourceBundle perDay, int minPop = 0, string[] specialists = null, string[] techs = null)
            => new MonumentStage(name, required, perDay, minPop, specialists, techs);

        [Test]
        public void ComecaNaRuinaInativo()
        {
            var m = NewMonument(Stage("Fundação", new ResourceBundle(30f, 20f, 10f), new ResourceBundle(10f, 10f, 10f)));
            Assert.AreEqual(0, m.CurrentStageIndex);
            Assert.IsFalse(m.IsActive);
            Assert.IsFalse(m.IsComplete);
            Assert.AreEqual(0f, m.Progress);
            Assert.AreEqual("Fundação", m.CurrentStage.DisplayName);
            Assert.AreEqual(new ResourceBundle(30f, 20f, 10f), m.Remaining);
        }

        [Test]
        public void InativoNaoRetiraNada()
        {
            var m = NewMonument(Stage("Fundação", new ResourceBundle(30f, 20f, 10f), new ResourceBundle(10f, 10f, 10f)));
            m.Tick();
            Assert.AreEqual(new ResourceBundle(100f, 100f, 100f), _resources.Stock);
            Assert.IsTrue(m.Delivered.IsZero);
        }

        [Test]
        public void AtivoRetiraContribuicaoPorTickLimitadaAoQueFalta()
        {
            // 10 ticks por dia; contribuição 10/dia = 1 por tick; alimento exige só 1.5
            var m = NewMonument(Stage("Fundação", new ResourceBundle(30f, 20f, 1.5f), new ResourceBundle(10f, 10f, 10f)));
            var progress = new List<float>();
            m.OnProgressChanged += progress.Add;
            m.SetActive(true);

            m.Tick();
            Assert.AreEqual(new ResourceBundle(1f, 1f, 1f), m.Delivered);
            Assert.AreEqual(new ResourceBundle(99f, 99f, 99f), _resources.Stock);

            m.Tick();
            Assert.AreEqual(new ResourceBundle(2f, 2f, 1.5f), m.Delivered);
            Assert.AreEqual(98.5f, _resources.Stock.Food, 1e-4f);
            Assert.AreEqual(2, progress.Count);
            Assert.AreEqual(5.5f / 51.5f, m.Progress, 1e-4f);
        }

        [Test]
        public void RetiraSoOQueHaNoEstoque()
        {
            _resources.SetStock(new ResourceBundle(0.25f, 0f, 100f));
            var m = NewMonument(Stage("Fundação", new ResourceBundle(30f, 20f, 10f), new ResourceBundle(10f, 10f, 10f)));
            m.SetActive(true);
            m.Tick();
            Assert.AreEqual(new ResourceBundle(0.25f, 0f, 1f), m.Delivered);
            Assert.AreEqual(0f, _resources.Stock.Energy);
        }

        [Test]
        public void ConcluiEtapaQuandoRecursosEntreguesEAvancaParaAProxima()
        {
            var m = NewMonument(
                Stage("Fundação", new ResourceBundle(2f, 0f, 0f), new ResourceBundle(10f, 0f, 0f)),
                Stage("Estrutura", new ResourceBundle(5f, 0f, 0f), new ResourceBundle(10f, 0f, 0f)));
            var completed = new List<int>();
            bool done = false;
            m.OnStageCompleted += completed.Add;
            m.OnCompleted += () => done = true;
            m.SetActive(true);

            m.Tick();
            Assert.IsEmpty(completed);
            m.Tick();
            Assert.AreEqual(new[] { 0 }, completed);
            Assert.AreEqual(1, m.CurrentStageIndex);
            Assert.AreEqual("Estrutura", m.CurrentStage.DisplayName);
            Assert.IsTrue(m.Delivered.IsZero);
            Assert.IsTrue(m.IsActive); // obra continua na próxima etapa
            Assert.IsFalse(done);
            Assert.AreEqual(0.5f, m.OverallProgress, 1e-4f);
        }

        [Test]
        public void BloqueiaPorPopulacaoEspecialistaETecnologiaEConcluiQuandoAtendidos()
        {
            var m = NewMonument(Stage("Sistemas", new ResourceBundle(1f, 0f, 0f), new ResourceBundle(10f, 0f, 0f),
                minPop: 10, specialists: new[] { "engineer" }, techs: new[] { "modular" }));
            var completed = new List<int>();
            m.OnStageCompleted += completed.Add;
            m.SetActive(true);
            m.Tick();

            Assert.IsTrue(m.ResourcesDelivered);
            Assert.IsFalse(m.RequirementsMet);
            Assert.AreEqual(new[] { "População 10 (atual 5)", "Especialista: Engenheiro", "Tecnologia: Engenharia modular" }, m.MissingRequirements());
            Assert.IsEmpty(completed);

            _population.SetPopulation(10); // desbloqueia o engenheiro também
            Assert.AreEqual(new[] { "Tecnologia: Engenharia modular" }, m.MissingRequirements());
            Assert.IsEmpty(completed);

            _research.Restore(new string[0], null, 0f, 0f);
            _research.SetSource("lab", 100f);
            _research.StartResearch(_modular);
            _research.Tick(); // 10 pontos: conclui
            Assert.AreEqual(new[] { 0 }, completed);
            Assert.IsTrue(m.IsComplete);
        }

        [Test]
        public void UltimaEtapaDisparaVitoriaEDesliga()
        {
            var m = NewMonument(Stage("Única", new ResourceBundle(1f, 0f, 0f), new ResourceBundle(10f, 0f, 0f)));
            bool done = false;
            var active = new List<bool>();
            m.OnCompleted += () => done = true;
            m.OnActiveChanged += active.Add;
            m.SetActive(true);
            m.Tick();

            Assert.IsTrue(done);
            Assert.IsTrue(m.IsComplete);
            Assert.IsFalse(m.IsActive);
            Assert.AreEqual(1f, m.Progress);
            Assert.AreEqual(1f, m.OverallProgress);
            Assert.AreEqual(new[] { true, false }, active);
            m.SetActive(true); // não religa depois de pronto
            Assert.IsFalse(m.IsActive);
        }

        [Test]
        public void SemDadosNaoFazNada()
        {
            var m = new MonumentManager(null, 10, _resources, _population, _specialists, _research);
            m.SetActive(true);
            m.Tick();
            Assert.IsFalse(m.IsComplete);
            Assert.IsNull(m.CurrentStage);
            Assert.AreEqual(0f, m.OverallProgress);
        }

        [Test]
        public void RestoreRecolocaEtapaEEntrega()
        {
            var m = NewMonument(
                Stage("A", new ResourceBundle(10f, 0f, 0f), new ResourceBundle(10f, 0f, 0f)),
                Stage("B", new ResourceBundle(10f, 0f, 0f), new ResourceBundle(10f, 0f, 0f)));
            m.Restore(1, new ResourceBundle(4f, 0f, 0f), true);
            Assert.AreEqual("B", m.CurrentStage.DisplayName);
            Assert.AreEqual(0.4f, m.Progress, 1e-5f);
            Assert.IsTrue(m.IsActive);
        }
    }
}
