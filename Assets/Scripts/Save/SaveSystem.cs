using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Drowned.Core;
using UnityEngine;

namespace Drowned.Save
{
    /// <summary>
    /// Grava e lê a partida em JSON (SCRUM-176).
    ///
    /// <see cref="Capture"/> tira a foto de um <see cref="GameManager"/>; <see cref="Apply"/> restaura a foto em um
    /// GameManager <b>recém-criado</b> (os multiplicadores de bônus partem de 1 e são reaplicados pelos Restore de cada sistema).
    /// Na cena, <see cref="GameManagerBehaviour"/> lê <see cref="PendingLoad"/> ao iniciar e aplica no lugar da cidade inicial.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>Save a aplicar quando a cena de jogo carregar. O menu principal preenche; o GameManagerBehaviour consome.</summary>
        public static SaveData PendingLoad { get; set; }

        /// <summary>Pasta raiz dos saves. Testes trocam por uma pasta temporária.</summary>
        public static string RootOverride { get; set; }

        public static string Root => string.IsNullOrEmpty(RootOverride)
            ? Path.Combine(Application.persistentDataPath, "saves")
            : RootOverride;

        public static string PathFor(int slot) => Path.Combine(Root, $"slot{Math.Max(1, slot)}.json");

        // ------------------------------------------------------------------ captura e aplicação

        public static SaveData Capture(GameManager game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            var data = new SaveData
            {
                savedAt = DateTime.Now.ToString("o"),
                clock = new SaveData.ClockSave
                {
                    day = game.Clock.Day,
                    tickOfDay = game.Clock.TickOfDay,
                    simulatedSeconds = game.Clock.SimulatedSeconds,
                    playedSeconds = game.Clock.PlayedSeconds
                },
                stock = game.Resources.Stock,
                population = game.Population.Population,
                specialists = game.Specialists.Unlocked.Select(s => s.Id).ToList(),
                research = new SaveData.ResearchSave
                {
                    researched = game.Research.Researched.Select(t => t.Id).ToList(),
                    current = game.Research.Current != null ? game.Research.Current.Id : null,
                    currentPoints = game.Research.CurrentPoints,
                    storedPoints = game.Research.StoredPoints
                },
                monument = new SaveData.MonumentSave
                {
                    stage = game.Monument.CurrentStageIndex,
                    delivered = game.Monument.Delivered,
                    active = game.Monument.IsActive
                },
                collapseDays = game.Collapse.ConsecutiveDays,
                stats = new SaveData.StatsSave
                {
                    peakPopulation = game.Stats.PeakPopulation,
                    structuresBuilt = game.Stats.StructuresBuilt,
                    upgradesDone = game.Stats.UpgradesDone,
                    technologiesResearched = game.Stats.TechnologiesResearched,
                    monumentStagesCompleted = game.Stats.MonumentStagesCompleted
                }
            };

            foreach (var s in game.Structures.Instances.OrderBy(i => i.Id))
                data.structures.Add(new SaveData.StructureSave { id = s.Data.Id, x = s.X, y = s.Y, level = s.Level });

            return data;
        }

        /// <summary>
        /// Restaura a foto em um GameManager recém-criado. Ids desconhecidos são ignorados com aviso.
        /// Devolve quantas estruturas foram recolocadas.
        /// </summary>
        public static int Apply(GameManager game, SaveData data)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.version > SaveData.CurrentVersion)
                Debug.LogWarning($"Save de versão {data.version} mais nova que a suportada ({SaveData.CurrentVersion}); tentando mesmo assim.");

            // Limpa o que já estava no mapa (ex.: cidade inicial).
            foreach (int id in game.Structures.Instances.Select(i => i.Id).ToList()) game.Structures.Remove(id);

            // Bônus primeiro, para moradia e produção já saírem multiplicados.
            game.Research.Restore(data.research.researched ?? new List<string>(), data.research.current, data.research.currentPoints, data.research.storedPoints);
            game.Specialists.Restore(data.specialists ?? new List<string>());

            int placed = 0;
            foreach (var s in data.structures ?? new List<SaveData.StructureSave>())
            {
                var structure = game.Rules.FindStructure(s.id);
                if (structure == null)
                {
                    Debug.LogWarning($"Save: estrutura '{s.id}' não existe mais no jogo; ignorada.");
                    continue;
                }
                int level = structure.HasLevel(s.level) ? s.level : 1;
                if (game.Structures.PlaceFree(structure, s.x, s.y, level) != null) placed++;
                else Debug.LogWarning($"Save: não foi possível recolocar '{s.id}' em ({s.x},{s.y}).");
            }

            game.Population.SetPopulation(Math.Max(0, data.population));
            game.Resources.SetStock(data.stock);
            game.Monument.Restore(data.monument.stage, data.monument.delivered, data.monument.active);
            game.Collapse.Restore(Math.Max(0, data.collapseDays), collapsed: false);
            game.Stats.Restore(data.stats.peakPopulation, data.stats.structuresBuilt, data.stats.upgradesDone,
                data.stats.technologiesResearched, data.stats.monumentStagesCompleted);

            int ticksPerDay = game.Clock.TicksPerDay;
            game.Clock.Restore(Math.Max(1, data.clock.day), Mathf.Clamp(data.clock.tickOfDay, 0, ticksPerDay - 1),
                data.clock.simulatedSeconds, data.clock.playedSeconds);
            return placed;
        }

        // ------------------------------------------------------------------ JSON

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, prettyPrint: true);

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonUtility.FromJson<SaveData>(json); }
            catch (Exception e)
            {
                Debug.LogWarning($"Save inválido: {e.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------------ arquivos

        public static bool HasSave(int slot = 1) => File.Exists(PathFor(slot));

        /// <summary>Grava de forma atômica: escreve em .tmp e renomeia por cima do arquivo final.</summary>
        public static void Save(SaveData data, int slot = 1)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            string path = PathFor(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, ToJson(data));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static void Save(GameManager game, int slot = 1) => Save(Capture(game), slot);

        /// <summary>Lê o slot. Null se não existe ou está corrompido.</summary>
        public static SaveData Load(int slot = 1)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;
            try { return FromJson(File.ReadAllText(path)); }
            catch (Exception e)
            {
                Debug.LogWarning($"Não foi possível ler o save '{path}': {e.Message}");
                return null;
            }
        }

        public static bool Delete(int slot = 1)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>Data da gravação do slot, para o menu. Null se não há save.</summary>
        public static DateTime? SavedAt(int slot = 1)
        {
            var data = Load(slot);
            if (data == null || string.IsNullOrEmpty(data.savedAt)) return null;
            return DateTime.TryParse(data.savedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
        }
    }
}
