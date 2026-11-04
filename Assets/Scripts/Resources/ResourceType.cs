namespace Drowned.Resources
{
    /// <summary>Os três recursos da cidade (SCRUM-128). A ordem é a da barra superior da HUD.</summary>
    public enum ResourceType
    {
        Energy = 0,
        Oxygen = 1,
        Food = 2
    }

    public static class ResourceTypeExtensions
    {
        public const int Count = 3;

        /// <summary>Nome em português para a HUD.</summary>
        public static string Label(this ResourceType type) => type switch
        {
            ResourceType.Energy => "Energia",
            ResourceType.Oxygen => "Oxigênio",
            ResourceType.Food => "Alimento",
            _ => "?"
        };
    }
}
