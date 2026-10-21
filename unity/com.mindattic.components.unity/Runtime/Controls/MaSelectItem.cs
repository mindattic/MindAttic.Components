using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// One row of an open <see cref="MaSelect"/> list (TextMesh Pro clones the template's item for every option):
    /// hover and focus light the row like the web's <c>.ma-menu__item</c>, the chosen option shows a check.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class MaSelectItem : MonoBehaviour
    {
        [SerializeField] Toggle toggle;
        [SerializeField] Image background;
        [SerializeField] Image check;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] MaStateRelay relay;
        bool _hooked;

        internal void Init(Toggle t, Image bg, Image ch, TextMeshProUGUI lbl)
        {
            toggle = t;
            background = bg;
            check = ch;
            label = lbl;
            relay = MaStateRelay.Attach(t);
        }

        internal void ApplyTheme(ThemeTokens theme)
        {
            MaSprites.Apply(background, MaSprites.Rounded(theme.Metrics.RadiusSm));
            MaUi.Style(label, TextStyle.Control, theme);
            check.sprite = MaIcons.Get(MaIcon.Check);
            Refresh();
        }

        void OnEnable()
        {
            if (!_hooked && relay != null)
            {
                relay.Changed += Refresh;
                _hooked = true;
            }
            if (label != null) ApplyTheme(MaTheme.Current);
        }

        void OnDisable()
        {
            if (_hooked) relay.Changed -= Refresh;
            _hooked = false;
        }

        void Refresh()
        {
            if (relay == null || background == null) return;
            var p = MaTheme.Palette;
            var state = relay.State;
            bool lit = state == ControlState.Hover || state == ControlState.Pressed || relay.Focused;
            background.color = lit ? p.Bg3.ToColor() : p.Bg3.WithAlpha(0).ToColor();
            label.color = (lit ? p.Accent : toggle != null && toggle.isOn ? p.Text : p.Text2).ToColor();
            check.color = p.Accent.ToColor();
        }
    }
}
