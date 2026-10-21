using System;
using MindAttic.Components.Unity.Core;
using UnityEditor;
using UnityEngine;

namespace MindAttic.Components.Unity.Editor
{
    /// <summary>
    /// GameObject > MindAttic UI: adds a themed component under the selected object (or a new MindAttic canvas
    /// when nothing on a canvas is selected), built by the same code as the runtime <c>Create</c> methods.
    /// </summary>
    public static class MaGameObjectMenu
    {
        const string Root = "GameObject/MindAttic UI/";
        const int Priority = 10;

        /// <summary>The parent a menu item builds under: the selected object if it is on a canvas, else a new canvas.</summary>
        public static Transform ParentFor(GameObject selected)
        {
            if (selected != null && selected.GetComponentInParent<Canvas>(true) != null) return selected.transform;
            var canvas = MaCanvas.Create();
            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Create MindAttic Canvas");
            if (selected != null) canvas.transform.SetParent(selected.transform, false);
            return canvas.transform;
        }

        static T Make<T>(MenuCommand cmd, string undo, Func<Transform, T> create) where T : Component
        {
            var parent = ParentFor(cmd.context as GameObject ?? Selection.activeGameObject);
            var c = create(parent);
            Undo.RegisterCreatedObjectUndo(c.gameObject, undo);
            Selection.activeGameObject = c.gameObject;
            return c;
        }

        [MenuItem(Root + "Canvas (1920x1080)", false, Priority)]
        static void Canvas(MenuCommand cmd)
        {
            var c = MaCanvas.Create();
            Undo.RegisterCreatedObjectUndo(c.gameObject, "Create MindAttic Canvas");
            Selection.activeGameObject = c.gameObject;
        }

        [MenuItem(Root + "Theme Provider", false, Priority)]
        static void Provider(MenuCommand cmd)
        {
            var go = new GameObject("Theme Provider", typeof(ThemeProvider));
            Undo.RegisterCreatedObjectUndo(go, "Create Theme Provider");
            Selection.activeGameObject = go;
        }

        [MenuItem(Root + "Menu Stack", false, Priority + 20)]
        static void Stack(MenuCommand cmd) => Make(cmd, "Create Menu Stack", p => MenuStack.Create(p));

        [MenuItem(Root + "Menu Screen", false, Priority + 20)]
        static void Screen(MenuCommand cmd) => Make(cmd, "Create Menu Screen", p =>
        {
            var s = MenuScreen.Create(p, "Menu");
            s.AddButton("Play", null, ButtonVariant.Primary);
            s.AddButton("Settings", null);
            s.AddButton("Quit", null, ButtonVariant.Danger);
            return s;
        });

        [MenuItem(Root + "Button", false, Priority + 40)]
        static void Button(MenuCommand cmd) => Make(cmd, "Create Button", p => MaButton.Create(p, "Button"));

        [MenuItem(Root + "Icon Button", false, Priority + 40)]
        static void IconButton(MenuCommand cmd) => Make(cmd, "Create Icon Button", p => MaIconButton.Create(p, MaIcon.Settings, null, "Settings"));

        [MenuItem(Root + "Toggle", false, Priority + 40)]
        static void Toggle(MenuCommand cmd) => Make(cmd, "Create Toggle", p => MaToggle.Create(p, "Toggle", true));

        [MenuItem(Root + "Textbox", false, Priority + 40)]
        static void Textbox(MenuCommand cmd) => Make(cmd, "Create Textbox", p => MaTextbox.Create(p, "Label", "Placeholder"));

        [MenuItem(Root + "Select", false, Priority + 40)]
        static void Select(MenuCommand cmd) => Make(cmd, "Create Select", p => MaSelect.Create(p, new[] { "Option A", "Option B", "Option C" }));

        [MenuItem(Root + "Range", false, Priority + 40)]
        static void Range(MenuCommand cmd) => Make(cmd, "Create Range", p => MaRange.Create(p, 0, 100, 50, null, true));

        [MenuItem(Root + "Progress Bar", false, Priority + 40)]
        static void Progress(MenuCommand cmd) => Make(cmd, "Create Progress Bar", p => MaProgressBar.Create(p, 0.4f));

        [MenuItem(Root + "Spinner", false, Priority + 40)]
        static void Spinner(MenuCommand cmd) => Make(cmd, "Create Spinner", p => MaSpinner.Create(p));

        [MenuItem(Root + "Toolbar", false, Priority + 40)]
        static void Toolbar(MenuCommand cmd) => Make(cmd, "Create Toolbar", p =>
        {
            var tb = MaToolbar.Create(p);
            tb.AddIconButton(MaIcon.Menu, null, "Menu");
            tb.AddText("Toolbar");
            tb.AddFlexibleSpace();
            tb.AddIconButton(MaIcon.Settings, null, "Settings");
            return tb;
        });

        [MenuItem(Root + "Code Display", false, Priority + 40)]
        static void Code(MenuCommand cmd) => Make(cmd, "Create Code Display", p => MaCodeDisplay.Create(p, "Join code", "ABC123"));

        [MenuItem(Root + "List", false, Priority + 40)]
        static void List(MenuCommand cmd) => Make(cmd, "Create List", p =>
        {
            var l = MaList.Create(p, 4);
            l.AddRow("Player 1", new Color(0.42f, 0.64f, 0.91f), "24 ms").AddTag("Host", TagTone.Accent);
            l.AddRow("Player 2", new Color(0.9f, 0.45f, 0.3f), "61 ms");
            return l;
        });

        [MenuItem(Root + "Tag", false, Priority + 40)]
        static void Tag(MenuCommand cmd) => Make(cmd, "Create Tag", p => MaTag.Create(p, "Tag"));

        [MenuItem(Root + "Text", false, Priority + 40)]
        static void Text(MenuCommand cmd) => Make(cmd, "Create Text", p => MaText.Create(p, "Text"));

        [MenuItem(Root + "Panel", false, Priority + 40)]
        static void Panel(MenuCommand cmd) => Make(cmd, "Create Panel", p => MaPanel.Create(p));
    }
}
