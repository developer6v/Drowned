using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Drowned.UI
{
    /// <summary>Mensagens rápidas no centro-baixo da tela (erro de construção, novo especialista, "Salvo!").</summary>
    public sealed class ToastView : MonoBehaviour
    {
        private readonly List<(Text text, float until)> _active = new List<(Text, float)>();
        private VerticalLayoutGroup _stack;

        public void Build(Transform canvas)
        {
            _stack = UiKit.Vertical(canvas, "Toasts", 4f, new RectOffset(0, 0, 0, 0), TextAnchor.LowerCenter);
            _stack.childForceExpandWidth = false;
            var rt = _stack.GetComponent<RectTransform>();
            UiKit.Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 200f));
        }

        public void Show(string message, Color? color = null, float seconds = 3f)
        {
            if (_stack == null) return;
            var panel = UiKit.Panel(_stack.transform, "Toast", UiTheme.PanelBg);
            panel.raycastTarget = false;
            var h = UiKit.Horizontal(panel.transform, "Row", 0f, new RectOffset(16, 16, 8, 8), TextAnchor.MiddleCenter);
            UiKit.Stretch(h.GetComponent<RectTransform>());
            var text = UiKit.Label(h.transform, message, UiTheme.FontNormal, color ?? UiTheme.Text, TextAnchor.MiddleCenter);
            UiKit.FitHeight(panel);
            var le = panel.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 700f;
            _active.Add((text, Time.unscaledTime + seconds));
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < _active[i].until) continue;
                if (_active[i].text != null) Destroy(_active[i].text.transform.parent.parent.gameObject);
                _active.RemoveAt(i);
            }
        }
    }
}
