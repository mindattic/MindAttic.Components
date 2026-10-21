using System.IO;
using MindAttic.Components.Unity.Core;
using UnityEditor;
using UnityEngine;

namespace MindAttic.Components.Unity.Editor
{
    /// <summary>
    /// The ThemeTokens inspector: the fields, then a WCAG check of the palette, buttons to load a web theme
    /// stylesheet (any <c>web/themes/*/*.css</c>), reset to the built-in dark/light palette, copy the palette as
    /// CSS, and make this theme current (restyling every live component).
    /// </summary>
    [CustomEditor(typeof(ThemeTokens))]
    public sealed class ThemeTokensEditor : UnityEditor.Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var t = (ThemeTokens)target;

            EditorGUILayout.Space();
            var issues = ThemeValidator.Validate(t.Palette);
            if (issues.Count == 0) EditorGUILayout.HelpBox("Text and focus colours meet WCAG 2.2 AA.", MessageType.Info);
            else EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load web theme CSS..."))
                {
                    string path = EditorUtility.OpenFilePanel("Web theme stylesheet", "", "css");
                    if (!string.IsNullOrEmpty(path))
                    {
                        string css = File.ReadAllText(path);
                        var names = CssTokenParser.ThemeNames(css);
                        string name = names.Count > 0 ? names[names.Count - 1] : Path.GetFileNameWithoutExtension(path);
                        Undo.RecordObject(t, "Load theme CSS");
                        LoadCss(t, css, name);
                        EditorUtility.SetDirty(t);
                    }
                }
                if (GUILayout.Button("Reset to Dark"))
                {
                    Undo.RecordObject(t, "Reset theme");
                    t.SetPalette(ThemePalette.Dark);
                    EditorUtility.SetDirty(t);
                }
                if (GUILayout.Button("Reset to Light"))
                {
                    Undo.RecordObject(t, "Reset theme");
                    t.SetPalette(ThemePalette.Light);
                    EditorUtility.SetDirty(t);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy as CSS")) EditorGUIUtility.systemCopyBuffer = t.ToCss();
                if (GUILayout.Button("Make current")) MaTheme.Apply(t);
            }
        }

        /// <summary>Loads one theme of a stylesheet into an existing asset (tokens the CSS leaves out keep built-in values).</summary>
        public static void LoadCss(ThemeTokens tokens, string css, string themeName) =>
            tokens.SetPalette(ThemePalette.FromCss(css, themeName));
    }
}
