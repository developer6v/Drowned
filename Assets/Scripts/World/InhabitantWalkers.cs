using System.Collections.Generic;
using System.Linq;
using Drowned.Core;
using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Habitantes andando pelo mapa (SCRUM-126/139): um mergulhador por habitante, até <see cref="_maxWalkers"/>,
    /// circulando entre as estruturas. Cada um tem um tom de roupa diferente (variações da SCRUM-139) e vira para a
    /// direção em que anda usando as 4 direções do sheet. Andam no tempo do jogo: param na pausa e aceleram em 2x/3x.
    /// Sprites de <c>Resources/Characters/Inhabitant/Inhabitant_sheet</c>; sem eles, não desenha nada.
    /// </summary>
    public sealed class InhabitantWalkers : MonoBehaviour
    {
        [SerializeField] private string _sheetPath = "Characters/Inhabitant/Inhabitant_sheet";
        [SerializeField, Range(0, 100)] private int _maxWalkers = 24;
        [SerializeField, Min(0.05f)] private float _speed = 0.55f;
        [Tooltip("Raio, em células, em volta de uma estrutura onde o habitante escolhe o próximo destino.")]
        [SerializeField, Min(1)] private int _wanderRadius = 3;
        [SerializeField] private Vector2 _pauseAtTarget = new Vector2(0.5f, 2.5f);
        [SerializeField] private Color[] _suitTints =
        {
            Color.white,
            new Color(0.75f, 0.9f, 1f),
            new Color(1f, 0.85f, 0.7f),
            new Color(0.8f, 1f, 0.8f),
            new Color(1f, 0.8f, 0.9f),
            new Color(0.85f, 0.8f, 1f),
        };

        private const float Scale = 1f;

        private GameManager _game;
        private TilemapMapBehaviour _map;
        private Sprite[] _facing; // S, W, N, E
        private readonly List<Walker> _walkers = new List<Walker>();
        private System.Random _rng = new System.Random(11);

        private sealed class Walker
        {
            public SpriteRenderer Renderer;
            public Vector3 Position;
            public Vector3 Target;
            public float WaitUntil;
            public int Facing;
            public float Phase;
        }

        public void Configure(TilemapMapBehaviour map)
        {
            _map = map;
        }

        private void Start()
        {
            var gm = GameManagerBehaviour.Instance;
            _game = gm != null ? gm.Game : null;
            if (_game == null || _map == null) { enabled = false; return; }

            _facing = LoadFacing();
            if (_facing == null)
            {
                Debug.Log($"InhabitantWalkers: sheet Resources/{_sheetPath} não encontrado; habitantes não serão desenhados.", this);
                enabled = false;
                return;
            }
            _game.Population.OnPopulationChanged += _ => Sync();
            Sync();
        }

        private Sprite[] LoadFacing()
        {
            var all = UnityEngine.Resources.LoadAll<Sprite>(_sheetPath);
            if (all == null || all.Length == 0) return null;
            var result = new Sprite[4];
            string[] dirs = { "S", "W", "N", "E" };
            for (int i = 0; i < dirs.Length; i++)
                result[i] = all.FirstOrDefault(s => s.name.EndsWith("_" + dirs[i] + "_000")) ?? all[0];
            return result;
        }

        /// <summary>Cria ou remove mergulhadores até bater com a população.</summary>
        private void Sync()
        {
            int wanted = InhabitantMath.WalkerCount(_game.Population.Population, _maxWalkers);
            while (_walkers.Count < wanted) _walkers.Add(Spawn(_walkers.Count));
            while (_walkers.Count > wanted)
            {
                var w = _walkers[_walkers.Count - 1];
                if (w.Renderer != null) Destroy(w.Renderer.gameObject);
                _walkers.RemoveAt(_walkers.Count - 1);
            }
        }

        private Walker Spawn(int index)
        {
            var go = new GameObject("Inhabitant" + index);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(Scale, Scale, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _facing[InhabitantMath.FacingDownLeft];
            r.color = _suitTints.Length > 0 ? _suitTints[index % _suitTints.Length] : Color.white;
            var start = RandomDestination();
            return new Walker
            {
                Renderer = r,
                Position = start,
                Target = RandomDestination(),
                WaitUntil = 0f,
                Facing = InhabitantMath.FacingDownLeft,
                Phase = (float)_rng.NextDouble(),
            };
        }

        /// <summary>Uma célula construível perto de uma estrutura qualquer (ou do centro do mapa, se não houver).</summary>
        private Vector3 RandomDestination()
        {
            var grid = _map.Map;
            var structures = _game.Structures.Instances.ToList();
            int cx = grid.Width / 2, cy = grid.Height / 2;
            if (structures.Count > 0)
            {
                var s = structures[_rng.Next(structures.Count)];
                cx = s.X;
                cy = s.Y;
            }
            for (int tries = 0; tries < 12; tries++)
            {
                int x = cx + _rng.Next(-_wanderRadius, _wanderRadius + 1);
                int y = cy + _rng.Next(-_wanderRadius, _wanderRadius + 1);
                if (grid.IsBuildable(x, y) && !grid.IsOccupied(x, y))
                {
                    var c = _map.GridToWorldCenter(x, y);
                    // espalha dentro da célula para não andarem em fila
                    return c + new Vector3(((float)_rng.NextDouble() - 0.5f) * 0.4f, ((float)_rng.NextDouble() - 0.5f) * 0.2f, 0f);
                }
            }
            return _map.GridToWorldCenter(cx, cy);
        }

        private void Update()
        {
            if (_game == null) return;
            float mult = _game.IsOver ? 0f : _game.Clock.Speed.Multiplier();
            float dt = Time.deltaTime * mult;
            float now = Time.time;

            foreach (var w in _walkers)
            {
                if (w.Renderer == null) continue;
                Vector3 delta = w.Target - w.Position;
                bool moving = false;
                if (dt > 0f && now >= w.WaitUntil)
                {
                    float dist = delta.magnitude;
                    float step = _speed * dt;
                    if (dist <= step)
                    {
                        w.Position = w.Target;
                        w.Target = RandomDestination();
                        w.WaitUntil = now + Mathf.Lerp(_pauseAtTarget.x, _pauseAtTarget.y, (float)_rng.NextDouble()) / Mathf.Max(1f, mult);
                    }
                    else
                    {
                        w.Position += delta / dist * step;
                        moving = true;
                    }
                    w.Facing = InhabitantMath.DirectionIndex(delta.x, delta.y, w.Facing);
                }

                float bob = moving ? InhabitantMath.StepBob(now * mult, w.Phase, 2.2f, 0.035f) : 0f;
                w.Renderer.transform.position = w.Position + new Vector3(0f, bob, 0f);
                w.Renderer.sprite = _facing[w.Facing];
                var cell = _map.WorldToGrid(w.Position);
                w.Renderer.sortingOrder = StructureManagerBehaviour.SortingOrderFor(cell.x, cell.y) + 1;
            }
        }
    }
}
