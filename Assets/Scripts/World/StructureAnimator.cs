using System.Collections.Generic;
using Drowned.Core;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Animações de uma estrutura no mapa, adicionado pelo <see cref="StructureManagerBehaviour"/>:
    /// montagem ao construir e ao subir de nível (SCRUM-166) e idle (SCRUM-153): respiração em todas, brilho no
    /// gerador, bolhas na planta de oxigênio e na fazenda. Só mexe na escala e na cor do próprio SpriteRenderer,
    /// então a ordem de desenho e a seleção não mudam.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StructureAnimator : MonoBehaviour
    {
        [Header("Montagem")]
        [SerializeField, Min(0.05f)] private float _buildDuration = 0.5f;
        [SerializeField, Min(0.05f)] private float _upgradeDuration = 0.3f;
        [SerializeField, Range(0f, 4f)] private float _overshoot = 1.7f;

        [Header("Idle")]
        [SerializeField, Range(0f, 0.1f)] private float _breatheAmplitude = 0.012f;
        [SerializeField, Min(0f)] private float _breatheSpeed = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _glowStrength = 0.25f;
        [SerializeField, Min(0f)] private float _glowSpeed = 0.6f;
        [SerializeField] private Vector2 _bubbleInterval = new Vector2(1.5f, 3.5f);
        [SerializeField] private Color _bubbleColor = new Color(0.8f, 0.95f, 1f, 0.7f);

        private SpriteRenderer _renderer;
        private StructureCategory _category;
        private float _phase;
        private float _popStart = -1f;
        private float _popDuration;
        private float _nextBubble;
        private System.Random _rng;
        private readonly List<(Transform t, SpriteRenderer r, float born, float life, float drift)> _bubbles =
            new List<(Transform, SpriteRenderer, float, float, float)>();

        private static Sprite _bubbleSprite;
        private static Transform _fxRoot;

        /// <summary>Objeto que guarda as bolhas, fora de "Structures" (que tem um filho por estrutura).</summary>
        private static Transform FxRoot
        {
            get
            {
                if (_fxRoot == null) _fxRoot = new GameObject("StructureFx").transform;
                return _fxRoot;
            }
        }

        public void Init(StructureCategory category, int structureId, bool playBuild)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _category = category;
            _phase = StructureAnimMath.PhaseFor(structureId);
            _rng = new System.Random(structureId);
            _nextBubble = Time.unscaledTime + RandomInterval();
            if (playBuild) Play(_buildDuration, bubbles: 6);
            else transform.localScale = Vector3.one;
        }

        /// <summary>Animação curta de subida de nível.</summary>
        public void PlayUpgrade() => Play(_upgradeDuration, bubbles: 3);

        private void Play(float duration, int bubbles)
        {
            _popStart = Time.unscaledTime;
            _popDuration = duration;
            transform.localScale = new Vector3(1f, 0f, 1f);
            for (int i = 0; i < bubbles; i++) SpawnBubble(spread: 0.45f);
        }

        private void Update()
        {
            if (_renderer == null) return;
            float now = Time.unscaledTime;
            var game = GameManagerBehaviour.Instance != null ? GameManagerBehaviour.Instance.Game : null;
            bool over = game != null && game.IsOver;
            bool paused = game != null && game.Clock.IsPaused;

            // Escala: montagem tem prioridade; depois, respiração
            float sy;
            if (_popStart >= 0f)
            {
                float t = (now - _popStart) / _popDuration;
                sy = StructureAnimMath.BuildPop(t, _overshoot);
                if (t >= 1f) _popStart = -1f;
            }
            else sy = over ? 1f : StructureAnimMath.Breathe(now, _phase, _breatheAmplitude, _breatheSpeed);
            float sx = 1f + (1f - sy) * 0.15f; // achata um pouco quando estica, para não parecer só esticado
            transform.localScale = new Vector3(sx, sy, 1f);

            // Brilho do gerador
            if (_category == StructureCategory.Energy && !over)
            {
                float g = StructureAnimMath.Pulse01(now, _phase, _glowSpeed) * _glowStrength;
                _renderer.color = new Color(1f + g, 1f + g * 0.8f, 1f + g * 0.3f, 1f);
            }

            // Bolhas de idle
            bool bubbly = _category == StructureCategory.Oxygen || _category == StructureCategory.Food;
            if (bubbly && !over && !paused && now >= _nextBubble)
            {
                SpawnBubble(spread: 0.2f);
                _nextBubble = now + RandomInterval();
            }
            UpdateBubbles(now);
        }

        private float RandomInterval()
        {
            double r = _rng != null ? _rng.NextDouble() : 0.5;
            return Mathf.Lerp(_bubbleInterval.x, _bubbleInterval.y, (float)r);
        }

        private void SpawnBubble(float spread)
        {
            if (_bubbleSprite == null) _bubbleSprite = MakeBubbleSprite();
            var go = new GameObject("Bubble");
            go.transform.SetParent(FxRoot, false); // fora da estrutura: não herda a escala da animação
            var top = _renderer.bounds.center + new Vector3(0f, _renderer.bounds.extents.y * 0.6f, 0f);
            float dx = ((float)(_rng?.NextDouble() ?? 0.5) - 0.5f) * spread * 2f;
            go.transform.position = top + new Vector3(dx, 0f, 0f);
            float size = Mathf.Lerp(0.06f, 0.14f, (float)(_rng?.NextDouble() ?? 0.5));
            go.transform.localScale = new Vector3(size, size, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _bubbleSprite;
            r.color = _bubbleColor;
            r.sortingLayerID = _renderer.sortingLayerID;
            r.sortingOrder = _renderer.sortingOrder + 1;
            _bubbles.Add((go.transform, r, Time.unscaledTime, Mathf.Lerp(1.2f, 2.2f, (float)(_rng?.NextDouble() ?? 0.5)),
                (float)(_rng?.NextDouble() ?? 0.5) * Mathf.PI * 2f));
        }

        private void UpdateBubbles(float now)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                var b = _bubbles[i];
                float age = (now - b.born) / b.life;
                if (b.t == null || age >= 1f)
                {
                    if (b.t != null) Destroy(b.t.gameObject);
                    _bubbles.RemoveAt(i);
                    continue;
                }
                b.t.position += new Vector3(Mathf.Sin(now * 3f + b.drift) * 0.08f * dt, 0.45f * dt, 0f);
                var c = _bubbleColor;
                c.a *= 1f - age * age;
                b.r.color = c;
            }
        }

        private void OnDestroy()
        {
            foreach (var b in _bubbles)
                if (b.t != null) Destroy(b.t.gameObject);
            _bubbles.Clear();
        }

        private static Sprite MakeBubbleSprite()
        {
            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            float c = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) * 6f);           // contorno da bolha
                    float fill = Mathf.Clamp01(1f - d) * 0.25f;                           // miolo transparente
                    float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(c * 0.65f, c * 1.35f)) / 3f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ring + fill + shine)));
                }
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s.hideFlags = HideFlags.DontSave;
            return s;
        }
    }
}
