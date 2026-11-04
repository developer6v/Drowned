using Drowned.Resources;

namespace Drowned.World
{
    /// <summary>Uma estrutura construída no mapa (SCRUM-130). Criada só pelo <see cref="StructureManager"/>.</summary>
    public sealed class StructureInstance
    {
        /// <summary>Id único na partida. É o mesmo id usado no <see cref="GridMap"/>.</summary>
        public int Id { get; }
        public StructureData Data { get; }

        /// <summary>Nível atual, começando em 1.</summary>
        public int Level { get; internal set; }

        /// <summary>Canto inferior esquerdo da pegada no GridMap.</summary>
        public int X { get; }
        public int Y { get; }

        internal StructureInstance(int id, StructureData data, int level, int x, int y)
        {
            Id = id;
            Data = data;
            Level = level;
            X = x;
            Y = y;
        }

        public StructureLevelData CurrentLevel => Data.GetLevel(Level);
        public bool IsMaxLevel => !Data.HasLevel(Level + 1);

        /// <summary>Id da fonte desta estrutura no ResourceManager.</summary>
        public string ResourceSourceId => $"structure:{Id}";

        public override string ToString() => $"{Data.DisplayName} n{Level} #{Id} em ({X},{Y})";
    }
}
