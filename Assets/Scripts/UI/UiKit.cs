using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Drowned.UI
{
    /// <summary>
    /// Fábrica de elementos uGUI por código, para a HUD existir sem montar Canvas à mão (SCRUM-140).
    /// Usa a fonte padrão do Unity e <c>Text</c> legado; a UI final (SCRUM-192) pode trocar por TextMeshPro.
    /// </summary>
    public static class UiKit
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>Canvas de tela inteira com escala por resolução de referência, raycaster e EventSystem.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTheme.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

        /// <summary>Retângulo vazio (sem imagem) para agrupar elementos.</summary>
        public static RectTransform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return Rect(go);
        }

        /// <summary>Painel com fundo, cantos arredondados e borda fina (SCRUM-192). Para overlays de tela cheia use <see cref="Flat"/>.</summary>
        public static Image Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            Round(img);
            AddBorder(img);
            return img;
        }

        /// <summary>Tira os cantos (e a borda, se pedido) de um painel: para overlays de tela cheia e faixas finas.</summary>
        public static Image Flat(Image img, bool keepBorder = false)
        {
            img.sprite = null;
            img.type = Image.Type.Simple;
            var border = img.transform.Find(BorderName);
            if (border != null)
            {
                if (keepBorder) border.GetComponent<Image>().sprite = RingSprite(SquareRingRadius);
                else Object.DestroyImmediate(border.gameObject);
            }
            return img;
        }

        private const string BorderName = "Border";
        private const int SquareRingRadius = 2;

        private static void Round(Image img)
        {
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
        }

        private static void AddBorder(Graphic g)
        {
            var go = new GameObject(BorderName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(g.transform, false);
            go.transform.SetAsFirstSibling();
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var img = go.GetComponent<Image>();
            img.sprite = RingSprite(UiTheme.CornerRadius);
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            img.color = UiTheme.PanelBorder;
            img.raycastTarget = false;
            Stretch(img.rectTransform);
        }

        private static readonly System.Collections.Generic.Dictionary<int, Sprite> _rings =
            new System.Collections.Generic.Dictionary<int, Sprite>();

        /// <summary>Anel (só a borda) de cantos arredondados com espessura <see cref="UiTheme.BorderWidth"/>, 9-slice.</summary>
        public static Sprite RingSprite(int radius)
        {
            if (_rings.TryGetValue(radius, out var cached) && cached != null) return cached;
            var sprite = MakeRoundedSprite("UiRing" + radius, Mathf.Max(1, radius), UiTheme.BorderWidth);
            _rings[radius] = sprite;
            return sprite;
        }

        /// <summary>
        /// Gera um retângulo branco de cantos arredondados (9-slice). <paramref name="ringWidth"/> &lt;= 0 = cheio;
        /// &gt; 0 = só um anel com essa espessura. Bordas suavizadas em 1 px.
        /// </summary>
        private static Sprite MakeRoundedSprite(string name, int r, float ringWidth)
        {
            int size = r * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = name,
                hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Distância até o centro do canto mais próximo; nas bordas retas vira a distância até a linha interna.
                    float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                    float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (ringWidth > 0f) a -= Mathf.Clamp01(r - ringWidth - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static Sprite _rounded;

        /// <summary>
        /// Retângulo branco de cantos arredondados, 9-slice, gerado por código (sem asset). O raio vem de
        /// <see cref="UiTheme.CornerRadius"/>; a cor vem do Image.
        /// </summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (_rounded == null) _rounded = MakeRoundedSprite("UiRounded", Mathf.Max(1, UiTheme.CornerRadius), 0f);
                return _rounded;
            }
        }

        public static Text Label(Transform parent, string text, int size = UiTheme.FontNormal, Color? color = null,
            TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color ?? UiTheme.Text;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = UiTheme.TextShadow;
            shadow.effectDistance = UiTheme.TextShadowOffset;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityEngine.Events.UnityAction onClick, int fontSize = UiTheme.FontNormal)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            Round(img);
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = UiTheme.Button;
            colors.highlightedColor = UiTheme.ButtonHover;
            colors.pressedColor = UiTheme.ButtonPressed;
            colors.selectedColor = UiTheme.Button;
            colors.disabledColor = UiTheme.ButtonDisabled;
            colors.colorMultiplier = 1f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var text = Label(go.transform, label, fontSize, UiTheme.Text, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f, 4f);
            return btn;
        }

        /// <summary>Pinta um botão como "selecionado" (âmbar) ou normal.</summary>
        public static void SetSelected(Button btn, bool selected)
        {
            var c = btn.colors;
            c.normalColor = selected ? UiTheme.ButtonSelected : UiTheme.Button;
            c.selectedColor = c.normalColor;
            c.highlightedColor = selected ? UiTheme.ButtonSelected : UiTheme.ButtonHover;
            btn.colors = c;
            var label = ButtonText(btn);
            if (label != null)
            {
                label.color = selected ? UiTheme.TextOnAmber : UiTheme.Text;
                var shadow = label.GetComponent<Shadow>();
                if (shadow != null) shadow.enabled = !selected;
            }
        }

        public static Text ButtonText(Button btn) => btn.GetComponentInChildren<Text>();

        /// <summary>
        /// Põe um ícone à esquerda dentro do botão e empurra o texto para a direita (SCRUM-184).
        /// Sem ícone (null) não faz nada, então dá para chamar sempre.
        /// </summary>
        public static Image AddButtonIcon(Button btn, Sprite icon, float size)
        {
            if (btn == null || icon == null) return null;
            var img = Icon(btn.transform, icon, size);
            img.GetComponent<LayoutElement>().ignoreLayout = true;
            Anchor(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(size, size));
            var label = ButtonText(btn);
            if (label != null)
            {
                label.rectTransform.offsetMin = new Vector2(size + 14f, label.rectTransform.offsetMin.y);
                label.alignment = TextAnchor.MiddleLeft;
            }
            return img;
        }

        /// <summary>Barra de progresso: fundo + preenchimento. Devolve o Image de preenchimento (use fillAmount).</summary>
        public static Image ProgressBar(Transform parent, float height, Color? fill = null)
        {
            var bg = Flat(Panel(parent, "Bar", UiTheme.BarBg));
            var le = bg.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            bg.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bg.transform, false);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = fill ?? UiTheme.BarFill;
            fillImg.raycastTarget = false;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            // Sprite branco padrão para o Filled funcionar
            fillImg.sprite = WhiteSprite;
            Stretch(fillImg.rectTransform, 2f, 2f);
            return fillImg;
        }

        private static Sprite _white;
        public static Sprite WhiteSprite
        {
            get
            {
                if (_white == null)
                {
                    var tex = Texture2D.whiteTexture;
                    _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                return _white;
            }
        }

        public static Image Icon(Transform parent, Sprite sprite, float size)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sprite == null) img.color = new Color(1f, 1f, 1f, 0.25f);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = size;
            le.minHeight = size;
            le.preferredWidth = size;
            le.preferredHeight = size;
            return img;
        }

        // ------------------------------------------------------------------ layout

        public static HorizontalLayoutGroup Horizontal(Transform parent, string name, float spacing = 8f, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = Group(parent, name);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset(8, 8, 4, 4);
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static VerticalLayoutGroup Vertical(Transform parent, string name, float spacing = 6f, RectOffset padding = null, TextAnchor align = TextAnchor.UpperLeft)
        {
            var rt = Group(parent, name);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(10, 10, 8, 8);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static LayoutElement Size(Component c, float? width = null, float? height = null, float flexibleWidth = 0f)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (width.HasValue) { le.minWidth = width.Value; le.preferredWidth = width.Value; }
            if (height.HasValue) { le.minHeight = height.Value; le.preferredHeight = height.Value; }
            le.flexibleWidth = flexibleWidth;
            return le;
        }

        public static ContentSizeFitter FitHeight(Component c)
        {
            var f = c.gameObject.GetComponent<ContentSizeFitter>() ?? c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return f;
        }

        // ------------------------------------------------------------------ ancoragem

        public static void Stretch(RectTransform rt, float marginX = 0f, float marginY = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(marginX, marginY);
            rt.offsetMax = new Vector2(-marginX, -marginY);
        }

        /// <summary>Ancora em uma borda da tela. anchorMin/Max em frações; size em pixels de referência.</summary>
        public static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        public static void TopStretch(RectTransform rt, float height, float yOffset = 0f)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yOffset);
            rt.sizeDelta = new Vector2(0f, height);
        }

        public static void BottomStretch(RectTransform rt, float height, float yOffset = 0f)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, yOffset);
            rt.sizeDelta = new Vector2(0f, height);
        }

        public static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        public static string Signed(float value, string suffix = "")
        {
            int v = Mathf.RoundToInt(value);
            return (v > 0 ? "+" : "") + v + suffix;
        }
    }
}
