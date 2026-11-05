using System;

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
    }
}
