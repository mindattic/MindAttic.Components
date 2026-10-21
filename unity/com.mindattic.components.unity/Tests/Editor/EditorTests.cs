using System.Collections.Generic;
using System.IO;
using MindAttic.Components.Unity.Core;
using MindAttic.Components.Unity.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MindAttic.Components.Unity.Editor.Tests
{
    public class EditorTests
    {
        const string Package = PrefabBuilder.PackageRoot;
        readonly List<Object> _cleanup = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            MaTheme.UseDark();
            MaTheme.ReduceMotion = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _cleanup)
            {
                if (o != null) Object.DestroyImmediate(o is Component c ? c.gameObject : o);
            }
            _cleanup.Clear();
            MaCanvas.DestroyOverlay();
            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(es.gameObject);
            MaTheme.UseDark();
        }

        T Keep<T>(T o) where T : Object
        {
            _cleanup.Add(o);
            return o;
        }

        [Test]
        public void ThemeTokens_load_the_repos_web_theme_css()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(Package);
            Assert.That(info, Is.Not.Null);
            string themes = Path.GetFullPath(Path.Combine(info.resolvedPath, "..", "..", "web", "themes"));
            if (!Directory.Exists(themes)) Assert.Ignore("Not inside the MindAttic.Components checkout: " + themes);

            var dark = Keep(ThemeTokens.FromCss(File.ReadAllText(Path.Combine(themes, "Dark", "dark.css")), "dark"));
            var light = Keep(ThemeTokens.FromCss(File.ReadAllText(Path.Combine(themes, "Light", "light.css")), "light"));
            foreach (string token in ThemeTokenNames.Web)
            {
                Assert.That(dark.Palette[token], Is.EqualTo(ThemePalette.Dark[token]), "dark " + token);
                Assert.That(light.Palette[token], Is.EqualTo(ThemePalette.Light[token]), "light " + token);
            }

            // Any other web theme reads into a full asset too (Ocean defines its own palette).
            var ocean = Path.Combine(themes, "Ocean", "ocean.css");
            if (File.Exists(ocean))
            {
                string css = File.ReadAllText(ocean);
                var names = CssTokenParser.ThemeNames(css);
                Assert.That(names, Is.Not.Empty);
                var t = Keep(ThemeTokens.CreateDark());
                ThemeTokensEditor.LoadCss(t, css, names[names.Count - 1]);
                Assert.That(t.ThemeName, Is.EqualTo(names[names.Count - 1]));
            }
        }

        [Test]
        public void Package_theme_assets_match_the_built_in_palettes()
        {
            var dark = AssetDatabase.LoadAssetAtPath<ThemeTokens>(Package + "/Runtime/Themes/Dark.asset");
            var light = AssetDatabase.LoadAssetAtPath<ThemeTokens>(Package + "/Runtime/Themes/Light.asset");
            Assert.That(dark, Is.Not.Null);
            Assert.That(light, Is.Not.Null);
            foreach (var kv in ThemePalette.Dark.Colors) Assert.That(dark.Palette[kv.Key], Is.EqualTo(kv.Value), "dark " + kv.Key);
            foreach (var kv in ThemePalette.Light.Colors) Assert.That(light.Palette[kv.Key], Is.EqualTo(kv.Value), "light " + kv.Key);
        }

        [Test]
        public void Every_package_prefab_instantiates_themed_and_restyles()
        {
            var canvas = Keep(MaCanvas.Create("Prefab Test"));
            foreach (var recipe in PrefabBuilder.Recipes)
            {
                string path = Package + "/Runtime/Prefabs/" + recipe.Key + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
                var el = MaElement.Of(go.transform);
                Assert.That(el, Is.Not.Null, recipe.Key + " has a MindAttic element");
                foreach (var img in go.GetComponentsInChildren<Image>(false))
                {
                    if (!img.gameObject.activeInHierarchy) continue; // hidden parts are styled when they are enabled
                    if (img.name == "Rule" || img.name == "Backdrop") continue; // flat colour by design
                    Assert.That(img.sprite, Is.Not.Null, recipe.Key + "/" + img.name + " got its generated sprite back");
                }
            }
            var button = canvas.GetComponentInChildren<MaButton>();
            MaTheme.UseLight();
            Assert.That(button.Surface.TargetFill, Is.EqualTo(ThemePalette.Light.Bg2.ToColor()), "a prefab instance restyles live");
        }

        [Test]
        public void Prefab_builder_writes_one_prefab_per_recipe()
        {
            const string folder = "Assets/MaPrefabBuilderTest";
            try
            {
                var paths = PrefabBuilder.BuildPrefabs(folder);
                Assert.That(paths.Count, Is.EqualTo(PrefabBuilder.Recipes.Count));
                foreach (string p in paths) Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(p), Is.Not.Null, p);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void GameObject_menu_builds_on_a_new_canvas_when_nothing_is_selected()
        {
            var parent = MaGameObjectMenu.ParentFor(null);
            Keep(parent.gameObject);
            Assert.That(parent.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(parent.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
            Assert.That(Object.FindAnyObjectByType<EventSystem>(), Is.Not.Null, "an EventSystem comes with it");

            var child = MaButton.Create(parent, "Child");
            Assert.That(MaGameObjectMenu.ParentFor(child.gameObject), Is.SameAs(child.transform), "builds under a selection on a canvas");
        }

        [Test]
        public void Components_theme_and_restyle_in_edit_mode()
        {
            var canvas = Keep(MaCanvas.Create("Edit Mode"));
            var screen = MenuScreen.Create(canvas, "Edit");
            var b = screen.AddButton("Primary", null, ButtonVariant.Primary);
            var text = screen.AddText("Hello");
            Assert.That(b.Surface.TargetFill, Is.EqualTo(ThemePalette.Dark.Accent.ToColor()));
            MaTheme.UseLight();
            Assert.That(b.Surface.TargetFill, Is.EqualTo(ThemePalette.Light.Accent.ToColor()));
            Assert.That(text.Tmp.color, Is.EqualTo(ThemePalette.Light.Text.ToColor()));
        }

        [Test]
        public void Scene_menu_validation_finds_broken_screens()
        {
            var canvas = Keep(MaCanvas.Create("Validation"));
            var good = MenuScreen.Create(canvas, "Good");
            good.AddButton("Play", null);
            var bad = MenuScreen.Create(canvas, "Bad");
            bad.AddButton("Same", null);
            bad.AddButton("Same", null);
            var issues = MenuValidation.ValidateOpenScenes();
            Assert.That(issues, Has.Some.Contains("Bad").And.Contains("two buttons"));
            Assert.That(issues, Has.None.Contains("Menu: Good"));
        }

        [Test]
        public void Tmp_essentials_are_present()
        {
            Assert.That(TmpEssentials.Present, Is.True);
            Assert.That(TmpEssentials.PackagePath, Is.Not.Null);
            Assert.That(MaTheme.Font, Is.Not.Null);
        }
    }
}
