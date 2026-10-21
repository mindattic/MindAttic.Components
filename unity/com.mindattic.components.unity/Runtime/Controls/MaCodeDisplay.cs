using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A read-only code (a lobby's join code, an IP:port) in large spaced letters with a Copy button beside it:
    /// Copy puts it on the clipboard, flips to "Copied" for a moment and shows a toast. Only the button takes focus.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Code Display")]
    public sealed class MaCodeDisplay : MaElement
    {
        [SerializeField] MaText caption;
        [SerializeField] MaPanel box;
        [SerializeField] TextMeshProUGUI codeText;
        [SerializeField] MaButton copyButton;
        [SerializeField] string code = "";
        [SerializeField] bool toastOnCopy = true;
        [SerializeField] string copyLabel = "Copy";
        [SerializeField] string copiedLabel = "Copied";

        float _revertAt = -1;

        /// <summary>Raised with the code after it was copied.</summary>
        public event Action<string> Copied;

        /// <summary>Creates a code display (with <paramref name="label"/> as its caption when not empty) under <paramref name="parent"/>.</summary>
        public static MaCodeDisplay Create(Component parent, string label, string code)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Code: " + MaText.Short(label), ResolveParent(parent));
            var d = rt.gameObject.AddComponent<MaCodeDisplay>();
            MaUi.VColumn(rt.gameObject, m.SpaceSm, null, TextAnchor.UpperLeft);
            MaUi.Layout(rt.gameObject, flexW: 1);
            d.caption = MaText.Create(rt, label, TextStyle.Label);
            d.caption.gameObject.SetActive(!string.IsNullOrEmpty(label));

            var row = MaUi.Rect("Row", rt);
            MaUi.HRow(row.gameObject, m.SpaceMd, null, TextAnchor.MiddleLeft);
            d.box = MaPanel.Create(row, PanelStyle.Inset, "Code Box");
            MaUi.HRow(d.box.gameObject, 0, MaUi.Pad(m.SpaceLg, 0), TextAnchor.MiddleCenter, expandWidth: true);
            float h = m.ControlHeight + 16;
            // The box asks for no width of its own and takes what the Copy button leaves (prefW 0, flexW 1), so a long code in a
            // narrow menu never squeezes the button to "C..."; the code shrinks to fit the box instead (FitCode).
            MaUi.Layout(d.box.gameObject, minH: h, prefW: 0, prefH: h, flexW: 1);
            d.codeText = MaUi.Text(d.box.transform, code, TextStyle.Code, "Code");
            d.codeText.alignment = TextAlignmentOptions.Center;
            d.FitCode();
            d.copyButton = MaButton.Create(row, d.copyLabel, d.Copy, ButtonVariant.Secondary, MaIcon.Copy);
            d.copyButton.LayoutElement.minHeight = d.copyButton.LayoutElement.preferredHeight = h;
            d.Code = code;
            d.ApplyCurrentTheme();
            return d;
        }

        /// <summary>The code shown and copied.</summary>
        public string Code
        {
            get => code;
            set
            {
                code = value ?? "";
                codeText.text = string.IsNullOrEmpty(code) ? "—" : code;
                copyButton.Interactable = !string.IsNullOrEmpty(code);
            }
        }

        /// <summary>The caption above the code.</summary>
        public string Label
        {
            get => caption.Text;
            set
            {
                caption.Text = value;
                caption.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        /// <summary>Show a "Copied" toast after copying (on by default).</summary>
        public bool ToastOnCopy
        {
            get => toastOnCopy;
            set => toastOnCopy = value;
        }

        /// <summary>The copy button.</summary>
        public MaButton CopyButton => copyButton;

        /// <inheritdoc />
        public override Selectable PrimarySelectable => copyButton != null ? copyButton.Button : null;

        /// <summary>Copies the code to the system clipboard.</summary>
        public void Copy()
        {
            if (string.IsNullOrEmpty(code)) return;
            GUIUtility.systemCopyBuffer = code;
            copyButton.Label = copiedLabel;
            copyButton.Icon = MaIcon.Check;
            _revertAt = Time.unscaledTime + 1.6f;
            if (toastOnCopy) MaToast.Show("Copied " + code + " to the clipboard", ToastKind.Success);
            Copied?.Invoke(code);
        }

        void Update()
        {
            if (_revertAt < 0 || Time.unscaledTime < _revertAt) return;
            _revertAt = -1;
            copyButton.Label = copyLabel;
            copyButton.Icon = MaIcon.Copy;
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (codeText != null) MaUi.Style(codeText, TextStyle.Code, theme);
            FitCode();
        }

        /// <summary>
        /// Lets the code text shrink, down to half its style size, rather than run out of its box when the box is narrow.
        /// Called right after the Code style is applied, so the current size is the style's.
        /// </summary>
        void FitCode()
        {
            if (codeText == null) return;
            float size = codeText.fontSize;
            codeText.enableAutoSizing = true;
            codeText.fontSizeMax = size;
            codeText.fontSizeMin = size * 0.5f;
            codeText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>True when the whole code shows inside its box at the current layout (for tests and layout checks).</summary>
        public bool CodeFits
        {
            get
            {
                if (codeText == null) return false;
                codeText.ForceMeshUpdate();
                // Not cut off with an ellipsis, and inside the box (TMP's bounds count the spacing after the last letter, which
                // may run into the box's padding).
                return !codeText.isTextTruncated && codeText.textBounds.size.x <= box.RectTransform.rect.width;
            }
        }

        /// <summary>The size the code is drawn at now (smaller than the style's when it had to shrink to fit).</summary>
        public float CodeFontSize => codeText != null ? codeText.fontSize : 0f;

        /// <inheritdoc />
        public override MenuItemDefinition Describe() =>
            new MenuItemDefinition(MenuItemKind.Code, string.IsNullOrEmpty(Label) ? "Code" : Label, copyButton != null && copyButton.Interactable);
    }
}
