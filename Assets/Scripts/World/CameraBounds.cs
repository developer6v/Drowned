using System;

namespace Drowned.World
{
    /// <summary>
    /// Matemática pura da câmera 2D (SCRUM-123): limita zoom e posição para a câmera ortográfica
    /// nunca mostrar além do mapa. Testável sem Unity.
    ///
    /// Convenção: <c>orthoSize</c> é a metade da altura visível (igual ao Camera.orthographicSize).
    /// </summary>
    public readonly struct CameraBounds
    {
        public readonly float MinX, MinY, MaxX, MaxY;

        public CameraBounds(float minX, float minY, float maxX, float maxY)
        {
            if (maxX < minX || maxY < minY) throw new ArgumentException("Limites invertidos.");
            MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
        }

        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterY => (MinY + MaxY) * 0.5f;

        /// <summary>
        /// Maior orthoSize que ainda cabe dentro dos limites, para o dado aspecto (largura/altura).
        /// Se o mapa for pequeno demais, a câmera fica centralizada e este valor é o teto.
        /// </summary>
        public float MaxOrthoSizeFor(float aspect)
        {
            float byHeight = Height * 0.5f;
            float byWidth = Width * 0.5f / aspect;
            return Math.Min(byHeight, byWidth);
        }

        /// <summary>Limita o zoom pedido entre min e o que cabe no mapa.</summary>
        public float ClampOrthoSize(float requested, float minOrthoSize, float maxOrthoSize, float aspect)
        {
            float cap = Math.Min(maxOrthoSize, MaxOrthoSizeFor(aspect));
            if (cap < minOrthoSize) cap = minOrthoSize;
            return Math.Clamp(requested, minOrthoSize, cap);
        }

        /// <summary>
        /// Limita a posição da câmera para a área visível ficar dentro dos limites.
        /// Se a área visível for maior que o mapa naquele eixo, centraliza.
        /// </summary>
        public (float x, float y) ClampPosition(float x, float y, float orthoSize, float aspect)
        {
            float halfH = orthoSize;
            float halfW = orthoSize * aspect;

            float cx = halfW * 2f >= Width ? CenterX : Math.Clamp(x, MinX + halfW, MaxX - halfW);
            float cy = halfH * 2f >= Height ? CenterY : Math.Clamp(y, MinY + halfH, MaxY - halfH);
            return (cx, cy);
        }

        /// <summary>Expande os limites em todas as direções (margem para ver o fundo além do terreno).</summary>
        public CameraBounds Expand(float margin) =>
            new(MinX - margin, MinY - margin, MaxX + margin, MaxY + margin);
    }
}
