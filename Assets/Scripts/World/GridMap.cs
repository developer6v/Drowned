using System;
using System.Collections.Generic;

namespace Drowned.World
{
    /// <summary>
    /// Mapa lógico do jogo (SCRUM-122): uma grade retangular de células onde cada célula pode ou não
    /// ser construível e pode estar ocupada por uma estrutura. Lógica pura e testável; a ponte com o
    /// Tilemap do Unity é feita por <see cref="TilemapMapBehaviour"/>.
    ///
    /// Coordenadas: (x, y) com origem no canto inferior esquerdo, x para a direita, y para cima,
    /// iguais às coordenadas de célula do Tilemap.
    /// </summary>
    public sealed class GridMap
    {
        public int Width { get; }
        public int Height { get; }

        private readonly bool[,] _buildable;
        private readonly int[,] _occupant; // 0 = livre; > 0 = id da estrutura
        private readonly Dictionary<int, Footprint> _placed = new();
        private int _nextId = 1;

        public readonly struct Footprint
        {
            public readonly int X, Y, Width, Height;
            public Footprint(int x, int y, int width, int height)
            {
                X = x; Y = y; Width = width; Height = height;
            }
        }

        public GridMap(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Dimensões devem ser positivas.");
            Width = width;
            Height = height;
            _buildable = new bool[width, height];
            _occupant = new int[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsBuildable(int x, int y) => InBounds(x, y) && _buildable[x, y];

        public bool IsOccupied(int x, int y) => InBounds(x, y) && _occupant[x, y] != 0;

        /// <summary>Id da estrutura na célula, ou 0 se livre/fora do mapa.</summary>
        public int OccupantAt(int x, int y) => InBounds(x, y) ? _occupant[x, y] : 0;

        public void SetBuildable(int x, int y, bool value)
        {
            if (!InBounds(x, y)) throw new ArgumentOutOfRangeException($"({x},{y}) fora do mapa.");
            _buildable[x, y] = value;
        }

        public void SetBuildableRect(int x, int y, int width, int height, bool value)
        {
            for (int i = x; i < x + width; i++)
                for (int j = y; j < y + height; j++)
                    if (InBounds(i, j)) _buildable[i, j] = value;
        }

        public int BuildableCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if (_buildable[i, j]) n++;
                return n;
            }
        }

        /// <summary>
        /// Verifica se uma estrutura de tamanho width×height cabe com o canto inferior esquerdo em (x, y):
        /// todas as células dentro do mapa, construíveis e livres.
        /// </summary>
        public bool CanPlace(int x, int y, int width = 1, int height = 1)
        {
            if (width <= 0 || height <= 0) return false;
            for (int i = x; i < x + width; i++)
                for (int j = y; j < y + height; j++)
                    if (!IsBuildable(i, j) || IsOccupied(i, j)) return false;
            return true;
        }

        /// <summary>Lista as células (dentro da pegada) que impedem a colocação, para pintar o preview de vermelho.</summary>
        public List<(int x, int y)> BlockedCells(int x, int y, int width = 1, int height = 1)
        {
            var blocked = new List<(int, int)>();
            for (int i = x; i < x + width; i++)
                for (int j = y; j < y + height; j++)
                    if (!IsBuildable(i, j) || IsOccupied(i, j)) blocked.Add((i, j));
            return blocked;
        }

        /// <summary>Ocupa as células e devolve o id da estrutura colocada. Lança se não couber.</summary>
        public int Place(int x, int y, int width = 1, int height = 1)
        {
            if (!CanPlace(x, y, width, height))
                throw new InvalidOperationException($"Não é possível construir em ({x},{y}) {width}x{height}.");
            int id = _nextId++;
            for (int i = x; i < x + width; i++)
                for (int j = y; j < y + height; j++)
                    _occupant[i, j] = id;
            _placed[id] = new Footprint(x, y, width, height);
            return id;
        }

        /// <summary>Libera as células da estrutura. Retorna false se o id não existe.</summary>
        public bool Remove(int id)
        {
            if (!_placed.TryGetValue(id, out var fp)) return false;
            for (int i = fp.X; i < fp.X + fp.Width; i++)
                for (int j = fp.Y; j < fp.Y + fp.Height; j++)
                    _occupant[i, j] = 0;
            _placed.Remove(id);
            return true;
        }

        public bool TryGetFootprint(int id, out Footprint footprint) => _placed.TryGetValue(id, out footprint);

        public int PlacedCount => _placed.Count;

        public IEnumerable<KeyValuePair<int, Footprint>> Placed => _placed;
    }
}
