namespace Drowned.Resources
{
    /// <summary>Categorias da barra inferior de construção (GDD, SCRUM-141).</summary>
    public enum StructureCategory
    {
        Housing = 0,
        Food = 1,
        Energy = 2,
        Oxygen = 3,
        Research = 4,
        Monument = 5
    }

    public static class StructureCategoryExtensions
    {
        /// <summary>Nome em português para a HUD.</summary>
        public static string Label(this StructureCategory category) => category switch
        {
            StructureCategory.Housing => "Moradia",
            StructureCategory.Food => "Alimento",
            StructureCategory.Energy => "Energia",
            StructureCategory.Oxygen => "Oxigênio",
            StructureCategory.Research => "Pesquisa",
            StructureCategory.Monument => "Monumento",
            _ => "?"
        };
    }
}
