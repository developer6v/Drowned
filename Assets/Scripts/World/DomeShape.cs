using System;

namespace Drowned.World
{
    /// <summary>
    /// Forma do domo de proteção (SCRUM-168) em coordenadas normalizadas, separada para teste.
    /// x vai de -1 (esquerda) a 1 (direita); y = 0 é a linha do centro da base, positivo para cima.
    /// A base é uma elipse achatada (o chão em isométrico, semi-eixo vertical <paramref name="baseRy"/>) e a cúpula é
    /// meia elipse por cima (altura <paramref name="height"/>). Valores em unidades da meia largura.
    /// </summary>
    public static class DomeShape
    {
        /// <summary>Distância até a borda, normalizada: &lt; 1 dentro, 1 na borda, &gt; 1 fora.</summary>
        public static float Edge(float x, float y, float baseRy, float height)
        {
            if (baseRy <= 0f || height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));
            float ry = y >= 0f ? height : baseRy;
            return (float)Math.Sqrt(x * x + (y / ry) * (y / ry));
        }

        /// <summary>
        /// Opacidade do vidro: miolo quase transparente, borda mais forte (efeito de vidro visto de lado),
        /// zero fora. <paramref name="rimWidth"/> é a espessura do brilho da borda (0..1).
        /// </summary>
        public static float Alpha(float x, float y, float baseRy, float height, float fill, float rim, float rimWidth)
        {
            float e = Edge(x, y, baseRy, height);
            if (e > 1f) return 0f;
            float t = rimWidth <= 0f ? 0f : Math.Max(0f, 1f - (1f - e) / rimWidth); // 0 no miolo, 1 na borda
            return Math.Min(1f, fill + rim * t * t);
        }
    }
}
