using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Cáusticas animadas no chão: duas camadas da mesma textura procedural (<see cref="CausticsMath"/>), em tamanhos
    /// diferentes, deslizando em direções opostas. Onde as linhas se cruzam a luz soma e parece "dançar".
    /// A textura é achatada 2:1, como o chão isométrico, e recortada no losango do mapa por uma SpriteMask.
    /// Usa tempo não escalado (continua com o jogo pausado, como o fundo).
    /// </summary>
    public sealed class CausticsOverlay : MonoBehaviour
    {
        public const int SortingOrder = 2; // acima do chão (0) e da área construível (1), abaixo das sombras (3)

        [SerializeField] private Color _color = new Color(0.70f, 0.95f, 1f, 0.07f);
        [SerializeField, Min(1f)] private float _tileWidthA = 7f;
        [SerializeField, Min(1f)] private float _tileWidthB = 11f;
        [SerializeField] private Vector2 _driftA = new Vector2(0.10f, 0.04f);
        [SerializeField] private Vector2 _driftB = new Vector2(-0.07f, 0.05f);
        [SerializeField, Range(1f, 10f)] private float _sharpness = 4f;

        private const int TexW = 256, TexH = 128;

        private Transform _layerA, _layerB;

        /// <summary>Monta as camadas sobre o mapa.</summary>
        public void Build(TilemapMapBehaviour map)
        {
            if (map == null) return;
            Bounds b = map.WorldBounds;
            transform.position = new Vector3(b.center.x, b.center.y, 0f);

            var mask = new GameObject("DiamondMask").AddComponent<SpriteMask>();
            mask.transform.SetParent(transform, false);
            mask.sprite = DiamondSprite();
            mask.transform.localScale = new Vector3(b.size.x, b.size.y, 1f);

            var caustic = CausticSprite();
            _layerA = NewLayer("CausticsA", caustic, _tileWidthA, b);
            _layerB = NewLayer("CausticsB", caustic, _tileWidthB, b);
        }

        private Transform NewLayer(string name, Sprite sprite, float tileWidth, Bounds b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.drawMode = SpriteDrawMode.Tiled;
            r.tileMode = SpriteTileMode.Continuous;
            float scale = tileWidth / (TexW / sprite.pixelsPerUnit);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            // Cobre o mapa com folga de um bloco para deslizar sem mostrar a borda
            r.size = new Vector2((b.size.x + tileWidth * 2f) / scale, (b.size.y + tileWidth) / scale);
            r.color = _color;
            r.sortingOrder = SortingOrder;
            r.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return go.transform;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            Slide(_layerA, _driftA * t, _tileWidthA);
            Slide(_layerB, _driftB * t, _tileWidthB);
        }

        private static void Slide(Transform layer, Vector2 offset, float tileWidth)
        {
            if (layer == null) return;
            float w = tileWidth, h = tileWidth * TexH / TexW;
            layer.localPosition = new Vector3(Mathf.Repeat(offset.x, w) - w * 0.5f, Mathf.Repeat(offset.y, h) - h * 0.5f, 0f);
        }

        private Sprite CausticSprite()
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
                name = "Caustics",
            };
            for (int y = 0; y < TexH; y++)
                for (int x = 0; x < TexW; x++)
                {
                    float a = CausticsMath.Intensity((x + 0.5f) / TexW, (y + 0.5f) / TexH, _sharpness);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, TexW, TexH), new Vector2(0.5f, 0.5f), TexW, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        /// <summary>Losango de 1 × 1 unidade (cheio dentro), para recortar no formato do mapa.</summary>
        private static Sprite DiamondSprite()
        {
            const int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
                name = "DiamondMask",
            };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / w * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, dx + dy <= 1f ? 1f : 0f));
                }
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.DontSave;
            return s;
        }
    }
}
