using System;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// The theme provider: the one current <see cref="ThemeTokens"/> every component draws with. Setting
    /// <see cref="Current"/> (or <see cref="UseDark"/>/<see cref="UseLight"/>/<see cref="Toggle"/>) restyles every live
    /// component at once, and editing the current theme asset does too. <see cref="ReduceMotion"/> turns every
    /// transition instant. A <see cref="ThemeProvider"/> in a scene sets both from the inspector.
    /// </summary>
    public static class MaTheme
    {
        static ThemeTokens s_dark;
        static ThemeTokens s_light;
        static ThemeTokens s_current;
        static bool s_reduceMotion;

        /// <summary>Raised after the current theme changes or is edited.</summary>
        public static event Action<ThemeTokens> Changed;

        /// <summary>Raised after <see cref="ReduceMotion"/> changes.</summary>
        public static event Action<bool> ReduceMotionChanged;

        /// <summary>The built-in dark theme (the default; the web's <c>:root</c>).</summary>
        public static ThemeTokens Dark => s_dark != null ? s_dark : (s_dark = BuiltIn(ThemePalette.Dark));

        /// <summary>The built-in light theme (the web's <c>[data-theme="light"]</c>).</summary>
        public static ThemeTokens Light => s_light != null ? s_light : (s_light = BuiltIn(ThemePalette.Light));

        /// <summary>The theme every component uses; <see cref="Dark"/> until set.</summary>
        public static ThemeTokens Current
        {
            get => s_current != null ? s_current : Dark;
            set => Apply(value);
        }

        /// <summary>The current palette.</summary>
        public static ThemePalette Palette => Current.Palette;

        /// <summary>The current metrics.</summary>
        public static ThemeMetrics Metrics => Current.Metrics;

        /// <summary>True when the current theme is dark.</summary>
        public static bool IsDark => Current.IsDark;

        /// <summary>
        /// Every transition, slide, scale and fade becomes instant (spinners pulse instead of turning). Honour a
        /// player's accessibility setting by binding it here.
        /// </summary>
        public static bool ReduceMotion
        {
            get => s_reduceMotion;
            set
            {
                if (s_reduceMotion == value) return;
                s_reduceMotion = value;
                ReduceMotionChanged?.Invoke(value);
            }
        }

        /// <summary>The font every label uses: the theme's, else TMP Settings' default.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                var f = Current.font;
                if (f != null) return f;
                return TmpSettingsPresent ? TMP_Settings.defaultFontAsset : null;
            }
        }

        /// <summary>Makes <paramref name="theme"/> current (null means <see cref="Dark"/>) and restyles everything.</summary>
        public static void Apply(ThemeTokens theme)
        {
            s_current = theme;
            NotifyChanged();
        }

        /// <summary>Switches to the built-in dark theme.</summary>
        public static void UseDark() => Apply(Dark);

        /// <summary>Switches to the built-in light theme.</summary>
        public static void UseLight() => Apply(Light);

        /// <summary>Switches between the built-in dark and light themes.</summary>
        public static void Toggle() => Apply(IsDark ? Light : Dark);

        /// <summary>Restyles every component with the current theme (after editing its fields from code).</summary>
        public static void NotifyChanged() => Changed?.Invoke(Current);

        internal static bool IsCurrent(ThemeTokens t) => t != null && (ReferenceEquals(t, s_current) || (s_current == null && ReferenceEquals(t, s_dark)));

        static bool? s_tmpSettings;

        /// <summary>
        /// True when the project has TMP Settings (TextMesh Pro's Essential Resources). Without them TextMesh Pro
        /// cannot draw text; the package's editor imports them on first load.
        /// </summary>
        public static bool TmpSettingsPresent
        {
            get
            {
                if (s_tmpSettings == true) return true;
                s_tmpSettings = Resources.Load<TMP_Settings>("TMP Settings") != null;
                if (s_tmpSettings == false && !s_warnedTmp)
                {
                    s_warnedTmp = true;
                    Debug.LogWarning("[MindAttic.Components] TextMesh Pro Essential Resources are missing, so labels can't draw. " +
                                     "Use Window > TextMeshPro > Import TMP Essential Resources (or MindAttic > Components > Import TMP Essentials).");
                }
                return s_tmpSettings == true;
            }
        }

        static bool s_warnedTmp;

        static ThemeTokens BuiltIn(ThemePalette palette)
        {
            var t = ThemeTokens.Create(palette);
            t.name = "MindAttic " + (palette.IsDark ? "Dark" : "Light") + " (built-in)";
            t.hideFlags = HideFlags.DontSave;
            return t;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Enter Play Mode without a domain reload keeps statics; start every session on the defaults.
            s_current = null;
            s_reduceMotion = false;
            s_tmpSettings = null;
            Changed = null;
            ReduceMotionChanged = null;
        }
    }
}
