using System;
using MindAttic.Components.Unity.Core;
using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A square button showing only an icon (the web's <c>.ma-icon-btn</c>): close, copy, back, settings. Give it
    /// a tooltip, which also becomes its accessible description.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Icon Button")]
    public sealed class MaIconButton : MaButton
    {
        [SerializeField] string tooltipText;

        /// <summary>Creates an icon button under <paramref name="parent"/>.</summary>
        public static MaIconButton Create(Component parent, MaIcon icon, Action onClick = null, string tooltip = null,
            ButtonVariant variant = ButtonVariant.Secondary)
        {
            var b = Build<MaIconButton>(ResolveParent(parent), tooltip ?? icon.ToString(), variant, icon, iconOnly: true);
            b.gameObject.name = "Icon Button: " + icon;
            if (onClick != null) b.button.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(tooltip)) b.Tooltip = tooltip;
            return b;
        }

        /// <summary>The tooltip shown on hover and focus (null for none).</summary>
        public string Tooltip
        {
            get => tooltipText;
            set
            {
                tooltipText = value;
                if (!string.IsNullOrEmpty(value)) MaTooltip.Attach(this, value);
                else if (TryGetComponent<MaTooltip>(out var t)) MaUi.Destroy(t);
            }
        }

        /// <summary>Changes the side length (default <see cref="ThemeMetrics.IconButtonSize"/>).</summary>
        public MaIconButton WithSize(float size)
        {
            var le = LayoutElement;
            le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = size;
            return this;
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() =>
            new MenuItemDefinition(MenuItemKind.Button, string.IsNullOrEmpty(tooltipText) ? Icon.ToString() : tooltipText,
                Button != null && Button.IsInteractable() && gameObject.activeInHierarchy);
    }
}
