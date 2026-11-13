using System.Collections.Generic;
using System.Linq;
using Drowned.Core;
using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Habitantes andando pelo mapa (SCRUM-126/139): um mergulhador por habitante, até <see cref="_maxWalkers"/>,
    /// circulando entre as estruturas pelas células livres, contornando as construções (rota em grade, SCRUM-199). Cada um tem um tom de roupa diferente (variações da SCRUM-139) e vira para a
    /// direção em que anda usando as 4 direções do sheet. Andam no tempo do jogo: param na pausa e aceleram em 2x/3x.
    /// Sprites de <c>Resources/Characters/Inhabitant/Inhabitant_sheet</c>; sem eles, não desenha nada.
    /// Especialistas desbloqueados (SCRUM-152) aparecem nos primeiros mergulhadores com o sprite próprio
    /// (<c>Characters/Specialist_&lt;id&gt;</c>), sem tinta de roupa; sem o sprite, ficam com o visual comum.
    /// </summary>
    public sealed class InhabitantWalkers : MonoBehaviour
    {
        [SerializeField] private string _sheetPath = "Characters/Inhabitant/Inhabitant_sheet";
        [Tooltip("Caminho em Resources do sheet de cada especialista; {0} é o id (engineer, biologist...).")]
        [SerializeField] private string _specialistSheetFormat = "Characters/Specialist_{0}/Specialist_{0}_sheet";
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
        private readonly Dictionary<string, Sprite[]> _specialistFacing = new Dictionary<string, Sprite[]>();
        private int _appliedSpecialists = -1;
        private readonly List<Walker> _walkers = new List<Walker>();
        private System.Random _rng = new System.Random(11);

        private sealed class Walker
        {
            public SpriteRenderer Renderer;
            public Sprite[] Sheet;
            public Color Tint;
            public Vector3 Position;
            /// <summary>Próximos pontos a percorrer (centros de célula livres), até o destino.</summary>
            public readonly Queue<Vector3> Path = new Queue<Vector3>();
            /// <summary>Rota precisa ser refeita (estrutura nova pode ter bloqueado o caminho).</summary>
            public bool Replan;
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

            _facing = LoadFacing(_sheetPath);
            if (_facing == null)
            {
                Debug.Log($"InhabitantWalkers: sheet Resources/{_sheetPath} não encontrado; habitantes não serão desenhados.", this);
                enabled = false;
                return;
            }
            _game.Population.OnPopulationChanged += HandlePopulationChanged;
            _game.Structures.OnStructureBuilt += HandleStructureBuilt;
            Sync();
        }

        private void OnDestroy()
        {
            if (_game == null) return;
            _game.Population.OnPopulationChanged -= HandlePopulationChanged;
            _game.Structures.OnStructureBuilt -= HandleStructureBuilt;
        }

        private void HandlePopulationChanged(int _) => Sync();

        // Construção nova pode cair no caminho ou em cima de alguém: todos refazem a rota (SCRUM-199)
        private void HandleStructureBuilt(StructureInstance _)
        {
            foreach (var w in _walkers) w.Replan = true;
        }

        private Sprite[] LoadFacing(string path)
        {
            var all = UnityEngine.Resources.LoadAll<Sprite>(path);
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
            ApplyAppearance();
        }

        /// <summary>Os primeiros mergulhadores viram os especialistas desbloqueados; o resto usa o visual comum.</summary>
        private void ApplyAppearance()
        {
            var unlocked = _game.Specialists.Unlocked;
            _appliedSpecialists = unlocked.Count;
            for (int i = 0; i < _walkers.Count; i++)
            {
                var w = _walkers[i];
                int slot = InhabitantMath.SpecialistSlot(i, unlocked.Count, _walkers.Count);
                Sprite[] sheet = slot >= 0 ? SpecialistFacing(unlocked[slot].Id) : null;
                w.Sheet = sheet ?? _facing;
                w.Renderer.color = sheet != null ? Color.white : w.Tint;
            }
        }

        private Sprite[] SpecialistFacing(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!_specialistFacing.TryGetValue(id, out var sheet))
            {
                sheet = LoadFacing(string.Format(_specialistSheetFormat, id));
                _specialistFacing[id] = sheet; // guarda null também, para não procurar de novo
            }
            return sheet;
        }

        private Walker Spawn(int index)
        {
            var go = new GameObject("Inhabitant" + index);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(Scale, Scale, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _facing[InhabitantMath.FacingDownLeft];
            BlobShadow.Attach(go.transform, 0.3f, 0.4f);
            var tint = _suitTints.Length > 0 ? _suitTints[index % _suitTints.Length] : Color.white;
            r.color = tint;
            var walker = new Walker
            {
                Renderer = r,
                Sheet = _facing,
                Tint = tint,
                Position = RandomCell(out var cell) ? Jitter(_map.GridToWorldCenter(cell.x, cell.y)) : CenterOfMap(),
                WaitUntil = 0f,
                Facing = InhabitantMath.FacingDownLeft,
                Phase = (float)_rng.NextDouble(),
            };
            PlanNewTrip(walker);
            return walker;
        }

        /// <summary>Uma célula livre perto de uma estrutura qualquer (ou do centro do mapa, se não houver).</summary>
        private bool RandomCell(out (int x, int y) cell)
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
                if (InhabitantMath.IsWalkable(grid, x, y))
                {
                    cell = (x, y);
                    return true;
                }
            }
            cell = default;
            return false;
        }

        private Vector3 CenterOfMap() => _map.GridToWorldCenter(_map.Map.Width / 2, _map.Map.Height / 2);

        /// <summary>Espalha dentro da célula para não andarem todos em fila no centro.</summary>
        private Vector3 Jitter(Vector3 c) =>
            c + new Vector3(((float)_rng.NextDouble() - 0.5f) * 0.3f, ((float)_rng.NextDouble() - 0.5f) * 0.15f, 0f);

        /// <summary>Escolhe um destino e monta a rota pelas células livres. Sem rota, tenta outro destino.</summary>
        private void PlanNewTrip(Walker w)
        {
            w.Path.Clear();
            w.Replan = false;
            var here = _map.WorldToGrid(w.Position);
            for (int tries = 0; tries < 4; tries++)
            {
                if (!RandomCell(out var target)) continue;
                if (TryRoute(w, here, target)) return;
            }
        }

        /// <summary>Refaz a rota até o mesmo destino final; se não der mais, escolhe outro.</summary>
        private void Replan(Walker w)
        {
            w.Replan = false;
            if (w.Path.Count == 0)
            {
                // Parado: só precisa sair se construíram em cima dele
                var cell = _map.WorldToGrid(w.Position);
                if (!InhabitantMath.IsWalkable(_map.Map, cell.x, cell.y)) PlanNewTrip(w);
                return;
            }
            Vector3 last = w.Path.ToArray()[w.Path.Count - 1];
            var target = _map.WorldToGrid(last);
            var here = _map.WorldToGrid(w.Position);
            w.Path.Clear();
            if (!TryRoute(w, here, (target.x, target.y))) PlanNewTrip(w);
        }

        private bool TryRoute(Walker w, Vector2Int here, (int x, int y) target)
        {
            var path = InhabitantMath.FindPath(_map.Map, here.x, here.y, target.x, target.y);
            if (path == null || path.Count == 0) return false;
            for (int i = 0; i < path.Count; i++)
            {
                var c = _map.GridToWorldCenter(path[i].x, path[i].y);
                // Só o ponto final ganha deslocamento; os do meio ficam no centro da célula, longe das paredes
                w.Path.Enqueue(i == path.Count - 1 ? Jitter(c) : c);
            }
            return true;
        }

        private void Update()
        {
            if (_game == null) return;
            // Desbloqueio durante a partida ou vindo de um save (Restore não dispara evento)
            if (_game.Specialists.UnlockedCount != _appliedSpecialists) ApplyAppearance();
            float mult = _game.IsOver ? 0f : _game.Clock.Speed.Multiplier();
            float dt = Time.deltaTime * mult;
            float now = Time.time;

            foreach (var w in _walkers)
            {
                if (w.Renderer == null) continue;
                if (w.Replan) Replan(w);
                bool moving = false;
                if (dt > 0f && now >= w.WaitUntil)
                {
                    if (w.Path.Count == 0)
                    {
                        PlanNewTrip(w);
                    }
                    else
                    {
                        Vector3 next = w.Path.Peek();
                        Vector3 delta = next - w.Position;
                        float dist = delta.magnitude;
                        float step = _speed * dt;
                        if (dist <= step)
                        {
                            w.Position = next;
                            w.Path.Dequeue();
                            // Chegou ao destino final: descansa um pouco antes da próxima volta
                            if (w.Path.Count == 0)
                                w.WaitUntil = now + Mathf.Lerp(_pauseAtTarget.x, _pauseAtTarget.y, (float)_rng.NextDouble()) / Mathf.Max(1f, mult);
                        }
                        else
                        {
                            w.Position += delta / dist * step;
                            moving = true;
                        }
                        w.Facing = InhabitantMath.DirectionIndex(delta.x, delta.y, w.Facing);
                    }
                }

                float bob = moving ? InhabitantMath.StepBob(now * mult, w.Phase, 2.2f, 0.035f) : 0f;
                w.Renderer.transform.position = w.Position + new Vector3(0f, bob, 0f);
                w.Renderer.sprite = (w.Sheet ?? _facing)[w.Facing];
                var cell = _map.WorldToGrid(w.Position);
                w.Renderer.sortingOrder = StructureManagerBehaviour.WalkerSortingOrderFor(cell.x, cell.y);
            }
        }
    }
}
