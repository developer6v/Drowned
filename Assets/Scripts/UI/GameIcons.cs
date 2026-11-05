using System.Collections.Generic;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.UI
{
    /// <summary>
    /// Ícones da HUD (SCRUM-184), carregados por id de <c>Assets/Sprites/Icons/Resources/Icons/&lt;Categoria&gt;/&lt;id&gt;.png</c>.
    /// Os PNGs são gerados por <c>tools/icons/make_game_icons.py</c> com os mesmos ids da planilha
    /// (<c>docs/balanceamento</c>). Ícone que não existe devolve null e a UI segue sem ele.
    /// </summary>
    public static class GameIcons
    {
        private const string Root = "Icons/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Resource(ResourceType type) => Load("ResourceTypes/" + type);

        public static Sprite Structure(string id) => string.IsNullOrEmpty(id) ? null : Load("Structures/" + id);

        public static Sprite Specialist(string id) => string.IsNullOrEmpty(id) ? null : Load("Specialists/" + id);

        public static Sprite Technology(string id) => string.IsNullOrEmpty(id) ? null : Load("Technologies/" + id);

        public static Sprite Alert => Load("Ui/alert");
        public static Sprite Monument => Load("Ui/monument");
        public static Sprite Population => Load("Ui/population");

        /// <summary>Os 3 ícones de recurso na ordem energia, oxigênio, alimento. Onde faltar, usa o de <paramref name="fallback"/>.</summary>
        public static Sprite[] ResourceIcons(Sprite[] fallback = null)
        {
            var types = new[] { ResourceType.Energy, ResourceType.Oxygen, ResourceType.Food };
            var icons = new Sprite[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                icons[i] = Resource(types[i]);
                if (icons[i] == null && fallback != null && i < fallback.Length) icons[i] = fallback[i];
            }
            return icons;
        }

        private static Sprite Load(string path)
        {
            if (Cache.TryGetValue(path, out var cached)) return cached;
            var sprite = UnityEngine.Resources.Load<Sprite>(Root + path);
            Cache[path] = sprite; // guarda null também, para não procurar de novo
            return sprite;
        }
    }
}
