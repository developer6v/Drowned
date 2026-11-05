using System;
using System.Collections.Generic;

namespace Drowned.World
{
    /// <summary>Uma decoração escolhida: célula do grid e qual variação (índice na lista de sprites).</summary>
    public readonly struct DecorPlacement
    {
        public int X { get; }
        public int Y { get; }
        public int Variant { get; }
        /// <summary>Espelhar na horizontal, para variar sem precisar de mais sprites.</summary>
        public bool FlipX { get; }

        public DecorPlacement(int x, int y, int variant, bool flipX)
        {
            X = x;
            Y = y;
            Variant = variant;
            FlipX = flipX;
        }
    }

    /// <summary>
    /// Escolhe onde vão as decorações do mapa (SCRUM-183): só em células do mapa que não são construíveis nem
    /// ocupadas, fora das áreas excluídas (ex.: o Monumento), com densidade e semente fixas. Lógica pura, para teste;
    /// a parte visual fica em <see cref="DecorScatterBehaviour"/>.
    /// </summary>
    public static class DecorLayout
    {
        public static List<DecorPlacement> Pick(GridMap grid, int variantCount, float density, int seed,
            Func<int, int, bool> excluded = null)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var result = new List<DecorPlacement>();
            if (variantCount <= 0 || density <= 0f) return result;
            if (density > 1f) density = 1f;

            var rng = new Random(seed);
            for (int y = 0; y < grid.Height; y++)
                for (int x = 0; x < grid.Width; x++)
                {
                    // Sorteia sempre, para a mesma semente dar o mesmo mapa mesmo que a regra de exclusão mude
                    double roll = rng.NextDouble();
                    int variant = rng.Next(variantCount);
                    bool flip = rng.Next(2) == 1;
                    if (grid.IsBuildable(x, y) || grid.IsOccupied(x, y)) continue;
                    if (excluded != null && excluded(x, y)) continue;
                    if (roll >= density) continue;
                    result.Add(new DecorPlacement(x, y, variant, flip));
                }
            return result;
        }
    }
}
