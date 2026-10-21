using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A label beside a control, with an optional live readout on the right (the web's <c>.ma-form-row</c>):
    /// <c>MaFormRow.Create(menu.Items, "Volume", MaRange.Create(null, 0, 100, 80))</c>. Navigation and validation
    /// see the control under the row's label.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Form Row")]
    public sealed class MaFormRow : MaElement
    {
        [SerializeField] MaText label;
        [SerializeField] MaElement control;
        [SerializeField] MaText value;

        /// <summary>Creates a row and moves <paramref name="control"/> into it.</summary>
        public static MaFormRow Create(Component parent, string label, MaElement control, bool showValue = false)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Form Row: " + MaText.Short(label), ResolveParent(parent));
            var row = rt.gameObject.AddComponent<MaFormRow>();
            MaUi.HRow(rt.gameObject, m.SpaceMd, null, TextAnchor.MiddleLeft);
            MaUi.Layout(rt.gameObject, minH: m.ControlHeight, flexW: 1);
            row.label = MaText.Create(rt, label, TextStyle.Label);
            row.label.Tmp.textWrappingMode = TextWrappingModes.Normal; // a long label wraps rather than pushing the controls out of line
            MaUi.Layout(row.label.gameObject, minW: m.LabelWidth * 0.6f, prefW: m.LabelWidth, flexW: 0);
            if (control != null)
            {
                row.control = control;
                control.transform.SetParent(rt, false);
                control.LayoutElement.flexibleWidth = 1;
            }
            if (showValue)
            {
                row.value = MaText.Create(rt, "", TextStyle.Body, TextAlignmentOptions.MidlineRight);
                MaUi.Layout(row.value.gameObject, minW: 72, prefW: 88, flexW: 0);
                if (control is MaRange range) range.ShowValueIn(row.value.Tmp);
            }
            row.ApplyCurrentTheme();
            return row;
        }

        /// <summary>The label text.</summary>
        public string Label
        {
            get => label.Text;
            set => label.Text = value;
        }

        /// <summary>The control in the row.</summary>
        public MaElement Control => control;

        /// <summary>The readout on the right, or null.</summary>
        public MaText ValueText => value;

        /// <inheritdoc />
        public override Selectable PrimarySelectable => control != null ? control.PrimarySelectable : null;

        /// <inheritdoc />
        public override void CollectNavRows(List<Selectable[]> rows)
        {
            if (control != null && control.gameObject.activeInHierarchy) control.CollectNavRows(rows);
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme) { }

        /// <inheritdoc />
        public override MenuItemDefinition Describe()
        {
            var d = control != null ? control.Describe() : new MenuItemDefinition(MenuItemKind.Custom, "", false);
            d.Label = Label;
            return d;
        }
    }
}
