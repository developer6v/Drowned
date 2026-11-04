using System.Collections.Generic;
using Drowned.Core;
using Drowned.Population;
using Drowned.Research;
using Drowned.Resources;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class CityLevelTests
    {
        [Test]
        public void CalculaScoreENivelPorEventos()
        {
            var resources = new ResourceManager(new ResourceBundle(1000f, 1000f, 1000f), 24);
            var population = new PopulationManager(new PopulationConfig(10, 1, 1, 1, ResourceBundle.Zero), resources, 100);
            var grid = new GridMap(5, 5);
            grid.SetBuildableRect(0, 0, 5, 5, true);
            var structures = new StructureManager(grid, resources, population);
            var tech = TechnologyData.Create("t", "Tec", 1f, null);
            var research = new ResearchManager(new[] { tech }, 24, resources, population, structures);
            var farm = StructureData.Create("farm", "Fazenda", StructureCategory.Food, 1, 1,
                new StructureLevelData(new ResourceBundle(1f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero),
                new StructureLevelData(new ResourceBundle(1f, 0f, 0f), ResourceBundle.Zero, ResourceBundle.Zero));
            farm.SetUnlockRequirements(false, 2);
            try
            {
                var city = new CityLevel(new CityLevelConfig(1f, 3f, 10f, 20f, 5), population, structures, research);
                var levels = new List<int>();
                city.OnLevelChanged += levels.Add;

                Assert.AreEqual(10f, city.Score);
                Assert.AreEqual(1, city.Level);
                Assert.AreEqual(0.5f, city.ProgressToNext, 1e-5f);

                structures.TryBuild(farm, 0, 0, out var a, out _); // +3 = 13
                structures.TryUpgrade(a.Id, out _);                 // +3 = 16
                population.SetPopulation(14);                       // 20 -> nível 2
                Assert.AreEqual(20f, city.Score);
                Assert.AreEqual(2, city.Level);

                research.Restore(new[] { "t" }, null, 0f, 0f);     // Restore não dispara OnResearched
                city.Recompute();                                   // 30
                Assert.AreEqual(30f, city.Score);
                Assert.AreEqual(2, city.Level);

                population.SetPopulation(100);                      // 116 -> nível 5 (máximo)
                Assert.AreEqual(5, city.Level);
                Assert.AreEqual(1f, city.ProgressToNext);
                Assert.AreEqual(new[] { 2, 5 }, levels);

                structures.Remove(a.Id);                            // 110 -> continua 5
                Assert.AreEqual(5, city.Level);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(farm);
                UnityEngine.Object.DestroyImmediate(tech);
            }
        }
    }
}
