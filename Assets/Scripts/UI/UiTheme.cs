using UnityEngine;

namespace Drowned.UI
{
    /// <summary>
    /// Paleta e tamanhos da HUD: flat, azul subaquático com destaques em âmbar (GDD). Centralizado aqui
    /// para a UI final (SCRUM-192) trocar em um lugar só.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color PanelBg = new Color(0.04f, 0.12f, 0.22f, 0.88f);
        public static readonly Color PanelBgSolid = new Color(0.04f, 0.10f, 0.18f, 0.98f);
        public static readonly Color Button = new Color(0.10f, 0.30f, 0.48f, 1f);
        public static readonly Color ButtonHover = new Color(0.16f, 0.42f, 0.64f, 1f);
        public static readonly Color ButtonPressed = new Color(0.06f, 0.22f, 0.36f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.18f, 0.24f, 0.30f, 0.7f);
        public static readonly Color ButtonSelected = new Color(0.95f, 0.70f, 0.20f, 1f);
        public static readonly Color Text = new Color(0.92f, 0.96f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.65f, 0.75f, 0.85f, 1f);
        public static readonly Color Positive = new Color(0.45f, 0.90f, 0.55f, 1f);
        public static readonly Color Amber = new Color(0.98f, 0.72f, 0.25f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.30f, 0.25f, 1f);
        public static readonly Color BarBg = new Color(0f, 0f, 0f, 0.45f);
        public static readonly Color BarFill = new Color(0.25f, 0.70f, 0.95f, 1f);

        public const int FontSmall = 16;
        public const int FontNormal = 20;
        public const int FontLarge = 28;
        public const int FontTitle = 44;

        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    }
}
