using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Drowned.World
{
    /// <summary>
    /// Espalha corais, rochas, algas e destroços nas células não construíveis do mapa (SCRUM-183).
    /// Os sprites vêm de <c>Assets/Sprites/Decor/Resources/Decor/&lt;Nome&gt;/&lt;Nome&gt;_sheet.png</c>, gerados por
    /// <c>tools/blender/render_batch.py</c>; cada uma das 4 direções do sheet conta como uma variação.
    /// Sem sprites, não faz nada (o mapa continua funcionando).
    /// </summary>
    public sealed class DecorScatterBehaviour : MonoBehaviour
    {
        [Tooltip("Pasta dentro de Resources com os sheets de decoração.")]
        [SerializeField] private string _resourcesFolder = "Decor";
        [SerializeField, Range(0f, 1f)] private float _density = 0.45f;
        [SerializeField] private int _seed = 2026;
        [SerializeField] private string _sortingLayer = "Default";

        private static readonly Regex FrameZero = new Regex(@"_[SWNE]_000$");

        /// <summary>Espalha no mapa dado, pulando as células em que <paramref name="excluded"/> devolve true.</summary>
        public int Scatter(TilemapMapBehaviour map, Func<int, int, bool> excluded = null)
        {
            if (map == null || map.Map == null) return 0;
            var sprites = LoadSprites();
            if (sprites.Count == 0)
            {
                Debug.Log($"DecorScatterBehaviour: nenhum sprite em Resources/{_resourcesFolder}; rode tools/blender/render_batch.py.", this);
                return 0;
            }

            var placements = DecorLayout.Pick(map.Map, sprites.Count, _density, _seed, excluded);
            foreach (var p in placements)
            {
                var go = new GameObject("Decor_" + sprites[p.Variant].name);
                go.transform.SetParent(transform, false);
                go.transform.position = map.GridToWorldCenter(p.X, p.Y);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = sprites[p.Variant];
                r.flipX = p.FlipX;
                r.sortingLayerName = _sortingLayer;
                r.sortingOrder = StructureManagerBehaviour.SortingOrderFor(p.X, p.Y);
            }
            return placements.Count;
        }

        private List<Sprite> LoadSprites()
        {
            var list = new List<Sprite>();
            foreach (var s in UnityEngine.Resources.LoadAll<Sprite>(_resourcesFolder))
                if (s != null && FrameZero.IsMatch(s.name)) list.Add(s);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // ordem estável: mesma semente, mesmo mapa
            return list;
        }
    }
}
