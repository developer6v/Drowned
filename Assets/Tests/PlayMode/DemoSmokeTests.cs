using System.Collections;
using System.Linq;
using Drowned.Core;
using Drowned.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Drowned.Tests
{
    /// <summary>
    /// Smoke test da demo jogável (SCRUM-188): monta a cena por código com <see cref="DemoBootstrap"/>,
    /// deixa a partida rodar alguns dias simulados, constrói uma estrutura e confere que nada quebrou.
    /// É o teste que diz "a demo abre e roda" sem ninguém clicar. Em batch:
    ///
    /// <code>
    /// Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults PlayMode.xml -logFile -
    /// </code>
    /// </summary>
    public class DemoSmokeTests
    {
        private const int DaysToSimulate = 5;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Cena vazia só para o teste (não precisa estar nas Build Settings)
            var scene = SceneManager.CreateScene("SmokeScene");
            SceneManager.SetActiveScene(scene);
            yield return null;
            new GameObject("DemoBootstrap").AddComponent<DemoBootstrap>();
            // Dois frames: Awake do DemoBootstrap monta tudo; os Behaviours ligam no Start.
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.transform.parent == null) Object.Destroy(go);
            yield return null;
            var scene = SceneManager.GetSceneByName("SmokeScene");
            if (scene.IsValid() && SceneManager.sceneCount > 1) yield return SceneManager.UnloadSceneAsync(scene);
        }

        /// <summary>A cena salva em Assets/Scenes/Demo.unity abre e monta o jogo sozinha.</summary>
        [UnityTest]
        public IEnumerator Cena_Demo_unity_abre_e_monta_o_jogo()
        {
#if UNITY_EDITOR
            const string path = "Assets/Scenes/Demo.unity";
            Assert.IsTrue(System.IO.File.Exists(path), $"{path} não existe; rode Drowned → Criar cena Demo");
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                path, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var gm = GameManagerBehaviour.Instance;
            Assert.IsNotNull(gm, "Demo.unity não criou o GameManager");
            Assert.IsNotNull(gm.Game);
            Assert.Greater(gm.Game.Structures.Instances.Count(), 0, "Demo.unity sem estruturas iniciais");
            Assert.IsNotNull(GameObject.Find("HUD"), "Demo.unity sem HUD");
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator Demo_abre_roda_cinco_dias_e_constroi()
        {
            var gm = GameManagerBehaviour.Instance;
            Assert.IsNotNull(gm, "GameManagerBehaviour não foi criado pelo DemoBootstrap");
            Assert.IsNotNull(gm.Game, "GameManager não inicializou");
            var game = gm.Game;

            bool fromAsset = UnityEngine.Resources.Load<GameRules>("GameRules") != null;
            Debug.Log(fromAsset
                ? "[Smoke] GameRules.asset da planilha carregado"
                : "[Smoke] GameRules.asset não existe; a demo usa DemoContent (rode Drowned → Gerar dados da planilha)");

            // 1. Começo de partida
            int initialStructures = game.Structures.Instances.Count();
            int initialPop = game.Population.Population;
            var initialStock = game.Resources.Stock;
            Debug.Log($"[Smoke] dia {game.Clock.Day}: {initialStructures} estruturas, {initialPop} habitantes, estoque {initialStock}");
            Assert.Greater(initialStructures, 0, "nenhuma estrutura inicial colocada");
            Assert.Greater(initialPop, 0, "população inicial zero");
            Assert.IsFalse(initialStock.IsZero, "estoque inicial zero");
            Assert.AreEqual(GameState.Playing, game.State);

            // 2. Objetos da cena
            foreach (var name in new[] { "GameClock", "Grid", "HUD", "BuildMode", "Structures", "Monument" })
                Assert.IsNotNull(GameObject.Find(name), $"objeto '{name}' não está na cena");
            Assert.IsNotNull(AudioManager.Instance, "AudioManager não está na cena");
            Assert.IsNotNull(Camera.main, "sem Main Camera");

            // 3. Sprites (do Blender via planilha, ou procedurais do DemoContent)
            foreach (var s in game.Structures.Instances)
                Assert.IsNotNull(s.CurrentLevel.Sprite, $"estrutura {s.Data.Id} nível {s.Level} sem sprite");
            Assert.IsNotNull(game.Monument.CurrentSprite, "Monumento sem sprite");

            // 4. Dias simulados: Advance dispara os ticks de verdade (recursos, população, colapso)
            float dayLength = game.Clock.SecondsPerDay;
            for (int d = 0; d < DaysToSimulate; d++)
            {
                game.Clock.Advance(dayLength);
                yield return null; // deixa HUD e views reagirem aos eventos
            }
            Debug.Log($"[Smoke] após {DaysToSimulate} dias: dia {game.Clock.Day}, {game.Population.Population} habitantes, " +
                      $"estoque {game.Resources.Stock}, estado {game.State}");
            Assert.GreaterOrEqual(game.Clock.Day, 1 + DaysToSimulate, "relógio não avançou os dias");
            Assert.AreEqual(GameState.Playing, game.State,
                $"partida terminou em {game.State} em {DaysToSimulate} dias com os valores iniciais: balanceamento inicial inviável");
            Assert.GreaterOrEqual(game.Population.Population, game.Rules.Population.MinimumPopulation);

            // 5. Constrói a estrutura desbloqueada mais barata no primeiro lugar livre
            var cheapest = game.Rules.Structures
                .Where(s => s != null && game.Structures.IsUnlocked(s))
                .OrderBy(s => s.BuildCost.Total).FirstOrDefault();
            Assert.IsNotNull(cheapest, "catálogo de estruturas vazio");
            var stockBefore = game.Resources.Stock;
            var cost = game.Structures.BuildCostOf(cheapest);
            Assert.IsTrue(stockBefore.Covers(cost), $"sem saldo para {cheapest.Id} após {DaysToSimulate} dias: estoque {stockBefore}, custo {cost}");

            StructureInstance built = null;
            for (int y = 0; y < game.Grid.Height && built == null; y++)
                for (int x = 0; x < game.Grid.Width && built == null; x++)
                {
                    if (game.Structures.CanBuild(cheapest, x, y, out _) != BuildResult.Ok) continue;
                    var result = game.Structures.TryBuild(cheapest, x, y, out built, out var missing);
                    Assert.AreEqual(BuildResult.Ok, result, $"TryBuild({cheapest.Id}) em ({x},{y}) faltando {missing}");
                }
            Assert.IsNotNull(built, $"não achou lugar livre para {cheapest.Id}");
            Debug.Log($"[Smoke] construiu {cheapest.DisplayName} em ({built.X},{built.Y})");
            yield return null;

            var expected = stockBefore - cost;
            Assert.AreEqual(expected.Total, game.Resources.Stock.Total, 0.01f, "estoque após construir");
            Assert.AreEqual(initialStructures + 1, game.Structures.Instances.Count(), "contagem de estruturas");
            Assert.AreEqual(initialStructures + 1, GameObject.Find("Structures").transform.childCount, "views de estrutura na cena");

            // 6. Mais um dia depois de construir, nada explode
            game.Clock.Advance(dayLength);
            yield return null;
            Assert.AreEqual(GameState.Playing, game.State, "partida terminou logo após construir");
        }
    }
}
