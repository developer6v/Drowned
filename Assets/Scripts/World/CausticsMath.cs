using System;

namespace Drowned.World
{
    /// <summary>
    /// Padrão de cáusticas (a luz da superfície focada pelas ondas, desenhando redes claras no fundo do mar),
    /// separado para teste. Soma de três "cristas" de seno em direções diferentes, com uma distorção de domínio
    /// para as linhas ficarem onduladas. Todas as frequências são inteiras, então a textura repete sem emenda
    /// com período 1 em u e em v.
    /// </summary>
    public static class CausticsMath
    {
        // Direções (frequências inteiras) das três cristas e da distorção
        private static readonly int[,] Ridges = { { 3, 1 }, { -1, 3 }, { 2, -3 } };
        private static readonly int[,] Warps = { { 1, 2 }, { 2, -1 }, { -2, 1 } };

        /// <summary>Intensidade 0..1 no ponto (u, v) da textura. <paramref name="sharpness"/> afina as linhas.</summary>
        public static float Intensity(float u, float v, float sharpness = 4f)
        {
            if (sharpness <= 0f) throw new ArgumentOutOfRangeException(nameof(sharpness));
            double sum = 0.0;
            for (int i = 0; i < 3; i++)
            {
                double warp = 0.18 * Math.Sin(2.0 * Math.PI * (Warps[i, 0] * u + Warps[i, 1] * v));
                double phase = Math.PI * (Ridges[i, 0] * u + Ridges[i, 1] * v) + warp * Math.PI;
                double ridge = 1.0 - Math.Abs(Math.Sin(phase)); // 1 na crista, 0 entre cristas
                sum += Math.Pow(ridge, sharpness);
            }
            return (float)Math.Min(1.0, Math.Max(0.0, sum / 1.5));
        }
    }
}
