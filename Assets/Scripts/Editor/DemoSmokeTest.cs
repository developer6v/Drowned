#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Drowned.Core;
using Drowned.Resources;
using Drowned.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drowned.Editor
{
    /// <summary>
    /// Smoke test da demo (SCRUM-188): monta a cena da demo por código, entra em Play, deixa a partida rodar
    /// alguns dias simulados, constrói uma estrutura e confere que nada quebrou. Serve para validar a build
    /// jogável sem ninguém clicar, inclusive em batch:
    ///
    /// <code>
    /// Unity.exe -batchmode -nographics -projectPath . -executeMethod Drowned.Editor.DemoSmokeTest.Run -logFile smoke.log
    /// </code>
    ///
    /// Em batch o processo sai com código 0 (ok) ou 1 (falhou). No editor, menu <b>Drowned → Smoke test da demo</b>
    /// e o resultado vai para o Console. Sobrevive aos domain reloads de entrar/sair do Play via <see cref="SessionState"/>.
    /// </summary>
    public static class DemoSmokeTest
    {
        private const string KeyActive = "drowned.smoke.active";
        private const string KeyBatch = "drowned.smoke.batch";
        private const string KeyResult = "drowned.smoke.result"; // "", "ok" ou "fail"
        private const int WarmupFrames = 10;
        private const int DaysToSimulate = 5;

        private static int _frames;
        private static bool _done;

        [MenuItem("Drowned/Smoke test da demo", priority = 2)]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Smoke] saia do Play antes de rodar.");
                return;
            }
            SessionState.SetBool(KeyActive, true);
            SessionState.SetBool(KeyBatch, Application.isBatchMode);
            SessionState.SetString(KeyResult, "");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("DemoBootstrap").AddComponent<DemoBootstrap>();
            Debug.Log("[Smoke] cena montada, entrando em Play...");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;

            string result = SessionState.GetString(KeyResult, "");
            if (!EditorApplication.isPlayingOrWillChangePlaymode && result != "")
            {
                // Voltamos do Play: encerra
                SessionState.EraseBool(KeyActive);
                bool batch = SessionState.GetBool(KeyBatch, false);
                SessionState.EraseString(KeyResult);
                Debug.Log($"[Smoke] resultado final: {result.ToUpperInvariant()}");
                if (batch) EditorApplication.Exit(result == "ok" ? 0 : 1);
                return;
            }

            _frames = 0;
            _done = false;
            EditorApplication.update -= Step;
            EditorApplication.update += Step;
        }

        private static void Step()
        {
            if (_done || !EditorApplication.isPlaying) return;
            _frames++;
            if (_frames < WarmupFrames) return;

            var gm = GameManagerBehaviour.Instance;
            if (gm == null || gm.Game == null)
            {
                if (_frames > 300) Finish(false, "GameManagerBehaviour não apareceu em 300 frames");
                return;
            }

            var errors = new List<string>();
            try
            {
                Check(gm, errors);
            }
            catch (Exception e)
            {
                errors.Add($"exceção: {e}");
            }
            Finish(errors.Count == 0, errors.Count == 0 ? "tudo ok" : string.Join("\n  - ", errors));
        }

        private static void Check(GameManagerBehaviour gm, List<string> errors)
        {
            var game = gm.Game;
            var rules = UnityEngine.Resources.Load<GameRules>("GameRules");
            Debug.Log(rules != null
                ? "[Smoke] GameRules.asset da planilha carregado"
                : "[Smoke] GameRules.asset não existe; a demo está usando DemoContent (rode Drowned → Gerar dados da planilha)");

            // 1. Começo de partida
            int initialStructures = game.Structures.Instances.Count();
            int initialPop = game.Population.Population;
            var initialStock = game.Resources.Stock;
            Debug.Log($"[Smoke] dia {game.Clock.Day}: {initialStructures} estruturas, {initialPop} habitantes, estoque E{initialStock.Energy:0} O{initialStock.Oxygen:0} A{initialStock.Food:0}");
            if (initialStructures == 0) errors.Add("nenhuma estrutura inicial colocada");
            if (initialPop <= 0) errors.Add("população inicial zero");
            if (initialStock.IsZero) errors.Add("estoque inicial zero");
            if (game.State != GameState.Playing) errors.Add($"estado inicial {game.State}, esperado Playing");

            // 2. Objetos da cena
            foreach (var name in new[] { "GameClock", "Grid", "HUD", "BuildMode", "Structures", "Monument" })
                if (GameObject.Find(name) == null) errors.Add($"objeto '{name}' não está na cena");
            if (AudioManager.Instance == null) errors.Add("AudioManager não está na cena");
            if (Camera.main == null) errors.Add("sem Main Camera");

            // 3. Sprites das estruturas (vindos do Blender via planilha) ou procedurais (DemoContent)
            int withSprite = 0, total = 0;
            foreach (var s in game.Structures.Instances)
            {
                total++;
                if (s.CurrentLevel.Sprite != null) withSprite++;
            }
            if (withSprite < total) errors.Add($"{total - withSprite} de {total} estruturas sem sprite");
            if (game.Monument.CurrentSprite == null) errors.Add("Monumento sem sprite");

            // 4. Simula dias. Advance dispara os ticks de verdade (recursos, população, colapso).
            float dayLength = game.Clock.SecondsPerDay;
            for (int d = 0; d < DaysToSimulate; d++) game.Clock.Advance(dayLength);
            Debug.Log($"[Smoke] após {DaysToSimulate} dias: dia {game.Clock.Day}, {game.Population.Population} habitantes, " +
                      $"estoque E{game.Resources.Stock.Energy:0} O{game.Resources.Stock.Oxygen:0} A{game.Resources.Stock.Food:0}, estado {game.State}");
            if (game.Clock.Day < 1 + DaysToSimulate) errors.Add($"relógio só chegou ao dia {game.Clock.Day}");
            if (game.State != GameState.Playing) errors.Add($"partida terminou em {game.State} em {DaysToSimulate} dias com os valores iniciais; balanceamento inicial inviável");
            if (game.Population.Population < game.Rules.Population.MinimumPopulation) errors.Add("população abaixo do mínimo");

            // 5. Constrói a estrutura mais barata num lugar livre
            var catalog = game.Rules.Structures.Where(s => s != null && game.Structures.IsUnlocked(s)).OrderBy(s => s.BuildCost.Total).ToList();
            if (catalog.Count == 0) { errors.Add("catálogo de estruturas vazio"); return; }
            var cheapest = catalog[0];
            var stockBefore = game.Resources.Stock;
            bool built = false;
            for (int y = 0; y < game.Grid.Height && !built; y++)
                for (int x = 0; x < game.Grid.Width && !built; x++)
                {
                    if (game.Structures.CanBuild(cheapest, x, y, out _) != BuildResult.Ok) continue;
                    var result = game.Structures.TryBuild(cheapest, x, y, out var instance, out var missing);
                    if (result != BuildResult.Ok) { errors.Add($"TryBuild({cheapest.Id}) em ({x},{y}) deu {result}, faltando {missing}"); return; }
                    built = true;
                    Debug.Log($"[Smoke] construiu {cheapest.DisplayName} em ({x},{y}), id {instance.Id}");
                }
            if (!built) { errors.Add($"não achou lugar nem saldo para construir {cheapest.Id} (estoque {stockBefore}, custo {game.Structures.BuildCostOf(cheapest)})"); return; }
            var cost = game.Structures.BuildCostOf(cheapest);
            var expected = stockBefore - cost;
            var actual = game.Resources.Stock;
            if (Mathf.Abs(expected.Total - actual.Total) > 0.01f) errors.Add($"estoque após construir: esperado {expected}, obtido {actual}");
            if (game.Structures.Instances.Count() != initialStructures + 1) errors.Add("contagem de estruturas não aumentou em 1");
            if (GameObject.Find("Structures").transform.childCount != initialStructures + 1)
                errors.Add($"cena tem {GameObject.Find("Structures").transform.childCount} views de estrutura, esperado {initialStructures + 1}");

            // 6. Mais um dia depois de construir, nada explode
            game.Clock.Advance(dayLength);
            if (game.State != GameState.Playing) errors.Add($"partida terminou em {game.State} logo após construir");
        }

        private static void Finish(bool ok, string message)
        {
            _done = true;
            EditorApplication.update -= Step;
            if (ok) Debug.Log($"[Smoke] OK: {message}");
            else Debug.LogError($"[Smoke] FALHOU:\n  - {message}");
            SessionState.SetString(KeyResult, ok ? "ok" : "fail");
            EditorApplication.ExitPlaymode();
        }
    }
}
#endif
