using System.Collections.Generic;
using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Fundo subaquático em camadas (SCRUM-169), todo gerado por código: gradiente de profundidade, feixes de luz,
    /// duas faixas de silhuetas com parallax e "neve marinha". Fica atrás dos Tilemaps (sortingOrder negativo) e
    /// anima com tempo não escalado, então continua vivo com o jogo pausado.
    /// Monte com <see cref="Configure"/> (o DemoBootstrap faz isso) ou coloque na cena e arraste a câmera.
    /// </summary>
    public sealed class UnderwaterBackground : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("Gradiente")]
        [SerializeField] private Color _topColor = new Color(0.07f, 0.26f, 0.40f);
        [SerializeField] private Color _bottomColor = new Color(0.01f, 0.04f, 0.09f);

        [Header("Feixes de luz")]
        [SerializeField] private Color _rayColor = new Color(0.55f, 0.85f, 1f, 0.10f);
        [SerializeField, Range(0f, 30f)] private float _raySwayDegrees = 6f;
        [SerializeField, Min(0f)] private float _raySwaySpeed = 0.15f;

        [Header("Silhuetas (parallax: 0 = parada no mundo, 1 = anda com a câmera)")]
        [SerializeField] private Color _farColor = new Color(0.05f, 0.15f, 0.24f);
        [SerializeField, Range(0f, 1f)] private float _farFactor = 0.85f;
        [SerializeField] private Color _nearColor = new Color(0.03f, 0.09f, 0.15f);
        [SerializeField, Range(0f, 1f)] private float _nearFactor = 0.6f;
        [Tooltip("Largura de um bloco de silhueta em unidades. Precisa ser maior que a visão no zoom máximo.")]
        [SerializeField, Min(10f)] private float _silhouetteTileWidth = 90f;
        [Tooltip("Altura das silhuetas como fração da altura da visão.")]
        [SerializeField, Range(0.05f, 1f)] private float _silhouetteHeight = 0.35f;

        [Header("Neve marinha")]
        [SerializeField, Range(0, 300)] private int _particleCount = 90;
        [SerializeField] private Color _particleColor = new Color(0.85f, 0.95f, 1f, 0.22f);
        [SerializeField, Min(0f)] private float _particleRiseSpeed = 0.25f;

        private const int OrderGradient = -1000;
        private const int OrderRays = -990;
        private const int OrderFar = -980;
        private const int OrderNear = -970;
        private const int OrderParticles = -960;

        private SpriteRenderer _gradient;
        private SpriteRenderer _rays;
        private SpriteRenderer[] _far;
        private SpriteRenderer[] _near;
        private readonly List<Particle> _particles = new List<Particle>();
        private bool _built;

        private struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public float Speed;
            public float Phase;
        }

        public void Configure(Camera cam)
        {
            _camera = cam;
            if (isActiveAndEnabled && !_built) Build();
        }

        private void Start()
        {
            if (_camera == null) _camera = Camera.main;
            if (!_built) Build();
        }

        private void Build()
        {
            if (_camera == null) return;
            _built = true;

            _gradient = NewRenderer("Gradient", MakeGradient(_topColor, _bottomColor), OrderGradient, Color.white);
            _rays = NewRenderer("LightRays", MakeRays(), OrderRays, _rayColor);
            _far = NewTiles("Far", MakeSilhouette(1337, ruins: false), OrderFar, _farColor);
            _near = NewTiles("Near", MakeSilhouette(4242, ruins: true), OrderNear, _nearColor);

            var dot = MakeDot();
            ViewRect(out var center, out var size);
            var rng = new System.Random(7);
            for (int i = 0; i < _particleCount; i++)
            {
                float depth = (float)rng.NextDouble(); // 0 = longe (pequena, lenta), 1 = perto
                var r = NewRenderer("Snow" + i, dot, OrderParticles, _particleColor * new Color(1f, 1f, 1f, 0.5f + 0.5f * depth));
                float scale = Mathf.Lerp(0.04f, 0.12f, depth);
                r.transform.localScale = new Vector3(scale, scale, 1f);
                _particles.Add(new Particle
                {
                    Renderer = r,
                    Position = center + new Vector2(((float)rng.NextDouble() - 0.5f) * size.x, ((float)rng.NextDouble() - 0.5f) * size.y),
                    Speed = _particleRiseSpeed * Mathf.Lerp(0.4f, 1.2f, depth),
                    Phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                });
            }
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (!_built || _camera == null) return;
            ViewRect(out var center, out var size);
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            // Gradiente e luz: grudados na câmera, cobrindo a visão com folga
            Cover(_gradient, center, size * 1.1f);
            Cover(_rays, center + new Vector2(0f, size.y * 0.15f), new Vector2(size.x * 1.6f, size.y * 1.6f));
            _rays.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * _raySwaySpeed) * _raySwayDegrees);
            var rc = _rayColor;
            rc.a *= 0.75f + 0.25f * Mathf.Sin(t * _raySwaySpeed * 2.3f);
            _rays.color = rc;

            // Silhuetas: faixa na parte de baixo da visão, rolando na horizontal com parallax
            PlaceTiles(_far, center, size, _farFactor, 1.0f);
            PlaceTiles(_near, center, size, _nearFactor, 0.8f);

            // Neve marinha: no mundo (anda com o mapa), sobe devagar e dá a volta nas bordas da visão
            float minX = center.x - size.x * 0.6f, maxX = center.x + size.x * 0.6f;
            float minY = center.y - size.y * 0.6f, maxY = center.y + size.y * 0.6f;
            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                p.Position.y += p.Speed * dt;
                p.Position.x += Mathf.Sin(t * 0.5f + p.Phase) * 0.05f * dt;
                p.Position.x = ParallaxMath.Wrap(p.Position.x, minX, maxX);
                p.Position.y = ParallaxMath.Wrap(p.Position.y, minY, maxY);
                p.Renderer.transform.position = new Vector3(p.Position.x, p.Position.y, 0f);
                _particles[i] = p;
            }
        }

        private void ViewRect(out Vector2 center, out Vector2 size)
        {
            float h = _camera.orthographicSize * 2f;
            size = new Vector2(h * _camera.aspect, h);
            var pos = _camera.transform.position;
            center = new Vector2(pos.x, pos.y);
        }

        private void PlaceTiles(SpriteRenderer[] tiles, Vector2 center, Vector2 size, float factor, float heightScale)
        {
            float w = _silhouetteTileWidth;
            float h = size.y * _silhouetteHeight * heightScale;
            float baseX = ParallaxMath.TileBase(center.x, factor, w);
            float y = center.y - size.y * 0.5f + h * 0.5f;
            for (int i = 0; i < tiles.Length; i++)
            {
                var r = tiles[i];
                r.transform.position = new Vector3(baseX + (i - 1) * w, y, 0f);
                var spriteSize = r.sprite.bounds.size;
                r.transform.localScale = new Vector3(w / spriteSize.x, h / spriteSize.y, 1f);
            }
        }

        private static void Cover(SpriteRenderer r, Vector2 center, Vector2 size)
        {
            r.transform.position = new Vector3(center.x, center.y, 0f);
            var spriteSize = r.sprite.bounds.size;
            r.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        }

        private SpriteRenderer NewRenderer(string name, Sprite sprite, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            r.color = color;
            return r;
        }

        private SpriteRenderer[] NewTiles(string name, Sprite sprite, int order, Color color)
        {
            var tiles = new SpriteRenderer[3];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = NewRenderer(name + i, sprite, order, color);
            return tiles;
        }

        // ------------------------------------------------------------------ texturas geradas

        private static Sprite ToSprite(Texture2D tex, string name)
        {
            tex.name = name;
            tex.hideFlags = HideFlags.DontSave;
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = name;
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        private static Sprite MakeGradient(Color top, Color bottom)
        {
            const int h = 64;
            var tex = NewTexture(2, h);
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);
                var c = Color.Lerp(bottom, top, t * t); // escurece mais rápido para baixo
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            return ToSprite(tex, "BgGradient");
        }

        /// <summary>Feixes diagonais suaves, brancos; a cor e a transparência vêm do SpriteRenderer.</summary>
        private static Sprite MakeRays()
        {
            const int size = 128;
            var tex = NewTexture(size, size);
            var bands = new[] { (0.22f, 0.07f), (0.45f, 0.04f), (0.63f, 0.09f), (0.84f, 0.05f) };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float diag = u + (1f - v) * 0.35f; // inclina os feixes
                    float a = 0f;
                    foreach (var (pos, width) in bands)
                    {
                        float d = Mathf.Abs(diag - pos - 0.15f) / width;
                        a += Mathf.Clamp01(1f - d * d);
                    }
                    a *= v * v; // mais forte em cima, some embaixo
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            return ToSprite(tex, "BgRays");
        }

        /// <summary>Silhueta de relevo (soma de senos) com colunas quebradas opcionais. Branca; cor no SpriteRenderer.</summary>
        private static Sprite MakeSilhouette(int seed, bool ruins)
        {
            const int w = 512, h = 128;
            var tex = NewTexture(w, h);
            tex.wrapMode = TextureWrapMode.Repeat;
            var rng = new System.Random(seed);
            float p1 = (float)rng.NextDouble() * 10f, p2 = (float)rng.NextDouble() * 10f, p3 = (float)rng.NextDouble() * 10f;
            var heights = new float[w];
            for (int x = 0; x < w; x++)
            {
                // Frequências inteiras sobre a largura: a borda esquerda casa com a direita (bloco repetível)
                float u = x / (float)w * Mathf.PI * 2f;
                float n = 0.45f + 0.18f * Mathf.Sin(u * 2f + p1) + 0.10f * Mathf.Sin(u * 5f + p2) + 0.05f * Mathf.Sin(u * 13f + p3);
                heights[x] = Mathf.Clamp01(n) * h;
            }
            if (ruins)
            {
                for (int k = 0; k < 6; k++)
                {
                    int cx = rng.Next(20, w - 20);
                    int cw = rng.Next(6, 12);
                    float top = heights[cx] + rng.Next(12, 40);
                    for (int x = cx - cw / 2; x <= cx + cw / 2; x++) heights[x] = Mathf.Max(heights[x], Mathf.Min(top, h - 2));
                }
            }
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float a = Mathf.Clamp01(heights[x] - y + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            return ToSprite(tex, ruins ? "BgNear" : "BgFar");
        }

        private static Sprite MakeDot()
        {
            const int size = 16;
            var tex = NewTexture(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
                }
            return ToSprite(tex, "BgDot");
        }
    }
}
