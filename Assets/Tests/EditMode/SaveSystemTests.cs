using System.Collections.Generic;
using System.IO;
using System.Linq;
using Drowned.Core;
using Drowned.Monument;
using Drowned.Population;
using Drowned.Research;
using Drowned.Resources;
using Drowned.Save;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class SaveSystemTests
    {
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        private GameRules _rules;
        private StructureData _house, _lab;
        private TechnologyData _modular;
        private string _tempRoot;

        [SetUp]
        public void SetUp()
        {
            _house = Track(StructureData.Create("housing", "Moradia", StructureCategory.Housing, 1, 1,
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), ResourceBundle.Zero, new ResourceBundle(1f, 0f, 0f), 4),
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), ResourceBundle.Zero, new ResourceBundle(2f, 0f, 0f), 10)));
            _house.SetUnlockRequirements(false, 2);
            _lab = Track(StructureData.Create("lab", "Laboratório", StructureCategory.Research, 1, 1,
                new StructureLevelData(new ResourceBundle(10f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero, researchPerDay: 24f)));
            _modular = Track(TechnologyData.Create("modular", "Engenharia modular", 10f, null, TechnologyEffect.HousingBonus(0.5f)));
            var engineer = Track(SpecialistData.Create("engineer", "Engenheiro", 8, SpecialistBonus.EnergyProduction, 0.2f));
            var monument = Track(MonumentData.Create(
                new MonumentStage("Fundação", new ResourceBundle(10f, 0f, 0f), new ResourceBundle(24f, 0f, 0f)),
                new MonumentStage("Estrutura", new ResourceBundle(50f, 0f, 0f), new ResourceBundle(24f, 0f, 0f))));

            _rules = Track(GameRules.Create(3, new ResourceBundle(200f, 200f, 200f),
                new PopulationConfig(5, 1, 1, 1, ResourceBundle.Zero),
                new GameRules.InitialStructure(_house, 0, 0)));
            _rules.SetStructureCatalog(new[] { _house, _lab });
            _rules.SetSpecialists(new[] { engineer });
            _rules.SetTechnologies(new[] { _modular });
            _rules.SetMonument(monument);

            _tempRoot = Path.Combine(Path.GetTempPath(), "drowned-tests-" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.RootOverride = _tempRoot;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var a in _assets) UnityEngine.Object.DestroyImmediate(a);
            _assets.Clear();
            SaveSystem.RootOverride = null;
            SaveSystem.PendingLoad = null;
            if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true);
        }

        private T Track<T>(T asset) where T : UnityEngine.Object
        {
            _assets.Add(asset);
            return asset;
        }

        private GameManager NewGame()
        {
            var grid = new GridMap(6, 6);
            grid.SetBuildableRect(0, 0, 6, 6, true);
            return new GameManager(_rules, new GameClock(24f, 24), grid);
        }

        private GameManager PlayedGame()
        {
            var game = NewGame();
            game.PlaceInitialStructures();
            game.Structures.TryBuild(_lab, 2, 2, out _, out _);
            game.Structures.TryBuild(_house, 1, 1, out var h2, out _);
            game.Population.SetPopulation(8);                 // desbloqueia o engenheiro
            game.Research.StartResearch(_modular);
            for (int i = 0; i < 10; i++) game.Research.Tick(); // 10 pontos: conclui (+50% moradia)
            game.Structures.TryUpgrade(h2.Id, out _);
            game.Monument.SetActive(true);
            game.Monument.Tick();                              // entrega 1 de energia
            game.Clock.Advance(30f);                           // dia 2, tick 6
            return game;
        }

        [Test]
        public void CapturaAplicaEOsEstadosBatem()
        {
            var source = PlayedGame();
            var data = SaveSystem.Capture(source);
            var json = SaveSystem.ToJson(data);
            var parsed = SaveSystem.FromJson(json);
            Assert.IsNotNull(parsed);

            var target = NewGame();
            target.PlaceInitialStructures(); // simula a cena: a cidade inicial é descartada pelo Apply
            int placed = SaveSystem.Apply(target, parsed);

            Assert.AreEqual(3, placed);
            Assert.AreEqual(source.Resources.Stock, target.Resources.Stock);
            Assert.AreEqual(source.Population.Population, target.Population.Population);
            Assert.AreEqual(9, target.Population.Population); // cresceu 1 na virada do dia
            Assert.AreEqual(source.Population.HousingCapacity, target.Population.HousingCapacity);
            Assert.AreEqual(1.5f, target.Population.HousingMultiplier, 1e-5f);
            Assert.IsTrue(target.Specialists.IsUnlocked("engineer"));
            Assert.AreEqual(1.2f, target.Resources.ProductionMultiplier.Energy, 1e-5f);
            Assert.IsTrue(target.Research.IsResearched("modular"));
            Assert.AreEqual(source.Research.StoredPoints, target.Research.StoredPoints, 1e-4f);
            Assert.AreEqual(source.Resources.NetPerDay, target.Resources.NetPerDay);
            Assert.AreEqual(24f, target.Research.PointsPerDay);

            var srcStructs = source.Structures.Instances.OrderBy(i => i.Id).Select(i => (i.Data.Id, i.X, i.Y, i.Level)).ToArray();
            var dstStructs = target.Structures.Instances.OrderBy(i => i.Id).Select(i => (i.Data.Id, i.X, i.Y, i.Level)).ToArray();
            Assert.AreEqual(srcStructs, dstStructs);

            // a Fundação (10 de energia) concluiu durante os 30 ticks; a Estrutura recebeu o resto
            Assert.AreEqual(1, source.Monument.CurrentStageIndex);
            Assert.AreEqual(source.Monument.CurrentStageIndex, target.Monument.CurrentStageIndex);
            Assert.AreEqual(source.Monument.Delivered, target.Monument.Delivered);
            Assert.AreEqual(new ResourceBundle(21f, 0f, 0f), target.Monument.Delivered);
            Assert.IsTrue(target.Monument.IsActive);
            Assert.AreEqual(source.Stats.MonumentStagesCompleted, target.Stats.MonumentStagesCompleted);
            Assert.AreEqual(2, target.Clock.Day);
            Assert.AreEqual(6, target.Clock.TickOfDay);
            Assert.AreEqual(30.0, target.Clock.PlayedSeconds, 1e-6);
            Assert.AreEqual(source.Stats.StructuresBuilt, target.Stats.StructuresBuilt);
            Assert.AreEqual(source.Stats.PeakPopulation, target.Stats.PeakPopulation);
            Assert.AreEqual(GameState.Playing, target.State);
        }

        [Test]
        public void IdDesconhecidoEIgnorado()
        {
            var data = SaveSystem.Capture(PlayedGame());
            data.structures.Add(new SaveData.StructureSave { id = "nao_existe", x = 4, y = 4 });
            data.research.researched.Add("tec_removida");
            data.specialists.Add("ninguem");

            var target = NewGame();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("nao_existe"));
            int placed = SaveSystem.Apply(target, data);
            Assert.AreEqual(3, placed);
            Assert.AreEqual(1, target.Research.Researched.Count);
            Assert.AreEqual(1, target.Specialists.UnlockedCount);
        }

        [Test]
        public void GravaELeArquivoNoSlot()
        {
            Assert.IsFalse(SaveSystem.HasSave(1));
            var game = PlayedGame();
            SaveSystem.Save(game, 1);
            Assert.IsTrue(SaveSystem.HasSave(1));
            Assert.IsTrue(File.Exists(Path.Combine(_tempRoot, "slot1.json")));
            Assert.IsFalse(File.Exists(Path.Combine(_tempRoot, "slot1.json.tmp")));

            var loaded = SaveSystem.Load(1);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(game.Population.Population, loaded.population);
            Assert.AreEqual(3, loaded.structures.Count);
            Assert.IsNotNull(SaveSystem.SavedAt(1));

            SaveSystem.Save(game, 1); // sobrescreve sem erro
            Assert.IsTrue(SaveSystem.Delete(1));
            Assert.IsFalse(SaveSystem.Delete(1));
            Assert.IsNull(SaveSystem.Load(1));
        }

        [Test]
        public void JsonCorrompidoDevolveNull()
        {
            Directory.CreateDirectory(_tempRoot);
            File.WriteAllText(SaveSystem.PathFor(2), "{ isso não é json");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.IsNull(SaveSystem.Load(2));
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
