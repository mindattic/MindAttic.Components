namespace MindAttic.Components.Unity.Core
{
    /// <summary>
    /// Sizes, in canvas units at the 1920x1080 reference resolution every MindAttic canvas uses: one spacing
    /// scale, three corner radii and a type ramp. The web components are drawn for a desktop page at about half
    /// this density, so each value is the web's pixel size doubled (a 6px web radius is 12 here, a 12px web
    /// label 24). Mutable so a game can tune one copy; <see cref="Default"/> returns a fresh one each call.
    /// </summary>
    [System.Serializable]
    public sealed class ThemeMetrics
    {
        /// <summary>The base step of the spacing scale; every gap is a multiple of it.</summary>
        public float Unit = 4f;

        /// <summary>Spacing scale: 4.</summary>
        public float SpaceXs => Unit;
        /// <summary>Spacing scale: 8.</summary>
        public float SpaceSm => Unit * 2;
        /// <summary>Spacing scale: 16.</summary>
        public float SpaceMd => Unit * 4;
        /// <summary>Spacing scale: 24.</summary>
        public float SpaceLg => Unit * 6;
        /// <summary>Spacing scale: 32.</summary>
        public float SpaceXl => Unit * 8;
        /// <summary>Spacing scale: 48.</summary>
        public float SpaceXxl => Unit * 12;

        /// <summary>Small radius: buttons, fields, icon buttons (web 6px).</summary>
        public float RadiusSm = 12f;
        /// <summary>Medium radius: menus, lists, toasts (web 8px).</summary>
        public float RadiusMd = 16f;
        /// <summary>Large radius: dialogs and menu panels (web 12px).</summary>
        public float RadiusLg = 24f;

        /// <summary>Hairline border width.</summary>
        public float BorderWidth = 2f;
        /// <summary>Keyboard focus ring width.</summary>
        public float FocusRingWidth = 3f;
        /// <summary>Gap between a control's edge and its focus ring.</summary>
        public float FocusRingOffset = 3f;

        /// <summary>Height of buttons, fields, selects and toggles.</summary>
        public float ControlHeight = 60f;
        /// <summary>Side length of an icon button.</summary>
        public float IconButtonSize = 52f;
        /// <summary>Icon size inside a button.</summary>
        public float IconSize = 26f;
        /// <summary>Height of a list/roster row.</summary>
        public float RowHeight = 56f;
        /// <summary>Default width of a menu screen's panel.</summary>
        public float MenuWidth = 640f;
        /// <summary>Default width of a modal dialog.</summary>
        public float DialogWidth = 680f;
        /// <summary>Width of the label column in a form row.</summary>
        public float LabelWidth = 220f;

        /// <summary>Captions, tags, tooltips (web 11px).</summary>
        public float FontCaption = 20f;
        /// <summary>Body copy and labels (web 12px).</summary>
        public float FontBody = 24f;
        /// <summary>Text on controls.</summary>
        public float FontControl = 26f;
        /// <summary>Dialog titles and section headings (web 15px).</summary>
        public float FontHeading = 32f;
        /// <summary>Menu screen titles.</summary>
        public float FontTitle = 46f;
        /// <summary>The read-only join-code display.</summary>
        public float FontCode = 40f;

        /// <summary>Hover/press colour transitions (the web's 0.15s).</summary>
        public float DurationFast = 0.15f;
        /// <summary>Panel and screen transitions (the web button's 0.28s).</summary>
        public float DurationBase = 0.28f;
        /// <summary>Delay before a tooltip shows.</summary>
        public float TooltipDelay = 0.45f;
        /// <summary>How long a toast stays up by default.</summary>
        public float ToastSeconds = 3.5f;

        /// <summary>A fresh copy of the defaults.</summary>
        public static ThemeMetrics Default => new ThemeMetrics();

        /// <summary>A member-wise copy.</summary>
        public ThemeMetrics Clone() => (ThemeMetrics)MemberwiseClone();
    }
}
