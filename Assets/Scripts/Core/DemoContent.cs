using System.Collections.Generic;
using Drowned.Monument;
using Drowned.Population;
using Drowned.Research;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.Core
{
    /// <summary>
    /// Conteúdo de demonstração criado em memória: as propostas de números registradas no Jira
    /// (SCRUM-119/120/121/143/149/150/163) até a planilha (SCRUM-118) virar assets em <c>Assets/Data</c>.
    /// Quando os assets existirem, arraste o <c>GameRules</c> no GameManagerBehaviour e este arquivo deixa de ser usado.
    /// </summary>
    public static class DemoContent
    {
        public static GameRules CreateRules(out List<Object> created)
        {
            created = new List<Object>();

            // ---------------------------------------------------------------- estruturas (SCRUM-143)
            var housing = StructureData.Create("housing", "Moradia", StructureCategory.Housing, 1, 1,
                new StructureLevelData(new ResourceBundle(20f, 0f, 10f), ResourceBundle.Zero, new ResourceBundle(1f, 1f, 0f), 6, DemoArt.Building(DemoArt.Housing, 1, 36)),
                new StructureLevelData(new ResourceBundle(40f, 0f, 20f), ResourceBundle.Zero, new ResourceBundle(2f, 2f, 0f), 10, DemoArt.Building(DemoArt.Housing, 1, 56)));
            var farm = StructureData.Create("algae_farm", "Fazenda de algas", StructureCategory.Food, 2, 2,
                new StructureLevelData(new ResourceBundle(30f, 10f, 0f), new ResourceBundle(0f, 0f, 12f), new ResourceBundle(3f, 0f, 0f), 0, DemoArt.Building(DemoArt.Food, 2, 24)),
                new StructureLevelData(new ResourceBundle(60f, 20f, 0f), new ResourceBundle(0f, 0f, 28f), new ResourceBundle(5f, 0f, 0f), 0, DemoArt.Building(DemoArt.Food, 2, 40)));
            var generator = StructureData.Create("generator", "Gerador", StructureCategory.Energy, 1, 1,
                new StructureLevelData(new ResourceBundle(40f, 0f, 10f), new ResourceBundle(24f, 0f, 0f), ResourceBundle.Zero, 0, DemoArt.Building(DemoArt.Energy, 1, 44)),
                new StructureLevelData(new ResourceBundle(80f, 0f, 20f), new ResourceBundle(55f, 0f, 0f), ResourceBundle.Zero, 0, DemoArt.Building(DemoArt.Energy, 1, 64)));
            var oxygen = StructureData.Create("oxygen_plant", "Planta de oxigênio", StructureCategory.Oxygen, 1, 1,
                new StructureLevelData(new ResourceBundle(30f, 0f, 10f), new ResourceBundle(0f, 16f, 0f), new ResourceBundle(4f, 0f, 0f), 0, DemoArt.Building(DemoArt.Oxygen, 1, 40)),
                new StructureLevelData(new ResourceBundle(60f, 0f, 20f), new ResourceBundle(0f, 36f, 0f), new ResourceBundle(6f, 0f, 0f), 0, DemoArt.Building(DemoArt.Oxygen, 1, 60)));
            var lab = StructureData.Create("lab", "Laboratório", StructureCategory.Research, 2, 2,
                new StructureLevelData(new ResourceBundle(60f, 20f, 20f), ResourceBundle.Zero, new ResourceBundle(5f, 2f, 0f), 0, DemoArt.Building(DemoArt.Research, 2, 36), researchPerDay: 20f),
                new StructureLevelData(new ResourceBundle(120f, 40f, 40f), ResourceBundle.Zero, new ResourceBundle(8f, 3f, 0f), 0, DemoArt.Building(DemoArt.Research, 2, 52), researchPerDay: 45f));
            var structures = new[] { housing, farm, generator, oxygen, lab };
            created.AddRange(structures);

            // ---------------------------------------------------------------- especialistas (SCRUM-150)
            var engineer = SpecialistData.Create("engineer", "Engenheiro", 10, SpecialistBonus.EnergyProduction, 0.20f, "Otimiza os geradores da cidade.");
            var biologist = SpecialistData.Create("biologist", "Biólogo", 15, SpecialistBonus.FoodProduction, 0.20f, "Cultiva algas mais produtivas.");
            var scientist = SpecialistData.Create("scientist", "Cientista", 20, SpecialistBonus.ResearchSpeed, 0.25f, "Acelera a pesquisa.");
            var architect = SpecialistData.Create("architect", "Arquiteto", 30, SpecialistBonus.BuildCostDiscount, 0.15f, "Projeta construções mais baratas.");
            var specialists = new[] { engineer, biologist, scientist, architect };
            created.AddRange(specialists);

            // ---------------------------------------------------------------- tecnologias (SCRUM-149)
            var algae = TechnologyData.Create("algae", "Algas resistentes", 40f, null, TechnologyEffect.ProductionBonus(ResourceType.Food, 0.2f));
            var turbines = TechnologyData.Create("turbines", "Turbinas eficientes", 50f, null, TechnologyEffect.ProductionBonus(ResourceType.Energy, 0.2f));
            var filters = TechnologyData.Create("filters", "Filtros de membrana", 50f, null, TechnologyEffect.ProductionBonus(ResourceType.Oxygen, 0.2f));
            var modular = TechnologyData.Create("modular", "Engenharia modular", 60f, null, TechnologyEffect.UnlockLevel("housing", 2));
            var hydro = TechnologyData.Create("hydroponics", "Hidroponia vertical", 80f, new[] { algae }, TechnologyEffect.UnlockLevel("algae_farm", 2));
            var reactor = TechnologyData.Create("geothermal", "Reator geotérmico", 100f, new[] { turbines }, TechnologyEffect.UnlockLevel("generator", 2));
            var photo = TechnologyData.Create("photosynthesis", "Fotossíntese artificial", 100f, new[] { filters }, TechnologyEffect.UnlockLevel("oxygen_plant", 2));
            var compact = TechnologyData.Create("compact_housing", "Habitação compacta", 120f, new[] { modular }, TechnologyEffect.HousingBonus(0.25f));
            var computing = TechnologyData.Create("computing", "Computação submersa", 150f, new[] { hydro, reactor }, TechnologyEffect.ResearchBonus(0.3f), TechnologyEffect.UnlockLevel("lab", 2));
            var monumental = TechnologyData.Create("monumental", "Arquitetura monumental", 200f, new[] { compact });
            var technologies = new[] { algae, turbines, filters, modular, hydro, reactor, photo, compact, computing, monumental };
            created.AddRange(technologies);

            // ---------------------------------------------------------------- monumento (SCRUM-163)
            var monument = MonumentData.Create(
                new MonumentStage("Fundação", new ResourceBundle(300f, 200f, 150f), new ResourceBundle(30f, 20f, 15f), 10, null, null, DemoArt.Building(DemoArt.Monument, 4, 30)),
                new MonumentStage("Estrutura", new ResourceBundle(600f, 400f, 300f), new ResourceBundle(50f, 35f, 25f), 15, new[] { "engineer" }, new[] { "modular" }, DemoArt.Building(DemoArt.Monument, 4, 70)),
                new MonumentStage("Sistemas", new ResourceBundle(1000f, 700f, 500f), new ResourceBundle(80f, 60f, 40f), 25, new[] { "engineer", "scientist" }, new[] { "geothermal", "monumental" }, DemoArt.Building(DemoArt.Monument, 4, 110)),
                new MonumentStage("Acabamento", new ResourceBundle(1500f, 1000f, 800f), new ResourceBundle(120f, 80f, 60f), 30, new[] { "engineer", "biologist", "scientist", "architect" }, new[] { "computing" }, DemoArt.Building(new Color(1f, 0.9f, 0.6f), 4, 150)));
            monument.SetRuinSprite(RuinSprite());
            created.Add(monument);

            // ---------------------------------------------------------------- regras (SCRUM-119/120/121)
            var rules = GameRules.Create(3, new ResourceBundle(100f, 100f, 80f),
                new PopulationConfig(5, 1, 1, 1, new ResourceBundle(1f, 1.5f, 1f)),
                new GameRules.InitialStructure(housing, 6, 6),
                new GameRules.InitialStructure(generator, 8, 6),
                new GameRules.InitialStructure(oxygen, 6, 8),
                new GameRules.InitialStructure(farm, 8, 8));
            rules.SetStructureCatalog(structures);
            rules.SetSpecialists(specialists);
            rules.SetTechnologies(technologies);
            rules.SetMonument(monument);
            created.Add(rules);
            return rules;
        }

        /// <summary>Sprite da ruína do Monumento (estágio 0).</summary>
        public static Sprite RuinSprite() => DemoArt.Building(new Color(0.45f, 0.45f, 0.5f), 4, 16);

        /// <summary>Ícones dos recursos na ordem energia, oxigênio, alimento.</summary>
        public static Sprite[] ResourceIcons() => new[]
        {
            DemoArt.Dot(DemoArt.Energy),
            DemoArt.Dot(DemoArt.Oxygen),
            DemoArt.Dot(DemoArt.Food)
        };
    }
}
