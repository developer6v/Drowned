using Drowned.Resources;

namespace Drowned.World
{
    /// <summary>
    /// Quem decide o que está liberado para construir e melhorar (SCRUM-148). Implementado pelo ResearchManager.
    /// Sem regras (null) tudo está liberado.
    /// </summary>
    public interface IUnlockRules
    {
        bool IsStructureUnlocked(StructureData data);
        bool IsLevelUnlocked(StructureData data, int level);
    }
}
