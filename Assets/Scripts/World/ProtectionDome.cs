using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Domo de proteção sobre a cidade (SCRUM-168): vidro translúcido gerado por código, cobrindo a área construível.
    /// Desenha por cima das estruturas (o vidro está "na frente" delas) com opacidade baixa, borda mais forte,
    /// reflexo no alto e um tremeluzir lento. Não bloqueia cliques (é só um SpriteRenderer).
    /// </summary>
    public sealed class ProtectionDome : MonoBehaviour
    {
        [SerializeField] private Color _glass = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField, Range(0f, 0.3f)] private float _fill = 0.015f;
        [SerializeField, Range(0f, 1f)] private float _rim = 0.22f;
        [SerializeField, Range(0.01f, 0.5f)] private float _rimWidth = 0.12f;
        [Tooltip("Altura da cúpula como fração da meia largura.")]
        [SerializeField, Range(0.2f, 1.5f)] private float _height = 0.55f;
        [Tooltip("Folga em volta da área construível, em células.")]
        [SerializeField, Min(0f)] private float _padding = 0.6f;
        [SerializeField, Range(0f, 0.5f)] private float _shimmer = 0.15f;
        [SerializeField, Min(0f)] private float _shimmerSpeed = 0.25f;

        public const int SortingOrder = StructureManagerBehaviour.SortingBase + 5000;

        private SpriteRenderer _renderer;

        /// <summary>Monta o domo sobre o retângulo de células [x0, x1] × [y0, y1] do mapa.</summary>
        public void Build(TilemapMapBehaviour map, int x0, int y0, int x1, int y1)
        {
            if (map == null) return;
            // Cantos do losango da área (centros das células extremas) em mundo
            Vector3 left = map.GridToWorldCenter(x0, y1);
            Vector3 right = map.GridToWorldCenter(x1, y0);
            Vector3 bottom = map.GridToWorldCenter(x0, y0);
            Vector3 top = map.GridToWorldCenter(x1, y1);
            Vector3 center = (left + right + bottom + top) * 0.25f;
            float halfW = (right.x - left.x) * 0.5f + _padding;
            float halfBaseH = (top.y - bottom.y) * 0.5f + _padding * 0.5f;

            float baseRy = halfBaseH / halfW;     // semi-eixo vertical da base, em meias larguras
            float height = _height;               // cúpula acima da linha da base

            const int texW = 512;
            int texH = Mathf.CeilToInt(texW * 0.5f * (baseRy + height));
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
                name = "ProtectionDome",
            };
            float pivotY = baseRy / (baseRy + height); // linha da base, como fração da altura da textura
            for (int py = 0; py < texH; py++)
                for (int px = 0; px < texW; px++)
                {
                    float x = (px + 0.5f) / texW * 2f - 1f;
                    float y = ((py + 0.5f) / texH - pivotY) * (baseRy + height);
                    float a = DomeShape.Alpha(x, y, baseRy, height, _fill, _rim, _rimWidth);
                    // Borda esfumada em ~2 pixels: sem isso o contorno sai serrilhado
                    a *= Mathf.Clamp01((1f - DomeShape.Edge(x, y, baseRy, height)) * texW * 0.25f);
                    // reflexo: arco claro no alto à esquerda
                    if (y > 0f)
                    {
                        float hx = x + 0.35f, hy = y / height - 0.55f;
                        float h = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx * 4f + hy * hy * 9f));
                        a = Mathf.Min(1f, a + h * h * 0.12f * (DomeShape.Edge(x, y, baseRy, height) <= 1f ? 1f : 0f));
                    }
                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(false, true);

            float ppu = texW / (halfW * 2f);
            var sprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, pivotY), ppu, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;

            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.color = _glass;
            _renderer.sortingOrder = SortingOrder;
            transform.position = new Vector3(center.x, center.y, 0f);
        }

        private void Update()
        {
            if (_renderer == null) return;
            float s = 1f - _shimmer * 0.5f + _shimmer * 0.5f * Mathf.Sin(Time.unscaledTime * _shimmerSpeed * Mathf.PI * 2f);
            var c = _glass;
            c.a = s;
            _renderer.color = c;
        }
    }
}
