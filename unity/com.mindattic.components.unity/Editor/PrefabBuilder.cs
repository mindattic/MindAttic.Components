using System;
using System.Collections.Generic;
using System.IO;
using MindAttic.Components.Unity.Core;
using UnityEditor;
using UnityEngine;

namespace MindAttic.Components.Unity.Editor
{
    /// <summary>
    /// Builds the package's prefabs (Runtime/Prefabs) and theme assets (Runtime/Themes) from the same code as the
    /// runtime <c>Create</c> methods, so a prefab and a code-built control are identical. Sprites are generated at
    /// runtime and re-applied when a prefab instance is enabled, so the saved prefabs carry no textures.
    /// Maintainers rerun it after changing a component: MindAttic > Components > Rebuild Package Prefabs.
    /// </summary>
    public static class PrefabBuilder
    {
        /// <summary>The package's own folder in the AssetDatabase.</summary>
        public const string PackageRoot = "Packages/com.mindattic.components.unity";

        /// <summary>Every prefab the builder makes: file name and how to build it under a parent.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, Func<Transform, Component>>> Recipes = new List<KeyValuePair<string, Func<Transform, Component>>>
        {
            Recipe("Button", p => MaButton.Create(p, "Button")),
            Recipe("Primary Button", p => MaButton.Create(p, "Primary", null, ButtonVariant.Primary)),
            Recipe("Danger Button", p => MaButton.Create(p, "Danger", null, ButtonVariant.Danger)),
            Recipe("Icon Button", p => MaIconButton.Create(p, MaIcon.Settings, null, "Settings")),
            Recipe("Toggle", p => MaToggle.Create(p, "Toggle", true)),
            Recipe("Textbox", p => MaTextbox.Create(p, "Label", "Placeholder")),
            Recipe("Select", p => MaSelect.Create(p, new[] { "Option A", "Option B", "Option C" })),
            Recipe("Range", p => MaRange.Create(p, 0, 100, 50, null, true)),
            Recipe("Progress Bar", p => MaProgressBar.Create(p, 0.4f)),
            Recipe("Spinner", p => MaSpinner.Create(p)),
            Recipe("Tag", p => MaTag.Create(p, "Tag")),
            Recipe("Code Display", p => MaCodeDisplay.Create(p, "Join code", "ABC123")),
            Recipe("List", p => MaList.Create(p, 6)),
            Recipe("Toolbar", p => MaToolbar.Create(p)),
            Recipe("Panel", p => MaPanel.Create(p)),
            Recipe("Menu Screen", p =>
            {
                var s = MenuScreen.Create(p, "Menu");
                s.AddButton("Play", null, ButtonVariant.Primary);
                s.AddButton("Settings", null);
                s.AddButton("Quit", null, ButtonVariant.Danger);
                return s;
            }),
            Recipe("Menu Stack", p => MenuStack.Create(p)),
        };

        static KeyValuePair<string, Func<Transform, Component>> Recipe(string name, Func<Transform, Component> build) =>
            new KeyValuePair<string, Func<Transform, Component>>(name, build);

        /// <summary>Rebuilds Runtime/Prefabs and Runtime/Themes inside the package.</summary>
        [MenuItem("MindAttic/Components/Rebuild Package Prefabs")]
        public static void RebuildPackage()
        {
            BuildPrefabs(PackageRoot + "/Runtime/Prefabs");
            BuildThemes(PackageRoot + "/Runtime/Themes");
        }

        /// <summary>For batch mode: <c>-executeMethod MindAttic.Components.Unity.Editor.PrefabBuilder.RebuildPackageForBatch</c>.</summary>
        public static void RebuildPackageForBatch()
        {
            try
            {
                RebuildPackage();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Saves every recipe as a prefab in <paramref name="folder"/> (an AssetDatabase path); returns their paths.</summary>
        public static List<string> BuildPrefabs(string folder)
        {
            EnsureFolder(folder);
            var paths = new List<string>();
            var canvas = MaCanvas.Create("Prefab Builder");
            try
            {
                foreach (var r in Recipes)
                {
                    var c = r.Value(canvas.transform);
                    var go = c.gameObject;
                    go.name = r.Key;
                    StripRuntimeArtwork(go);
                    string path = folder + "/" + r.Key + ".prefab";
                    PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
                    if (!ok) throw new InvalidOperationException("Could not save " + path);
                    paths.Add(path);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                MaCanvas.DestroyOverlay();
            }
            AssetDatabase.SaveAssets();
            return paths;
        }

        /// <summary>Saves Dark.asset and Light.asset (the built-in palettes) in <paramref name="folder"/>.</summary>
        public static List<string> BuildThemes(string folder)
        {
            EnsureFolder(folder);
            var paths = new List<string>();
            foreach (var palette in new[] { ThemePalette.Dark, ThemePalette.Light })
            {
                string path = folder + "/" + (palette.IsDark ? "Dark" : "Light") + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<ThemeTokens>(path);
                if (existing != null)
                {
                    existing.SetPalette(palette);
                    existing.metrics = ThemeMetrics.Default;
                    EditorUtility.SetDirty(existing);
                }
                else
                {
                    var t = ThemeTokens.Create(palette);
                    t.name = palette.IsDark ? "Dark" : "Light";
                    AssetDatabase.CreateAsset(t, path);
                }
                paths.Add(path);
            }
            AssetDatabase.SaveAssets();
            return paths;
        }

        // Runtime-generated sprites (and anything else not saved as an asset) must not be referenced by a prefab.
        static void StripRuntimeArtwork(GameObject go)
        {
            foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (img.sprite != null && !EditorUtility.IsPersistent(img.sprite)) img.sprite = null;
            }
            go.transform.localScale = Vector3.one;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
