using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Sombra de contato suave (elipse escura com borda esfumada) embaixo de estruturas e mergulhadores, para eles
    /// "pisarem" no chão. Uma textura só, gerada por código e compartilhada; desenha acima do chão e das cáusticas e
    /// abaixo de tudo que fica em pé.
    /// </summary>
    public static class BlobShadow
    {
        public const int SortingOrder = 3;
        private const int TexSize = 64;

        private static Sprite _sprite;

        /// <summary>Opacidade relativa a uma distância normalizada do centro (0 no centro, 1 na borda).</summary>
        public static float Falloff(float r)
        {
            if (r >= 1f) return 0f;
            float k = 1f - r * r;
            return k * k;
        }

        /// <summary>Cria a sombra como filha de <paramref name="parent"/>, com <paramref name="width"/> unidades de largura.</summary>
        public static SpriteRenderer Attach(Transform parent, float width, float alpha = 0.45f, Vector2 offset = default)
        {
            var go = new GameObject("Shadow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            // Elipse 2:1, a mesma proporção do losango isométrico
            go.transform.localScale = new Vector3(width, width * 0.5f, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Sprite_();
            r.color = new Color(0.01f, 0.04f, 0.08f, alpha);
            r.sortingOrder = SortingOrder;
            return r;
        }

        private static Sprite Sprite_()
        {
            if (_sprite != null) return _sprite;
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
                name = "BlobShadow",
            };
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    float dx = (x + 0.5f) / TexSize * 2f - 1f, dy = (y + 0.5f) / TexSize * 2f - 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Falloff(Mathf.Sqrt(dx * dx + dy * dy))));
                }
            tex.Apply(false, true);
            // PPU = tamanho: 1 unidade de lado, escalada por localScale
            _sprite = Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), TexSize);
            _sprite.hideFlags = HideFlags.DontSave;
            return _sprite;
        }
    }
}
