using System.Collections.Generic;
using Drowned.Core;
using Drowned.Population;
using Drowned.Resources;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class GameManagerTests
    {
        private StructureData _house;
        private StructureData _generator;
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            _house = StructureData.Create("housing", "Moradia", StructureCategory.Housing, 1, 1,
                new StructureLevelData(new ResourceBundle(20f, 0f, 10f), ResourceBundle.Zero, new ResourceBundle(1f, 1f, 0f), housingCapacity: 4));
            _generator = StructureData.Create("generator", "Gerador", StructureCategory.Energy, 1, 1,
                new StructureLevelData(new ResourceBundle(30f, 0f, 0f), new ResourceBundle(24f, 0f, 0f), ResourceBundle.Zero));
            _assets.Add(_house);
            _assets.Add(_generator);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var a in _assets) UnityEngine.Object.DestroyImmediate(a);
            _assets.Clear();
        }

        private GameManager NewGame(int daysToCollapse = 3, float energy = 100f, float oxygen = 100f, float food = 80f, params GameRules.InitialStructure[] initial)
        {
            var rules = GameRules.Create(daysToCollapse, new ResourceBundle(energy, oxygen, food),
                new PopulationConfig(5, 1, 1, 1, new ResourceBundle(1f, 1f, 1f)), initial);
            _assets.Add(rules);
            var grid = new GridMap(8, 8);
            grid.SetBuildableRect(0, 0, 8, 8, true);
            return new GameManager(rules, new GameClock(24f, 24), grid); // 1 s por tick, 24 s por dia
        }

        [Test]
        public void ComecaJogandoComOsSistemasLigados()
        {
            var game = NewGame();
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(new ResourceBundle(100f, 100f, 80f), game.Resources.Stock);
            Assert.AreEqual(5, game.Population.Population);
            Assert.AreEqual(3, game.Collapse.DaysToCollapse);
            Assert.AreEqual(5, game.Stats.PeakPopulation);
        }

        [Test]
        public void TickDoRelogioAplicaRecursos()
        {
            var game = NewGame();
            // população 5 consome (5,5,5)/dia = (5/24 por tick)
            game.Clock.Advance(1f);
            Assert.AreEqual(100f - 5f / 24f, game.Resources.Stock.Energy, 1e-4f);
        }

        [Test]
        public void EstruturasIniciaisSaoColocadasSemCobrar()
        {
            var game = NewGame(initial: new[]
            {
                new GameRules.InitialStructure(_house, 0, 0),
                new GameRules.InitialStructure(_generator, 2, 2),
                new GameRules.InitialStructure(_generator, 2, 2) // posição repetida: ignorada
            });
            var built = new List<StructureInstance>();
            game.Structures.OnStructureBuilt += built.Add;

            Assert.AreEqual(2, game.PlaceInitialStructures());
            Assert.AreEqual(2, built.Count);
            Assert.AreEqual(new ResourceBundle(100f, 100f, 80f), game.Resources.Stock);
            Assert.AreEqual(4, game.Population.HousingCapacity);
            Assert.AreEqual(new ResourceBundle(24f - 1f - 5f, -1f - 5f, -5f), game.Resources.NetPerDay);
        }

        [Test]
        public void NovoDiaAvaliaColapsoEPopulacao()
        {
            var game = NewGame(initial: new GameRules.InitialStructure(_house, 0, 0));
            game.PlaceInitialStructures();
            game.Population.SetHousingCapacity(10);

            game.Clock.Advance(24f); // vira o dia 2
            Assert.AreEqual(2, game.Clock.Day);
            Assert.AreEqual(6, game.Population.Population);
            Assert.AreEqual(0, game.Collapse.ConsecutiveDays);
        }

        [Test]
        public void ColapsoLevaADerrotaEPausaORelogio()
        {
            var game = NewGame(daysToCollapse: 2, energy: 0f);
            var states = new List<GameState>();
            game.OnStateChanged += states.Add;

            game.Clock.Advance(24f); // dia 1 fechado: 1 dia em perigo
            Assert.AreEqual(GameState.Playing, game.State);
            game.Clock.Advance(24f); // 2 dias: colapso
            Assert.AreEqual(GameState.Defeat, game.State);
            Assert.IsTrue(game.IsOver);
            Assert.IsTrue(game.Clock.IsPaused);
            Assert.AreEqual(new[] { GameState.Defeat }, states);
        }

        [Test]
        public void VitoriaEDepoisNadaMaisMuda()
        {
            var game = NewGame(daysToCollapse: 1, energy: 0f);
            var states = new List<GameState>();
            game.OnStateChanged += states.Add;

            game.ReportVictory();
            Assert.AreEqual(GameState.Victory, game.State);
            Assert.IsTrue(game.Clock.IsPaused);

            game.Clock.SetSpeed(GameSpeed.Normal);
            game.Clock.Advance(24f); // colapsaria, mas a partida já acabou
            Assert.AreEqual(GameState.Victory, game.State);
            Assert.AreEqual(0, game.Collapse.ConsecutiveDays);
            Assert.AreEqual(new[] { GameState.Victory }, states);
        }

        [Test]
        public void DisposeDesligaDoRelogio()
        {
            var game = NewGame();
            game.Dispose();
            game.Clock.Advance(1f);
            Assert.AreEqual(100f, game.Resources.Stock.Energy);
        }
    }
}
