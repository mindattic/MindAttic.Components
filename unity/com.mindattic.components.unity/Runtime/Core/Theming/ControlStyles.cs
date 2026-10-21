namespace MindAttic.Components.Unity.Core
{
    /// <summary>The four button looks, after the web's <c>.ma-tool-btn</c>, <c>--primary</c>, <c>.ma-btn</c> and <c>.ma-tool-link</c>.</summary>
    public enum ButtonVariant
    {
        /// <summary>The one emphasized action: an accent fill (<c>.ma-tool-btn--primary</c>).</summary>
        Primary,
        /// <summary>The default: a raised surface with a hairline border that lights up in the accent (<c>.ma-tool-btn</c>).</summary>
        Secondary,
        /// <summary>A destructive action: outlined in red, filled red on hover (<c>.ma-btn</c>).</summary>
        Danger,
        /// <summary>No fill and no border until hovered (<c>.ma-tool-link</c>).</summary>
        Ghost,
    }

    /// <summary>A control's pointer/interaction state. Keyboard focus is separate (a ring), so it combines with any of these.</summary>
    public enum ControlState
    {
        /// <summary>At rest.</summary>
        Normal,
        /// <summary>Under the pointer.</summary>
        Hover,
        /// <summary>Held down.</summary>
        Pressed,
        /// <summary>Not interactable.</summary>
        Disabled,
    }

    /// <summary>The three colours a control is drawn with in one state.</summary>
    public readonly struct ControlColors
    {
        /// <summary>The surface fill.</summary>
        public readonly ThemeColor Fill;
        /// <summary>The outline.</summary>
        public readonly ThemeColor Border;
        /// <summary>The label and icon.</summary>
        public readonly ThemeColor Text;

        /// <summary>Creates a colour triple.</summary>
        public ControlColors(ThemeColor fill, ThemeColor border, ThemeColor text)
        {
            Fill = fill;
            Border = border;
            Text = text;
        }

        /// <summary>The same triple at a fraction of its opacity (disabled controls).</summary>
        public ControlColors Faded(float alpha) =>
            new ControlColors(Fill.WithAlpha(Fill.A * alpha), Border.WithAlpha(Border.A * alpha), Text.WithAlpha(Text.A * alpha));
    }

    /// <summary>
    /// The colour rules for every control and state, engine-free so they are unit-tested and identical
    /// wherever they are drawn. Each mirrors the web component's CSS for the same palette.
    /// </summary>
    public static class ControlStyles
    {
        /// <summary>Opacity of a disabled control.</summary>
        public const float DisabledAlpha = 0.45f;

        /// <summary>A button's colours.</summary>
        public static ControlColors Button(ThemePalette p, ButtonVariant variant, ControlState state)
        {
            ControlColors c;
            switch (variant)
            {
                case ButtonVariant.Primary:
                    switch (state)
                    {
                        case ControlState.Hover: c = new ControlColors(p.Accent2, p.Accent2, p.OnAccent); break;
                        case ControlState.Pressed: c = new ControlColors(p.Accent.Lerp(p.Bg, 0.18f), p.Accent.Lerp(p.Bg, 0.18f), p.OnAccent); break;
                        default: c = new ControlColors(p.Accent, p.Accent, p.OnAccent); break;
                    }
                    break;
                case ButtonVariant.Danger:
                    switch (state)
                    {
                        case ControlState.Hover: c = new ControlColors(p.Danger, p.Danger2, p.OnAccent); break;
                        case ControlState.Pressed: c = new ControlColors(p.Danger, p.Danger, p.OnAccent); break;
                        default: c = new ControlColors(p.Bg2, p.Danger, p.Text); break;
                    }
                    break;
                case ButtonVariant.Ghost:
                    switch (state)
                    {
                        case ControlState.Hover: c = new ControlColors(p.Bg3, p.Bg3.WithAlpha(0), p.Accent); break;
                        case ControlState.Pressed: c = new ControlColors(p.Bg3.Lerp(p.Border, 0.5f), p.Bg3.WithAlpha(0), p.Accent); break;
                        default: c = new ControlColors(p.Bg3.WithAlpha(0), p.Bg3.WithAlpha(0), p.Text2); break;
                    }
                    break;
                default: // Secondary
                    switch (state)
                    {
                        case ControlState.Hover: c = new ControlColors(p.Bg3, p.Accent, p.Text); break;
                        case ControlState.Pressed: c = new ControlColors(p.Bg3.Lerp(p.Border, 0.5f), p.Accent, p.Text); break;
                        default: c = new ControlColors(p.Bg2, p.Border, p.Text2); break;
                    }
                    break;
            }
            return state == ControlState.Disabled ? c.Faded(DisabledAlpha) : c;
        }

        /// <summary>
        /// A text field's or select's colours (<c>.ma-select</c>, <c>.ma-field</c>): the border lights in the accent
        /// on hover and while editing.
        /// </summary>
        public static ControlColors Field(ThemePalette p, ControlState state, bool editing)
        {
            ControlColors c;
            if (editing) c = new ControlColors(p.Bg, p.Accent, p.Text);
            else if (state == ControlState.Hover || state == ControlState.Pressed) c = new ControlColors(p.Bg2, p.Accent, p.Text);
            else c = new ControlColors(p.Bg2, p.Border, p.Text);
            return state == ControlState.Disabled ? c.Faded(DisabledAlpha) : c;
        }

        /// <summary>A toggle switch's track (fill/border) and knob (text) colours.</summary>
        public static ControlColors Switch(ThemePalette p, bool on, ControlState state)
        {
            ControlColors c = on
                ? new ControlColors(state == ControlState.Hover ? p.Accent2 : p.Accent, state == ControlState.Hover ? p.Accent2 : p.Accent, p.OnAccent)
                : new ControlColors(p.Bg3, state == ControlState.Hover ? p.Accent : p.Border, state == ControlState.Hover ? p.Text : p.Text2);
            return state == ControlState.Disabled ? c.Faded(DisabledAlpha) : c;
        }

        /// <summary>A list/roster row's colours (<c>.ma-menu__item</c>: a hover lights the row in bg3 and the text in the accent).</summary>
        public static ControlColors Row(ThemePalette p, ControlState state)
        {
            ControlColors c = state == ControlState.Hover || state == ControlState.Pressed
                ? new ControlColors(p.Bg3, p.Bg3, p.Text)
                : new ControlColors(p.Bg3.WithAlpha(0), p.Bg3.WithAlpha(0), p.Text);
            return state == ControlState.Disabled ? c.Faded(DisabledAlpha) : c;
        }

        /// <summary>The keyboard/gamepad focus ring: the accent (the web's <c>:focus-visible</c>).</summary>
        public static ThemeColor FocusRing(ThemePalette p) => p.Accent;

        /// <summary>The stripe colour of a toast of the given kind.</summary>
        public static ThemeColor Toast(ThemePalette p, ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Success: return p.Success;
                case ToastKind.Warning: return p.Warning;
                case ToastKind.Error: return p.Danger;
                default: return p.Accent;
            }
        }
    }
}
