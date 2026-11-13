using System;
using Drowned.World;
using UnityEngine;

namespace Drowned.UI
{
    /// <summary>De que lado do alvo a seta fica. A seta sempre aponta para o alvo.</summary>
    public enum ArrowSide
    {
        /// <summary>Seta acima do alvo, apontando para baixo (alvo na metade de baixo da tela).</summary>
        Above = 0,
        /// <summary>Seta abaixo do alvo, apontando para cima (alvo na metade de cima da tela).</summary>
        Below = 1,
    }

    /// <summary>Onde desenhar a seta e o balão de um passo, em pixels da tela de referência (origem embaixo à esquerda).</summary>
    public readonly struct ArrowPlacement
    {
        /// <summary>Ponta da seta (encostada no alvo, com a folga).</summary>
        public Vector2 Tip { get; }
        public ArrowSide Side { get; }
        /// <summary>Rotação em graus de uma seta desenhada apontando para baixo.</summary>
        public float Angle => Side == ArrowSide.Above ? 0f : 180f;
        /// <summary>Centro do balão, já dentro da tela.</summary>
        public Vector2 BubbleCenter { get; }

        public ArrowPlacement(Vector2 tip, ArrowSide side, Vector2 bubbleCenter)
        {
            Tip = tip;
            Side = side;
            BubbleCenter = bubbleCenter;
        }
    }

    /// <summary>
    /// Contas de posição do tutorial visual (SCRUM-197), sem MonoBehaviour para dar para testar:
    /// os retângulos que escurecem a tela em volta do alvo, onde fica a seta e o balão, o balanço da seta,
    /// o lugar livre no grid para a seta do "construa aqui" e quando a câmera conta como movida.
    /// Todas as medidas em pixels da tela de referência, origem no canto de baixo à esquerda.
    /// </summary>
    public static class TutorialLayout
    {
        /// <summary>
        /// Quatro retângulos (cima, baixo, esquerda, direita) que cobrem a tela inteira menos o buraco
        /// (o alvo com a folga). Juntos não se sobrepõem. Partes do buraco fora da tela são cortadas.
        /// </summary>
        public static Rect[] DimRects(Rect screen, Rect hole, float padding)
        {
            float xMin = Mathf.Clamp(hole.xMin - padding, screen.xMin, screen.xMax);
            float xMax = Mathf.Clamp(hole.xMax + padding, screen.xMin, screen.xMax);
            float yMin = Mathf.Clamp(hole.yMin - padding, screen.yMin, screen.yMax);
            float yMax = Mathf.Clamp(hole.yMax + padding, screen.yMin, screen.yMax);
            return new[]
            {
                Rect.MinMaxRect(screen.xMin, yMax, screen.xMax, screen.yMax), // cima
                Rect.MinMaxRect(screen.xMin, screen.yMin, screen.xMax, yMin), // baixo
                Rect.MinMaxRect(screen.xMin, yMin, xMin, yMax),               // esquerda
                Rect.MinMaxRect(xMax, yMin, screen.xMax, yMax),               // direita
            };
        }

        /// <summary>
        /// Seta acima do alvo se ele está na metade de baixo da tela (aponta para baixo), senão abaixo (aponta para cima).
        /// O balão fica depois da seta, no mesmo lado, e é empurrado para dentro da tela com <paramref name="margin"/>.
        /// </summary>
        public static ArrowPlacement PlaceArrow(Rect screen, Rect target, Vector2 bubbleSize, float arrowLength,
            float gap, float margin)
        {
            var side = target.center.y < screen.center.y ? ArrowSide.Above : ArrowSide.Below;
            float x = Mathf.Clamp(target.center.x, screen.xMin + margin, screen.xMax - margin);
            Vector2 tip;
            Vector2 bubble;
            if (side == ArrowSide.Above)
            {
                tip = new Vector2(x, target.yMax + gap);
                bubble = new Vector2(x, tip.y + arrowLength + bubbleSize.y * 0.5f);
            }
            else
            {
                tip = new Vector2(x, target.yMin - gap);
                bubble = new Vector2(x, tip.y - arrowLength - bubbleSize.y * 0.5f);
            }
            return new ArrowPlacement(tip, side, ClampInside(screen, bubble, bubbleSize, margin));
        }

        /// <summary>Move o centro de uma caixa do tamanho dado até ela caber inteira na tela, com margem.</summary>
        public static Vector2 ClampInside(Rect screen, Vector2 center, Vector2 size, float margin)
        {
            float hx = size.x * 0.5f + margin, hy = size.y * 0.5f + margin;
            float x = screen.width < hx * 2f ? screen.center.x : Mathf.Clamp(center.x, screen.xMin + hx, screen.xMax - hx);
            float y = screen.height < hy * 2f ? screen.center.y : Mathf.Clamp(center.y, screen.yMin + hy, screen.yMax - hy);
            return new Vector2(x, y);
        }

        /// <summary>Menor retângulo que contém todos (alvo feito de vários elementos, ex.: os 3 recursos).</summary>
        public static Rect Union(Rect[] rects)
        {
            if (rects == null || rects.Length == 0) return Rect.zero;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (var r in rects)
            {
                xMin = Mathf.Min(xMin, r.xMin);
                yMin = Mathf.Min(yMin, r.yMin);
                xMax = Mathf.Max(xMax, r.xMax);
                yMax = Mathf.Max(yMax, r.yMax);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Quanto a seta se afasta do alvo no balanço: vai de 0 a <paramref name="amplitude"/> e volta,
        /// <paramref name="frequency"/> vezes por segundo.
        /// </summary>
        public static float Bob(float time, float amplitude, float frequency) =>
            amplitude * 0.5f * (1f - Mathf.Cos(time * frequency * 2f * Mathf.PI));

        /// <summary>Opacidade da moldura pulsando, entre <paramref name="min"/> e 1.</summary>
        public static float Pulse(float time, float frequency, float min) =>
            Mathf.Lerp(min, 1f, 0.5f * (1f + Mathf.Sin(time * frequency * 2f * Mathf.PI)));

        /// <summary>
        /// Procura a posição livre mais perto de (<paramref name="cx"/>, <paramref name="cy"/>) onde cabe uma pegada
        /// <paramref name="width"/> x <paramref name="height"/> (canto de baixo à esquerda em <paramref name="anchor"/>).
        /// Busca em anéis quadrados de distância crescente; empate fica com o primeiro do anel. Falso se não houver lugar.
        /// </summary>
        public static bool TryFindBuildSpot(GridMap grid, int width, int height, int cx, int cy, out Vector2Int anchor)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            anchor = default;
            if (width <= 0 || height <= 0) return false;
            // A célula (cx, cy) fica no centro da pegada, igual ao fantasma do modo construção
            int ox = (width - 1) / 2, oy = (height - 1) / 2;
            int maxRing = Mathf.Max(grid.Width, grid.Height);
            for (int ring = 0; ring <= maxRing; ring++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;
                        int x = cx + dx - ox, y = cy + dy - oy;
                        if (!grid.CanPlace(x, y, width, height)) continue;
                        anchor = new Vector2Int(x, y);
                        return true;
                    }
            }
            return false;
        }

        /// <summary>A câmera conta como movida se andou pelo menos <paramref name="minMove"/> unidades ou mudou o zoom em <paramref name="minZoom"/>.</summary>
        public static bool CameraMoved(Vector2 startPos, float startSize, Vector2 pos, float size, float minMove, float minZoom) =>
            (pos - startPos).sqrMagnitude >= minMove * minMove || Mathf.Abs(size - startSize) >= minZoom;
    }
}
