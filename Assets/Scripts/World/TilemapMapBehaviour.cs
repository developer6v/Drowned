using UnityEngine;
using UnityEngine.Tilemaps;

namespace Drowned.World
{
    /// <summary>
    /// Lê o Tilemap do mapa único e constrói o <see cref="GridMap"/> lógico (SCRUM-122).
    ///
    /// Como montar na cena (quem está no editor):
    /// 1. GameObject → 2D Object → Tilemap → Isometric. Isso cria "Grid" (Cell Layout = Isometric) com um Tilemap filho.
    /// 2. Renomeie o Tilemap para "Ground" e pinte o terreno com o tile de chão.
    /// 3. Crie um segundo Tilemap filho do mesmo Grid chamado "Buildable". Pinte com qualquer tile
    ///    (pode ser semi-transparente) só as células onde o jogador pode construir.
    ///    Deixe o Tilemap Renderer desse layer desligado, ou use-o como overlay de "modo construção".
    /// 4. Coloque este componente no objeto "Grid" e arraste os dois Tilemaps nos campos.
    ///
    /// O GridMap resultante tem a mesma origem de célula que o Tilemap "Ground": use
    /// <see cref="CellToGrid"/> e <see cref="GridToCell"/> para converter.
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public sealed class TilemapMapBehaviour : MonoBehaviour
    {
        [SerializeField] private Tilemap _ground;
        [SerializeField] private Tilemap _buildable;

        [Tooltip("Se verdadeiro, o layer Buildable é escondido ao iniciar o jogo e só aparece em modo construção.")]
        [SerializeField] private bool _hideBuildableLayerOnStart = true;

        public GridMap Map { get; private set; }
        public Grid Grid { get; private set; }

        /// <summary>Célula do Tilemap que corresponde a (0,0) do GridMap.</summary>
        public Vector3Int Origin { get; private set; }

        private void Awake()
        {
            Grid = GetComponent<Grid>();
            if (_ground == null)
            {
                Debug.LogError("TilemapMapBehaviour: arraste o Tilemap 'Ground' no inspetor.", this);
                enabled = false;
                return;
            }
            Build();
            if (_hideBuildableLayerOnStart) SetBuildableOverlayVisible(false);
        }

        /// <summary>Reconstrói o GridMap a partir dos tilemaps. Chame se o mapa for editado em runtime.</summary>
        public void Build()
        {
            _ground.CompressBounds();
            BoundsInt b = _ground.cellBounds;
            Origin = b.min;
            Map = new GridMap(Mathf.Max(1, b.size.x), Mathf.Max(1, b.size.y));

            Tilemap source = _buildable != null ? _buildable : _ground;
            for (int x = 0; x < Map.Width; x++)
            {
                for (int y = 0; y < Map.Height; y++)
                {
                    var cell = new Vector3Int(Origin.x + x, Origin.y + y, 0);
                    bool hasGround = _ground.HasTile(cell);
                    bool buildable = hasGround && source.HasTile(cell);
                    Map.SetBuildable(x, y, buildable);
                }
            }
        }

        public void SetBuildableOverlayVisible(bool visible)
        {
            if (_buildable == null) return;
            var renderer = _buildable.GetComponent<TilemapRenderer>();
            if (renderer != null) renderer.enabled = visible;
        }

        public Vector2Int CellToGrid(Vector3Int cell) => new(cell.x - Origin.x, cell.y - Origin.y);

        public Vector3Int GridToCell(int x, int y) => new(Origin.x + x, Origin.y + y, 0);

        /// <summary>Converte um ponto do mundo (ex.: posição do mouse) para coordenada do GridMap.</summary>
        public Vector2Int WorldToGrid(Vector3 world) => CellToGrid(Grid.WorldToCell(world));

        /// <summary>Centro, no mundo, da célula do GridMap. Use para posicionar o sprite da estrutura.</summary>
        public Vector3 GridToWorldCenter(int x, int y) => Grid.GetCellCenterWorld(GridToCell(x, y));

        /// <summary>Limites do mapa no mundo, para a câmera não sair do terreno (SCRUM-123).</summary>
        public Bounds WorldBounds
        {
            get
            {
                if (_ground == null) return new Bounds(Vector3.zero, Vector3.one);
                _ground.CompressBounds();
                return _ground.localBounds.size == Vector3.zero
                    ? new Bounds(transform.position, Vector3.one)
                    : TransformBounds(_ground.transform, _ground.localBounds);
            }
        }

        private static Bounds TransformBounds(Transform t, Bounds local)
        {
            Vector3 center = t.TransformPoint(local.center);
            Vector3 ext = local.extents;
            Vector3 axisX = t.TransformVector(ext.x, 0, 0);
            Vector3 axisY = t.TransformVector(0, ext.y, 0);
            Vector3 axisZ = t.TransformVector(0, 0, ext.z);
            ext.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            ext.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            ext.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
            return new Bounds(center, ext * 2f);
        }
    }
}
