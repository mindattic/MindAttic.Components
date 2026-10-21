using System;
using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>The kind of an item on a menu screen.</summary>
    public enum MenuItemKind
    {
        /// <summary>A button.</summary>
        Button,
        /// <summary>A text field.</summary>
        Textbox,
        /// <summary>An on/off switch.</summary>
        Toggle,
        /// <summary>A dropdown.</summary>
        Select,
        /// <summary>A slider.</summary>
        Range,
        /// <summary>A read-only code with a copy button.</summary>
        Code,
        /// <summary>A list or roster.</summary>
        List,
        /// <summary>A progress bar or spinner.</summary>
        Progress,
        /// <summary>Static text, a section heading or a spacer.</summary>
        Text,
        /// <summary>A row of other controls.</summary>
        Row,
        /// <summary>Anything else.</summary>
        Custom,
    }

    /// <summary>One item of a <see cref="MenuDefinition"/>.</summary>
    public sealed class MenuItemDefinition
    {
        /// <summary>The kind.</summary>
        public MenuItemKind Kind;
        /// <summary>The visible label (empty for unlabelled text and spacers).</summary>
        public string Label;
        /// <summary>True when it can take focus right now.</summary>
        public bool Focusable;

        /// <summary>Creates an item.</summary>
        public MenuItemDefinition(MenuItemKind kind, string label, bool focusable)
        {
            Kind = kind;
            Label = label ?? "";
            Focusable = focusable;
        }

        /// <inheritdoc />
        public override string ToString() => Kind + " '" + Label + "'" + (Focusable ? "" : " (not focusable)");
    }

    /// <summary>An engine-free description of a menu screen (what <c>MenuScreen.DescribeMenu()</c> returns), for validation.</summary>
    public sealed class MenuDefinition
    {
        /// <summary>The screen title.</summary>
        public string Title;
        /// <summary>The items, top to bottom.</summary>
        public List<MenuItemDefinition> Items = new List<MenuItemDefinition>();

        /// <summary>Creates a definition.</summary>
        public MenuDefinition(string title)
        {
            Title = title ?? "";
        }

        /// <summary>Adds an item and returns this definition.</summary>
        public MenuDefinition Add(MenuItemKind kind, string label, bool focusable = true)
        {
            Items.Add(new MenuItemDefinition(kind, label, focusable));
            return this;
        }
    }

    /// <summary>
    /// Rules every MindAttic menu keeps: it has a title, at least one focusable item (so a gamepad player is never
    /// stranded), every interactive item has a label, and no two buttons share one.
    /// </summary>
    public static class MenuValidator
    {
        /// <summary>One line per broken rule; empty when the menu is fine.</summary>
        public static IReadOnlyList<string> Validate(MenuDefinition menu)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            var issues = new List<string>();
            string where = string.IsNullOrWhiteSpace(menu.Title) ? "(untitled menu)" : "'" + menu.Title + "'";
            if (string.IsNullOrWhiteSpace(menu.Title)) issues.Add("The menu has no title.");

            bool anyFocusable = false;
            var buttonLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < menu.Items.Count; i++)
            {
                var item = menu.Items[i];
                anyFocusable |= item.Focusable;
                bool interactive = item.Kind != MenuItemKind.Text && item.Kind != MenuItemKind.Progress &&
                                   item.Kind != MenuItemKind.Row && item.Kind != MenuItemKind.Custom && item.Kind != MenuItemKind.List;
                if (interactive && string.IsNullOrWhiteSpace(item.Label))
                    issues.Add(where + ": item " + (i + 1) + " (" + item.Kind + ") has no label.");
                if (item.Kind == MenuItemKind.Button && !string.IsNullOrWhiteSpace(item.Label) && !buttonLabels.Add(item.Label.Trim()))
                    issues.Add(where + ": two buttons are labelled '" + item.Label.Trim() + "'.");
            }
            if (!anyFocusable) issues.Add(where + ": nothing on it can take focus, so a keyboard or gamepad player is stuck.");
            return issues;
        }
    }
}
