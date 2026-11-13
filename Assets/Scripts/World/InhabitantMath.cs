using System;
using System.Collections.Generic;

namespace Drowned.World
{
    /// <summary>
    /// Regras dos habitantes que andam pelo mapa (SCRUM-139/126), separadas para teste.
    /// O sheet do habitante tem 4 direções na ordem do render (S, W, N, E). Com a câmera do Blender a 45°,
    /// S olha para baixo-esquerda na tela, W para baixo-direita, N para cima-direita e E para cima-esquerda.
    /// </summary>
    public static class InhabitantMath
    {
        public const int FacingDownLeft = 0;  // S
        public const int FacingDownRight = 1; // W
        public const int FacingUpRight = 2;   // N
        public const int FacingUpLeft = 3;    // E

        /// <summary>Índice de direção do sheet para um movimento (dx, dy) na tela. Parado mantém <paramref name="current"/>.</summary>
        public static int DirectionIndex(float dx, float dy, int current = FacingDownLeft)
        {
            if (Math.Abs(dx) < 1e-5f && Math.Abs(dy) < 1e-5f) return current;
            if (dy <= 0f) return dx < 0f ? FacingDownLeft : FacingDownRight;
            return dx >= 0f ? FacingUpRight : FacingUpLeft;
        }

        /// <summary>Quantos habitantes desenhar: a população, limitada por <paramref name="max"/> para não pesar.</summary>
        public static int WalkerCount(int population, int max) => Math.Max(0, Math.Min(population, max));

        /// <summary>Balanço vertical do passo (0 a <paramref name="height"/>), com <paramref name="stepsPerSecond"/> passos por segundo.</summary>
        public static float StepBob(float time, float phase, float stepsPerSecond, float height)
        {
            return height * (float)Math.Abs(Math.Sin((time * stepsPerSecond + phase) * Math.PI));
        }

        /// <summary>Célula onde o habitante pode pisar: dentro do mapa, construível e sem estrutura (SCRUM-199).</summary>
        public static bool IsWalkable(GridMap grid, int x, int y) => grid.IsBuildable(x, y) && !grid.IsOccupied(x, y);

        /// <summary>
        /// Caminho mais curto em grade (4 vizinhos, BFS) de (fx, fy) até (tx, ty), andando só por células livres,
        /// para os habitantes contornarem as construções (SCRUM-199). A lista começa no primeiro passo depois da
        /// origem e termina no destino; vazia se já está lá. A origem pode estar bloqueada (estrutura construída em
        /// cima do habitante): ele sai por uma vizinha livre. Null se o destino estiver bloqueado ou inalcançável.
        /// <paramref name="maxVisited"/> limita o custo em mapas grandes.
        /// </summary>
        public static List<(int x, int y)> FindPath(GridMap grid, int fx, int fy, int tx, int ty, int maxVisited = 4096)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (!grid.InBounds(fx, fy) || !IsWalkable(grid, tx, ty)) return null;
            var result = new List<(int x, int y)>();
            if (fx == tx && fy == ty) return result;

            int w = grid.Width;
            var cameFrom = new Dictionary<int, int>();
            var queue = new Queue<int>();
            int start = fy * w + fx, goal = ty * w + tx;
            cameFrom[start] = -1;
            queue.Enqueue(start);
            // Ordem fixa dos vizinhos: resultado determinístico (bom para teste)
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            bool found = false;
            while (queue.Count > 0 && cameFrom.Count <= maxVisited)
            {
                int cur = queue.Dequeue();
                if (cur == goal) { found = true; break; }
                int cx = cur % w, cy = cur / w;
                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + dx[i], ny = cy + dy[i];
                    if (!IsWalkable(grid, nx, ny)) continue;
                    int key = ny * w + nx;
                    if (cameFrom.ContainsKey(key)) continue;
                    cameFrom[key] = cur;
                    queue.Enqueue(key);
                }
            }
            if (!found && !cameFrom.ContainsKey(goal)) return null;

            for (int node = goal; node != start; node = cameFrom[node]) result.Add((node % w, node / w));
            result.Reverse();
            return result;
        }

        /// <summary>
        /// Qual especialista (índice na ordem de desbloqueio) o mergulhador <paramref name="walkerIndex"/> representa,
        /// ou -1 se for um habitante comum (SCRUM-152). Os primeiros mergulhadores viram os especialistas, um cada.
        /// </summary>
        public static int SpecialistSlot(int walkerIndex, int unlockedCount, int walkerCount)
        {
            if (walkerIndex < 0 || walkerIndex >= walkerCount) return -1;
            return walkerIndex < unlockedCount ? walkerIndex : -1;
        }
    }
}
