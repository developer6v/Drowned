using UnityEngine;
using UnityEngine.Tilemaps;

namespace Drowned.Core
{
    /// <summary>
    /// Sprites gerados em memória para a demo rodar antes da arte do Blender existir:
    /// losango de chão isométrico, caixinha colorida por categoria, ícones redondos.
    /// Tudo é substituído pelos sprites renderizados (SCRUM-155/185) sem mudar código.
    /// </summary>
    public static class DemoArt
    {
        public const int PixelsPerUnit = 64;

        /// <summary>Losango 64x32 (uma célula isométrica de 1 x 0,5 unidades).</summary>
        public static Sprite Diamond(Color fill, Color outline, int w = 64, int h = 32)
        {
            var tex = NewTexture(w, h);
            var clear = new Color(0, 0, 0, 0);
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(x - cx) / (w * 0.5f) + Mathf.Abs(y - cy) / (h * 0.5f);
                    tex.SetPixel(x, y, d > 1f ? clear : d > 0.9f ? outline : fill);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        /// <summary>Tile de Tilemap com o losango.</summary>
        public static Tile Tile(Color fill, Color outline)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Diamond(fill, outline);
            tile.color = Color.white;
            return tile;
        }

        /// <summary>
        /// "Prédio" isométrico simples: base em losango e um bloco com duas faces. Pivô na base, para
        /// o sprite ficar em pé sobre o centro da célula. <paramref name="cells"/> é a largura da pegada.
        /// </summary>
        public static Sprite Building(Color color, int cells = 1, int height = 40)
        {
            int w = 64 * cells;
            int baseH = 32 * cells;
            int h = baseH + height;
            var tex = NewTexture(w, h);
            var clear = new Color(0, 0, 0, 0);
            Color left = color * 0.75f; left.a = 1f;
            Color right = color * 0.55f; right.a = 1f;
            Color top = color;
            float cx = (w - 1) * 0.5f;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // Losango deslocado por 'height': topo
                    float dyTop = y - height;
                    float dTop = Mathf.Abs(x - cx) / (w * 0.5f) + Mathf.Abs(dyTop - (baseH - 1) * 0.5f) / (baseH * 0.5f);
                    if (dyTop >= 0 && dTop <= 1f) { tex.SetPixel(x, y, top); continue; }
                    // Paredes: entre a base e o topo, dentro da metade inferior do losango
                    float dBase = Mathf.Abs(x - cx) / (w * 0.5f) + Mathf.Abs(y - (baseH - 1) * 0.5f) / (baseH * 0.5f);
                    bool inLowerHalf = y <= (baseH - 1) * 0.5f + height;
                    float lowerEdge = (baseH - 1) * 0.5f - (1f - Mathf.Abs(x - cx) / (w * 0.5f)) * (baseH * 0.5f);
                    if (y >= lowerEdge && inLowerHalf && Mathf.Abs(x - cx) / (w * 0.5f) <= 1f)
                    {
                        tex.SetPixel(x, y, x < cx ? left : right);
                        continue;
                    }
                    tex.SetPixel(x, y, clear);
                }
            tex.Apply();
            // Pivô no centro do losango da base
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, (baseH * 0.5f) / h), PixelsPerUnit);
        }

        /// <summary>Ícone redondo para a HUD.</summary>
        public static Sprite Dot(Color color, int size = 64)
        {
            var tex = NewTexture(size, size);
            var clear = new Color(0, 0, 0, 0);
            float r = size * 0.45f, c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    tex.SetPixel(x, y, d > r ? clear : d > r - 4f ? Color.Lerp(color, Color.black, 0.4f) : color);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            return tex;
        }

        // Paleta da demo
        public static readonly Color Ground = new Color(0.16f, 0.36f, 0.48f);
        public static readonly Color GroundOutline = new Color(0.10f, 0.26f, 0.36f);
        public static readonly Color Buildable = new Color(0.45f, 0.85f, 0.65f, 0.35f);
        public static readonly Color Housing = new Color(0.90f, 0.75f, 0.45f);
        public static readonly Color Food = new Color(0.40f, 0.80f, 0.45f);
        public static readonly Color Energy = new Color(0.98f, 0.80f, 0.25f);
        public static readonly Color Oxygen = new Color(0.45f, 0.80f, 0.98f);
        public static readonly Color Research = new Color(0.75f, 0.55f, 0.95f);
        public static readonly Color Monument = new Color(0.85f, 0.85f, 0.90f);
    }
}
