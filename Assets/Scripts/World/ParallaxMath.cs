using System;

namespace Drowned.World
{
    /// <summary>
    /// Matemática do parallax do fundo (SCRUM-169), separada para teste.
    /// Fator 0 = a camada fica parada no mundo (como o mapa); fator 1 = a camada anda junto com a câmera
    /// (infinitamente longe). Valores entre os dois dão a sensação de profundidade.
    /// </summary>
    public static class ParallaxMath
    {
        /// <summary>Posição da origem da camada para a câmera em <paramref name="camera"/>.</summary>
        public static float LayerOrigin(float camera, float factor) => camera * Clamp01(factor);

        /// <summary>
        /// Para uma camada feita de blocos repetidos de largura <paramref name="tileWidth"/>, devolve a posição do bloco
        /// que contém a câmera. Os vizinhos ficam em ±tileWidth. Assim três blocos sempre cobrem a visão,
        /// desde que tileWidth seja maior ou igual à largura visível.
        /// </summary>
        public static float TileBase(float camera, float factor, float tileWidth)
        {
            if (tileWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(tileWidth));
            float origin = LayerOrigin(camera, factor);
            return origin + (float)Math.Floor((camera - origin) / tileWidth) * tileWidth;
        }

        /// <summary>
        /// Leva <paramref name="value"/> para dentro de [min, max) dando a volta, para partículas que saem por um lado
        /// e voltam pelo outro.
        /// </summary>
        public static float Wrap(float value, float min, float max)
        {
            float range = max - min;
            if (range <= 0f) return min;
            float t = (value - min) % range;
            if (t < 0f) t += range;
            return min + t;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
