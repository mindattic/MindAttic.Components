using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// Sets <see cref="MaTheme.Current"/> and <see cref="MaTheme.ReduceMotion"/> from the inspector while it is
    /// enabled. Drop one in a game's first scene with your <see cref="ThemeTokens"/> asset (empty means the
    /// built-in dark theme); changing either field in Play Mode restyles everything live.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Theme Provider")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ThemeProvider : MonoBehaviour
    {
        [SerializeField] ThemeTokens theme;
        [SerializeField] bool reduceMotion;

        /// <summary>The theme this provider applies (null is the built-in dark theme).</summary>
        public ThemeTokens Theme
        {
            get => theme;
            set { theme = value; Push(); }
        }

        /// <summary>The reduce-motion setting this provider applies.</summary>
        public bool ReduceMotionSetting
        {
            get => reduceMotion;
            set { reduceMotion = value; Push(); }
        }

        void OnEnable() => Push();

        void OnValidate()
        {
            if (isActiveAndEnabled) Push();
        }

        void Push()
        {
            MaTheme.ReduceMotion = reduceMotion;
            MaTheme.Apply(theme != null ? theme : MaTheme.Dark);
        }
    }
}
