using Drowned.Core;
using Drowned.Population;
using Drowned.Resources;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class GameStatsTests
    {
        [Test]
        public void ContaPorEventos()
        {
            var clock = new GameClock(24f, 24);
            var rm = new ResourceManager(new ResourceBundle(1000f, 1000f, 1000f), 24);
            var pm = new PopulationManager(new PopulationConfig(5, 3, 1, 1, ResourceBundle.Zero), rm, 20);
            var grid = new GridMap(5, 5);
            grid.SetBuildableRect(0, 0, 5, 5, true);
            var sm = new StructureManager(grid, rm, pm);
            var stats = new GameStats(clock, pm, sm);

            var farm = StructureData.Create("farm", "Fazenda", StructureCategory.Food, 1, 1,
                new StructureLevelData(new ResourceBundle(1f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero),
                new StructureLevelData(new ResourceBundle(1f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero));
            try
            {
                sm.TryBuild(farm, 0, 0, out var a, out _);
                sm.TryBuild(farm, 1, 1, out _, out _);
                sm.TryUpgrade(a.Id, out _);
                pm.AdvanceDay(); // 8
                pm.SetPopulation(2);
                stats.RegisterTechnology();
                stats.RegisterMonumentStage();
                stats.RegisterMonumentStage();
                clock.Advance(30f);

                Assert.AreEqual(2, stats.StructuresBuilt);
                Assert.AreEqual(1, stats.UpgradesDone);
                Assert.AreEqual(8, stats.PeakPopulation);
                Assert.AreEqual(2, stats.FinalPopulation);
                Assert.AreEqual(1, stats.TechnologiesResearched);
                Assert.AreEqual(2, stats.MonumentStagesCompleted);
                Assert.AreEqual(2, stats.Days);
                Assert.AreEqual(30.0, stats.PlayedSeconds, 1e-6);

                var summary = stats.Summary();
                Assert.AreEqual(8, summary.Count);
                Assert.AreEqual(("Pico de população", "8"), summary[3]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(farm);
            }
        }

        [Test]
        public void FormataDuracao()
        {
            Assert.AreEqual("0min 05s", GameStats.FormatDuration(5));
            Assert.AreEqual("12min 30s", GameStats.FormatDuration(750));
            Assert.AreEqual("1h 05min", GameStats.FormatDuration(3900));
        }
    }
}
