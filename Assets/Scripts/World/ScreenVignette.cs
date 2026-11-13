using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Vinheta: escurece as bordas da tela em azul-marinho, dando profundidade e puxando o olhar para o centro.
    /// Um sprite gerado por código que acompanha a câmera e se ajusta ao zoom. Fica acima do mundo e do domo,
    /// abaixo do fantasma do modo construção; a HUD (Canvas overlay) continua por cima.
    /// </summary>
    public sealed class ScreenVignette : MonoBehaviour
    {
        public const int SortingOrder = StructureManagerBehaviour.SortingBase + 8000;

        [SerializeField] private Color _color = new Color(0.0f, 0.03f, 0.08f, 0.55f);
        [Tooltip("Raio normalizado onde a vinheta começa a escurecer (0 = centro, 1 = canto).")]
        [SerializeField, Range(0f, 1f)] private float _inner = 0.45f;

        private Camera _cam;

        public void Configure(Camera cam)
        {
            _cam = cam;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
                name = "Vignette",
            };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f); // 1 no canto
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_inner, 1f, r));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.DontSave;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = _color;
            sr.sortingOrder = SortingOrder;
            Follow();
        }

        private void LateUpdate() => Follow();

        /// <summary>Acompanha a câmera e o zoom. Público para o print em batch, que renderiza sem passar pelo LateUpdate.</summary>
        public void Follow()
        {
            if (_cam == null) return;
            float h = _cam.orthographicSize * 2f;
            transform.position = new Vector3(_cam.transform.position.x, _cam.transform.position.y, 0f);
            transform.localScale = new Vector3(h * _cam.aspect * 1.02f, h * 1.02f, 1f);
        }
    }
}
