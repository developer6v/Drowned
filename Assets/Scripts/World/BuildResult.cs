namespace Drowned.World
{
    /// <summary>Resultado de construir ou melhorar uma estrutura (SCRUM-130 / SCRUM-134).</summary>
    public enum BuildResult
    {
        Ok = 0,
        /// <summary>Fora do mapa, célula não construível ou já ocupada.</summary>
        InvalidPosition = 1,
        /// <summary>Falta recurso. O chamador recebe quanto falta de cada um.</summary>
        InsufficientResources = 2,
        /// <summary>A estrutura já está no último nível.</summary>
        MaxLevel = 3,
        /// <summary>Id de estrutura desconhecido.</summary>
        NotFound = 4,
        /// <summary>Estrutura ou nível ainda não liberado por tecnologia.</summary>
        Locked = 5
    }

    public static class BuildResultExtensions
    {
        /// <summary>Mensagem curta em português para a HUD.</summary>
        public static string Message(this BuildResult result) => result switch
        {
            BuildResult.Ok => "Construído.",
            BuildResult.InvalidPosition => "Não é possível construir aqui.",
            BuildResult.InsufficientResources => "Recursos insuficientes.",
            BuildResult.MaxLevel => "Nível máximo atingido.",
            BuildResult.NotFound => "Estrutura não encontrada.",
            BuildResult.Locked => "Requer tecnologia.",
            _ => "?"
        };
    }
}
