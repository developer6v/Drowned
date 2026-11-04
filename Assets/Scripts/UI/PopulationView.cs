using Drowned.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>Contador de população, nível da cidade e próximo especialista (SCRUM-142). Atualiza por eventos.</summary>
    public sealed class PopulationView : MonoBehaviour
    {
        private GameManager _game;
        private Text _population;
        private Text _level;
        private Text _next;

        public void Build(Transform canvas, GameManager game)
        {
            _game = game;
            var panel = UiKit.Panel(canvas, "Population", UiTheme.PanelBg);
            UiKit.Anchor(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -64f), new Vector2(360f, 86f));
            var col = UiKit.Vertical(panel.transform, "Col", 2f, new RectOffset(16, 16, 6, 6));
            UiKit.Stretch(col.GetComponent<RectTransform>());
            _population = UiKit.Label(col.transform, "", UiTheme.FontLarge, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            _level = UiKit.Label(col.transform, "", UiTheme.FontSmall, UiTheme.TextMuted);
            _next = UiKit.Label(col.transform, "", UiTheme.FontSmall, UiTheme.TextMuted);

            game.Population.OnPopulationChanged += _ => Refresh();
            game.Population.OnHousingCapacityChanged += _ => Refresh();
            game.City.OnLevelChanged += _ => Refresh();
            game.Specialists.OnSpecialistUnlocked += _ => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            int pop = _game.Population.Population;
            int cap = _game.Population.HousingCapacity;
            _population.text = $"População {pop} / {cap}";
            _population.color = pop >= cap ? UiTheme.Amber : UiTheme.Text;
            _level.text = $"Cidade nível {_game.City.Level}";
            var next = _game.Specialists.NextToUnlock;
            _next.text = next == null
                ? "Todos os especialistas desbloqueados"
                : $"Faltam {_game.Specialists.PopulationToNext} habitantes para {next.DisplayName}";
        }
    }
}
