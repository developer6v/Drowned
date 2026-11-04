#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Drowned.Core;
using Drowned.Monument;
using Drowned.Population;
using Drowned.Research;
using Drowned.Resources;
using UnityEditor;
using UnityEngine;

namespace Drowned.Editor
{
    /// <summary>
    /// Gera os ScriptableObjects de <c>Assets/Data</c> a partir da planilha de balanceamento
    /// (<c>docs/balanceamento/*.csv</c>, SCRUM-118). Menu <b>Drowned → Gerar dados da planilha</b>.
    ///
    /// Formato de cada aba está em <c>docs/balanceamento/README.md</c>. Toda a validação acontece antes
    /// de gravar: se houver qualquer erro, nada é escrito e os erros vão para o Console com arquivo e linha.
    /// Assets existentes são atualizados no lugar (mesmo GUID), então referências em cenas e prefabs não quebram.
    /// </summary>
    public static class BalanceSheetImporter
    {
        private const string CsvFolder = "docs/balanceamento";
        private const string DataFolder = "Assets/Data";
        private const string RulesAssetPath = DataFolder + "/Resources/GameRules.asset";
        private const string MonumentAssetPath = DataFolder + "/Monument.asset";
        private const string SpritesFolder = "Assets/Sprites";

        [MenuItem("Drowned/Gerar dados da planilha", priority = 1)]
        public static void Generate()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string csvDir = Path.Combine(root, CsvFolder);
            if (!Directory.Exists(csvDir))
            {
                Debug.LogError($"[Planilha] pasta não encontrada: {csvDir}");
                return;
            }

            var errors = new List<string>();
            var model = Read(csvDir, errors);
            if (errors.Count > 0)
            {
                foreach (var e in errors) Debug.LogError("[Planilha] " + e);
                Debug.LogError($"[Planilha] {errors.Count} erro(s). Nada foi gravado.");
                return;
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                Write(model);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log($"[Planilha] OK: {model.Structures.Count} estruturas, {model.Specialists.Count} especialistas, " +
                      $"{model.Technologies.Count} tecnologias, {model.MonumentStages.Count} etapas do Monumento → {DataFolder}");
        }

        // ------------------------------------------------------------------ modelo intermediário
        private sealed class Model
        {
            public float SecondsPerDay, TicksPerDay, DaysToCollapse;
            public int InitialPopulation;
            public ResourceBundle InitialStock;
            public PopulationConfig Population;
            public CityLevelConfig CityLevel;
            public readonly List<(string id, int x, int y, int level)> InitialStructures = new List<(string, int, int, int)>();
            public readonly List<StructureDef> Structures = new List<StructureDef>();
            public readonly List<SpecialistDef> Specialists = new List<SpecialistDef>();
            public readonly List<TechnologyDef> Technologies = new List<TechnologyDef>();
            public string RuinSprite;
            public readonly List<MonumentStageDef> MonumentStages = new List<MonumentStageDef>();
        }

        private sealed class StructureDef
        {
            public string Id, Name; public StructureCategory Category; public int W, H; public bool RequiresTech; public int BaseMaxLevel;
            public readonly List<(ResourceBundle cost, ResourceBundle prod, ResourceBundle cons, int housing, float research, string sprite)> Levels =
                new List<(ResourceBundle, ResourceBundle, ResourceBundle, int, float, string)>();
        }

        private sealed class SpecialistDef { public string Id, Name, Description; public int Threshold; public SpecialistBonus Bonus; public float Value; }

        private sealed class TechnologyDef
        {
            public string Id, Name, Description; public float Cost;
            public readonly List<string> Prerequisites = new List<string>();
            public readonly List<TechnologyEffect> Effects = new List<TechnologyEffect>();
        }

        private sealed class MonumentStageDef
        {
            public string Name, Sprite; public ResourceBundle Required, Contribution; public int MinPopulation;
            public readonly List<string> Specialists = new List<string>();
            public readonly List<string> Technologies = new List<string>();
        }

        // ------------------------------------------------------------------ leitura + validação
        private static Model Read(string dir, List<string> errors)
        {
            var m = new Model();

            var tempo = ReadParams(Path.Combine(dir, "Tempo.csv"), errors);
            m.SecondsPerDay = Num(tempo, "secondsPerDay", 1f, errors, "Tempo.csv");
            m.TicksPerDay = Num(tempo, "ticksPerDay", 1f, errors, "Tempo.csv");
            m.DaysToCollapse = Num(tempo, "daysToCollapse", 1f, errors, "Tempo.csv");

            var inicial = ReadParams(Path.Combine(dir, "Inicial.csv"), errors);
            m.InitialPopulation = (int)Num(inicial, "initialPopulation", 0f, errors, "Inicial.csv");
            m.InitialStock = new ResourceBundle(
                Num(inicial, "initialEnergy", 0f, errors, "Inicial.csv"),
                Num(inicial, "initialOxygen", 0f, errors, "Inicial.csv"),
                Num(inicial, "initialFood", 0f, errors, "Inicial.csv"));

            var pop = ReadParams(Path.Combine(dir, "Populacao.csv"), errors);
            m.Population = new PopulationConfig(
                m.InitialPopulation,
                (int)Num(pop, "growthPerDay", 0f, errors, "Populacao.csv"),
                (int)Num(pop, "starvationLossPerDay", 0f, errors, "Populacao.csv"),
                (int)Num(pop, "minimumPopulation", 0f, errors, "Populacao.csv"),
                new ResourceBundle(
                    Num(pop, "consumptionEnergy", 0f, errors, "Populacao.csv"),
                    Num(pop, "consumptionOxygen", 0f, errors, "Populacao.csv"),
                    Num(pop, "consumptionFood", 0f, errors, "Populacao.csv")));

            var nivel = ReadParams(Path.Combine(dir, "NivelCidade.csv"), errors);
            m.CityLevel = new CityLevelConfig(
                Num(nivel, "populationWeight", 0f, errors, "NivelCidade.csv"),
                Num(nivel, "structureLevelWeight", 0f, errors, "NivelCidade.csv"),
                Num(nivel, "technologyWeight", 0f, errors, "NivelCidade.csv"),
                Num(nivel, "pointsPerLevel", 1f, errors, "NivelCidade.csv"),
                (int)Num(nivel, "maxLevel", 1f, errors, "NivelCidade.csv"));

            ReadStructures(Path.Combine(dir, "Estruturas.csv"), m, errors);
            ReadInitialStructures(Path.Combine(dir, "EstruturasIniciais.csv"), m, errors);
            ReadSpecialists(Path.Combine(dir, "Especialistas.csv"), m, errors);
            ReadTechnologies(Path.Combine(dir, "Tecnologias.csv"), m, errors);
            ReadMonument(Path.Combine(dir, "Monumento.csv"), m, errors);
            return m;
        }

        private static void ReadStructures(string path, Model m, List<string> errors)
        {
            var byId = new Dictionary<string, StructureDef>();
            foreach (var row in ReadTable(path, errors, "id", "nome", "categoria", "largura", "altura", "requer_tecnologia", "nivel_max_base",
                         "nivel", "custo_energia", "custo_oxigenio", "custo_alimento", "prod_energia", "prod_oxigenio", "prod_alimento",
                         "cons_energia", "cons_oxigenio", "cons_alimento", "moradia", "pesquisa_dia", "sprite"))
            {
                string id = row.Get("id");
                if (string.IsNullOrEmpty(id)) { errors.Add(row.Where("id vazio")); continue; }
                int level = row.Int("nivel", 1, errors);
                if (!byId.TryGetValue(id, out var def))
                {
                    if (level != 1) { errors.Add(row.Where($"a primeira linha de '{id}' precisa ser o nível 1")); continue; }
                    def = new StructureDef
                    {
                        Id = id, Name = row.Get("nome"), Category = row.Enum<StructureCategory>("categoria", errors),
                        W = row.Int("largura", 1, errors), H = row.Int("altura", 1, errors),
                        RequiresTech = row.Int("requer_tecnologia", 0, errors) != 0, BaseMaxLevel = row.Int("nivel_max_base", 1, errors),
                    };
                    byId[id] = def;
                    m.Structures.Add(def);
                }
                else if (level != def.Levels.Count + 1)
                {
                    errors.Add(row.Where($"nível {level} de '{id}' fora de ordem (esperado {def.Levels.Count + 1})"));
                    continue;
                }
                def.Levels.Add((
                    row.Bundle("custo", errors), row.Bundle("prod", errors), row.Bundle("cons", errors),
                    row.Int("moradia", 0, errors), row.Float("pesquisa_dia", 0f, errors), row.Get("sprite")));
            }
        }

        private static void ReadInitialStructures(string path, Model m, List<string> errors)
        {
            foreach (var row in ReadTable(path, errors, "id", "x", "y", "nivel"))
            {
                string id = row.Get("id");
                var def = m.Structures.Find(s => s.Id == id);
                if (def == null) { errors.Add(row.Where($"estrutura '{id}' não existe em Estruturas.csv")); continue; }
                int level = row.Int("nivel", 1, errors);
                if (level > def.Levels.Count) errors.Add(row.Where($"'{id}' não tem nível {level}"));
                m.InitialStructures.Add((id, row.Int("x", 0, errors), row.Int("y", 0, errors), level));
            }
        }

        private static void ReadSpecialists(string path, Model m, List<string> errors)
        {
            var seen = new HashSet<string>();
            foreach (var row in ReadTable(path, errors, "id", "nome", "populacao_minima", "bonus", "valor", "descricao"))
            {
                string id = row.Get("id");
                if (!seen.Add(id)) { errors.Add(row.Where($"id duplicado '{id}'")); continue; }
                m.Specialists.Add(new SpecialistDef
                {
                    Id = id, Name = row.Get("nome"), Description = row.Get("descricao"),
                    Threshold = row.Int("populacao_minima", 1, errors), Bonus = row.Enum<SpecialistBonus>("bonus", errors),
                    Value = row.Float("valor", 0f, errors),
                });
            }
        }

        private static void ReadTechnologies(string path, Model m, List<string> errors)
        {
            var seen = new HashSet<string>();
            foreach (var row in ReadTable(path, errors, "id", "nome", "custo", "prerequisitos", "efeitos", "descricao"))
            {
                string id = row.Get("id");
                if (!seen.Add(id)) { errors.Add(row.Where($"id duplicado '{id}'")); continue; }
                var def = new TechnologyDef { Id = id, Name = row.Get("nome"), Description = row.Get("descricao"), Cost = row.Float("custo", 1f, errors) };
                def.Prerequisites.AddRange(SplitList(row.Get("prerequisitos")));
                foreach (var spec in SplitList(row.Get("efeitos")))
                {
                    var effect = ParseEffect(spec, m, out string err);
                    if (effect == null) errors.Add(row.Where(err));
                    else def.Effects.Add(effect);
                }
                m.Technologies.Add(def);
            }
            // Pré-requisitos existem e não formam ciclo
            var ids = new HashSet<string>(m.Technologies.ConvertAll(t => t.Id));
            foreach (var t in m.Technologies)
                foreach (var p in t.Prerequisites)
                    if (!ids.Contains(p)) errors.Add($"Tecnologias.csv: '{t.Id}' depende de '{p}', que não existe");
            foreach (var t in m.Technologies)
                if (HasCycle(t.Id, m, new HashSet<string>())) { errors.Add($"Tecnologias.csv: ciclo de pré-requisitos passando por '{t.Id}'"); break; }
        }

        private static bool HasCycle(string id, Model m, HashSet<string> stack)
        {
            if (!stack.Add(id)) return true;
            var t = m.Technologies.Find(x => x.Id == id);
            if (t != null)
                foreach (var p in t.Prerequisites)
                    if (HasCycle(p, m, stack)) return true;
            stack.Remove(id);
            return false;
        }

        private static TechnologyEffect ParseEffect(string spec, Model m, out string error)
        {
            error = null;
            var parts = spec.Split(':');
            string kind = parts[0].Trim();
            try
            {
                switch (kind)
                {
                    case "UnlockStructure":
                        if (parts.Length != 2) break;
                        if (m.Structures.Find(s => s.Id == parts[1]) == null) { error = $"efeito '{spec}': estrutura '{parts[1]}' não existe"; return null; }
                        return TechnologyEffect.UnlockStructure(parts[1]);
                    case "UnlockLevel":
                        if (parts.Length != 3) break;
                        var def = m.Structures.Find(s => s.Id == parts[1]);
                        if (def == null) { error = $"efeito '{spec}': estrutura '{parts[1]}' não existe"; return null; }
                        int level = int.Parse(parts[2], CultureInfo.InvariantCulture);
                        if (level > def.Levels.Count) { error = $"efeito '{spec}': '{parts[1]}' não tem nível {level}"; return null; }
                        return TechnologyEffect.UnlockLevel(parts[1], level);
                    case "ProductionBonus":
                        if (parts.Length != 3) break;
                        return TechnologyEffect.ProductionBonus((ResourceType)Enum.Parse(typeof(ResourceType), parts[1], true), ParseFloat(parts[2]));
                    case "ResearchBonus":
                        if (parts.Length != 2) break;
                        return TechnologyEffect.ResearchBonus(ParseFloat(parts[1]));
                    case "HousingBonus":
                        if (parts.Length != 2) break;
                        return TechnologyEffect.HousingBonus(ParseFloat(parts[1]));
                }
            }
            catch (Exception e)
            {
                error = $"efeito '{spec}' inválido: {e.Message}";
                return null;
            }
            error = $"efeito '{spec}' inválido (ver README para a sintaxe)";
            return null;
        }

        private static void ReadMonument(string path, Model m, List<string> errors)
        {
            int expected = 0;
            foreach (var row in ReadTable(path, errors, "etapa", "nome", "req_energia", "req_oxigenio", "req_alimento",
                         "contrib_energia", "contrib_oxigenio", "contrib_alimento", "populacao_minima", "especialistas", "tecnologias", "sprite"))
            {
                int stage = row.Int("etapa", 0, errors);
                if (stage != expected) { errors.Add(row.Where($"etapa {stage} fora de ordem (esperado {expected})")); continue; }
                expected++;
                if (stage == 0) { m.RuinSprite = row.Get("sprite"); continue; }
                var def = new MonumentStageDef
                {
                    Name = row.Get("nome"), Sprite = row.Get("sprite"),
                    Required = row.Bundle("req", errors), Contribution = row.Bundle("contrib", errors),
                    MinPopulation = row.Int("populacao_minima", 0, errors),
                };
                def.Specialists.AddRange(SplitList(row.Get("especialistas")));
                def.Technologies.AddRange(SplitList(row.Get("tecnologias")));
                foreach (var s in def.Specialists)
                    if (m.Specialists.Find(x => x.Id == s) == null) errors.Add(row.Where($"especialista '{s}' não existe"));
                foreach (var t in def.Technologies)
                    if (m.Technologies.Find(x => x.Id == t) == null) errors.Add(row.Where($"tecnologia '{t}' não existe"));
                m.MonumentStages.Add(def);
            }
            if (m.MonumentStages.Count == 0) errors.Add("Monumento.csv: precisa de pelo menos uma etapa além da ruína");
        }

        // ------------------------------------------------------------------ escrita
        private static void Write(Model m)
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Resources"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Structures"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Specialists"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Technologies"));

            var structures = new Dictionary<string, StructureData>();
            foreach (var def in m.Structures)
            {
                var levels = new StructureLevelData[def.Levels.Count];
                for (int i = 0; i < levels.Length; i++)
                {
                    var l = def.Levels[i];
                    levels[i] = new StructureLevelData(l.cost, l.prod, l.cons, l.housing, LoadSprite(l.sprite), l.research);
                }
                var fresh = StructureData.Create(def.Id, def.Name, def.Category, def.W, def.H, levels);
                fresh.SetUnlockRequirements(def.RequiresTech, def.BaseMaxLevel);
                structures[def.Id] = Persist(fresh, $"{DataFolder}/Structures/{def.Id}.asset");
            }

            var specialists = new Dictionary<string, SpecialistData>();
            foreach (var def in m.Specialists)
            {
                var fresh = SpecialistData.Create(def.Id, def.Name, def.Threshold, def.Bonus, def.Value, def.Description);
                specialists[def.Id] = Persist(fresh, $"{DataFolder}/Specialists/{def.Id}.asset");
            }

            // Tecnologias em duas passadas: primeiro os assets, depois as referências entre eles.
            var technologies = new Dictionary<string, TechnologyData>();
            foreach (var def in m.Technologies)
            {
                var fresh = TechnologyData.Create(def.Id, def.Name, def.Cost, null, def.Effects.ToArray());
                SetDescription(fresh, def.Description);
                technologies[def.Id] = Persist(fresh, $"{DataFolder}/Technologies/{def.Id}.asset");
            }
            foreach (var def in m.Technologies)
            {
                var so = new SerializedObject(technologies[def.Id]);
                var list = so.FindProperty("_prerequisites");
                list.arraySize = def.Prerequisites.Count;
                for (int i = 0; i < def.Prerequisites.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = technologies[def.Prerequisites[i]];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var stages = new MonumentStage[m.MonumentStages.Count];
            for (int i = 0; i < stages.Length; i++)
            {
                var s = m.MonumentStages[i];
                stages[i] = new MonumentStage(s.Name, s.Required, s.Contribution, s.MinPopulation, s.Specialists, s.Technologies, LoadSprite(s.Sprite));
            }
            var freshMonument = MonumentData.Create(stages);
            freshMonument.SetRuinSprite(LoadSprite(m.RuinSprite));
            var monument = Persist(freshMonument, MonumentAssetPath);

            var initial = new GameRules.InitialStructure[m.InitialStructures.Count];
            for (int i = 0; i < initial.Length; i++)
            {
                var (id, x, y, level) = m.InitialStructures[i];
                initial[i] = new GameRules.InitialStructure(structures[id], x, y, level);
            }
            var freshRules = GameRules.Create((int)m.DaysToCollapse, m.InitialStock, m.Population, initial);
            freshRules.SetTime(m.SecondsPerDay, (int)m.TicksPerDay);
            freshRules.SetStructureCatalog(m.Structures.ConvertAll(s => structures[s.Id]));
            freshRules.SetSpecialists(m.Specialists.ConvertAll(s => specialists[s.Id]));
            freshRules.SetTechnologies(m.Technologies.ConvertAll(t => technologies[t.Id]));
            freshRules.SetMonument(monument);
            freshRules.SetCityLevel(m.CityLevel);
            Persist(freshRules, RulesAssetPath);
        }

        /// <summary>Grava <paramref name="fresh"/> em <paramref name="path"/>, atualizando o asset existente no lugar se houver.</summary>
        private static T Persist<T>(T fresh, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                fresh.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(fresh);
            return existing;
        }

        private static void SetDescription(TechnologyData tech, string description)
        {
            var so = new SerializedObject(tech);
            so.FindProperty("_description").stringValue = description ?? "";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>"Structures/Housing_L1" ou "Structures/Housing_L1:N" → sprite do sheet gerado pelo Blender.</summary>
        private static Sprite LoadSprite(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec)) return null;
            string dir = "S";
            string key = spec.Trim();
            int colon = key.IndexOf(':');
            if (colon >= 0) { dir = key.Substring(colon + 1).Trim(); key = key.Substring(0, colon); }
            string name = Path.GetFileName(key);
            string png = $"{SpritesFolder}/{key}/{name}_sheet.png";
            string wanted = $"{name}_{dir}_000";
            foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(png))
                if (obj is Sprite sprite && sprite.name == wanted) return sprite;
            Debug.LogWarning($"[Planilha] sprite '{spec}' não encontrado ({png} / {wanted}). Rode tools/blender/render_batch.py. Ficou vazio.");
            return null;
        }

        // ------------------------------------------------------------------ CSV
        private sealed class Row
        {
            public string File; public int Line; public Dictionary<string, string> Cells;
            public string Get(string col) => Cells.TryGetValue(col, out var v) ? v.Trim() : "";
            public string Where(string msg) => $"{File} linha {Line}: {msg}";

            public float Float(string col, float min, List<string> errors)
            {
                string v = Get(col);
                if (!float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                {
                    errors.Add(Where($"'{col}' = '{v}' não é um número (use ponto como decimal)"));
                    return min;
                }
                if (f < min) errors.Add(Where($"'{col}' = {v} abaixo do mínimo {min}"));
                return f;
            }

            public int Int(string col, int min, List<string> errors)
            {
                float f = Float(col, min, errors);
                if (Mathf.Abs(f - Mathf.Round(f)) > 0.0001f) errors.Add(Where($"'{col}' = {f} precisa ser inteiro"));
                return Mathf.RoundToInt(f);
            }

            public TEnum Enum<TEnum>(string col, List<string> errors) where TEnum : struct
            {
                string v = Get(col);
                if (System.Enum.TryParse(v, true, out TEnum result)) return result;
                errors.Add(Where($"'{col}' = '{v}' inválido; opções: {string.Join(", ", System.Enum.GetNames(typeof(TEnum)))}"));
                return default;
            }

            public ResourceBundle Bundle(string prefix, List<string> errors) =>
                new ResourceBundle(Float(prefix + "_energia", 0f, errors), Float(prefix + "_oxigenio", 0f, errors), Float(prefix + "_alimento", 0f, errors));
        }

        private static Dictionary<string, string> ReadParams(string path, List<string> errors)
        {
            var result = new Dictionary<string, string>();
            foreach (var row in ReadTable(path, errors, "Chave", "Valor"))
            {
                string key = row.Get("Chave");
                if (string.IsNullOrEmpty(key)) continue;
                if (result.ContainsKey(key)) { errors.Add(row.Where($"chave duplicada '{key}'")); continue; }
                result[key] = row.Get("Valor");
            }
            return result;
        }

        private static float Num(Dictionary<string, string> p, string key, float min, List<string> errors, string file)
        {
            if (!p.TryGetValue(key, out var v)) { errors.Add($"{file}: falta a chave '{key}'"); return min; }
            if (!float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) { errors.Add($"{file}: '{key}' = '{v}' não é um número"); return min; }
            if (f < min) errors.Add($"{file}: '{key}' = {v} abaixo do mínimo {min}");
            return f;
        }

        private static List<Row> ReadTable(string path, List<string> errors, params string[] requiredColumns)
        {
            var rows = new List<Row>();
            string file = Path.GetFileName(path);
            if (!File.Exists(path)) { errors.Add($"{file}: arquivo não encontrado em {CsvFolder}"); return rows; }

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) { errors.Add($"{file}: vazio"); return rows; }
            var header = SplitCsvLine(lines[0].TrimStart('﻿'));
            for (int i = 0; i < header.Count; i++) header[i] = header[i].Trim();
            foreach (var col in requiredColumns)
                if (!header.Contains(col)) errors.Add($"{file}: falta a coluna '{col}' no cabeçalho");

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cells = SplitCsvLine(lines[i]);
                var row = new Row { File = file, Line = i + 1, Cells = new Dictionary<string, string>() };
                for (int c = 0; c < header.Count; c++) row.Cells[header[c]] = c < cells.Count ? cells[c] : "";
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>Divide uma linha por ';' respeitando aspas duplas ("" dentro de aspas vira ").</summary>
        private static List<string> SplitCsvLine(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (quoted)
                {
                    if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (ch == '"') quoted = false;
                    else sb.Append(ch);
                }
                else if (ch == '"') quoted = true;
                else if (ch == ';') { cells.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
            cells.Add(sb.ToString());
            return cells;
        }

        private static IEnumerable<string> SplitList(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) yield break;
            foreach (var part in value.Split('|'))
            {
                string p = part.Trim();
                if (p.Length > 0) yield return p;
            }
        }

        private static float ParseFloat(string s) => float.Parse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
#endif
