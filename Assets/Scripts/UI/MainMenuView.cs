using Drowned.Core;
using Drowned.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>
    /// Menu principal (SCRUM-178) e opções de volume (SCRUM-180), montados por código.
    /// Coloque em um objeto vazio na cena "MainMenu". Jogar carrega a cena "Game"; Continuar aplica o save do slot 1.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        private GameObject _menu;
        private GameObject _options;
        private Text _sfxLabel;
        private Text _musicLabel;

        private void Start()
        {
            var canvas = UiKit.CreateCanvas("MainMenuCanvas");
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            var bg = UiKit.Panel(root, "Background", new Color(0.02f, 0.08f, 0.16f, 1f));
            UiKit.Stretch(bg.rectTransform);

            // Menu
            var box = UiKit.Panel(root, "Menu", UiTheme.PanelBg);
            _menu = box.gameObject;
            UiKit.Center(box.rectTransform, new Vector2(460f, 520f));
            var col = UiKit.Vertical(box.transform, "Col", 12f, new RectOffset(28, 28, 28, 28), TextAnchor.UpperCenter);
            UiKit.Stretch(col.GetComponent<RectTransform>());
            UiKit.Label(col.transform, "DROWNED", UiTheme.FontTitle, UiTheme.Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Label(col.transform, "Reconstrua a cidade submersa", UiTheme.FontSmall, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            UiKit.Size(UiKit.Button(col.transform, "Jogar", () =>
            {
                SaveSystem.PendingLoad = null;
                SceneFlow.LoadGame();
            }), height: 52f);
            var cont = UiKit.Button(col.transform, "Continuar", () =>
            {
                var data = SaveSystem.Load(1);
                if (data == null) return;
                SaveSystem.PendingLoad = data;
                SceneFlow.LoadGame();
            });
            UiKit.Size(cont, height: 52f);
            var at = SaveSystem.HasSave(1) ? SaveSystem.SavedAt(1) : null;
            cont.interactable = at.HasValue;
            UiKit.ButtonText(cont).text = at.HasValue ? $"Continuar ({at.Value:dd/MM HH:mm})" : "Continuar (sem save)";
            UiKit.Size(UiKit.Button(col.transform, "Opções", () => ShowOptions(true)), height: 52f);
            UiKit.Size(UiKit.Button(col.transform, "Sair", SceneFlow.Quit), height: 52f);

            // Opções
            var opt = UiKit.Panel(root, "Options", UiTheme.PanelBg);
            _options = opt.gameObject;
            UiKit.Center(opt.rectTransform, new Vector2(460f, 360f));
            var ocol = UiKit.Vertical(opt.transform, "Col", 12f, new RectOffset(28, 28, 28, 28), TextAnchor.UpperCenter);
            UiKit.Stretch(ocol.GetComponent<RectTransform>());
            UiKit.Label(ocol.transform, "Opções", UiTheme.FontLarge, UiTheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _sfxLabel = VolumeRow(ocol.transform, "Efeitos", () => AudioManager.SfxVolume, v => AudioManager.SfxVolume = v);
            _musicLabel = VolumeRow(ocol.transform, "Ambiente", () => AudioManager.MusicVolume, v => AudioManager.MusicVolume = v);
            UiKit.Label(ocol.transform, $"Resolução: {Screen.width}x{Screen.height} (ajuste nas configurações do sistema)", UiTheme.FontSmall, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            UiKit.Size(UiKit.Button(ocol.transform, "Voltar", () => ShowOptions(false)), height: 48f);
            ShowOptions(false);
        }

        private Text VolumeRow(Transform parent, string label, System.Func<float> get, System.Action<float> set)
        {
            var row = UiKit.Horizontal(parent, label, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
            var text = UiKit.Label(row.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleLeft);
            UiKit.Size(text, width: 200f);
            Text valueText = null;
            void Refresh() => valueText.text = $"{Mathf.RoundToInt(get() * 100f)}%";
            UiKit.Size(UiKit.Button(row.transform, "-", () => { set(Mathf.Max(0f, get() - 0.1f)); Refresh(); }), width: 44f, height: 40f);
            valueText = UiKit.Label(row.transform, "", UiTheme.FontNormal, UiTheme.Text, TextAnchor.MiddleCenter);
            UiKit.Size(valueText, width: 70f);
            UiKit.Size(UiKit.Button(row.transform, "+", () => { set(Mathf.Min(1f, get() + 0.1f)); Refresh(); }), width: 44f, height: 40f);
            text.text = label;
            Refresh();
            return valueText;
        }

        private void ShowOptions(bool show)
        {
            _menu.SetActive(!show);
            _options.SetActive(show);
        }
    }
}
