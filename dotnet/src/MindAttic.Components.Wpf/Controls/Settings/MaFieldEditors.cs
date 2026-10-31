using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>The built-in editor for each <see cref="MaFieldKinds"/> kind (M.COMP-T159).</summary>
public static class MaFieldEditors
{
    internal static void RegisterBuiltIns(MaFieldEditorRegistry registry)
    {
        registry.Register(MaFieldKinds.Text, c => TextEditor(c, StringValue));
        registry.Register(MaFieldKinds.Multiline, Multiline);
        registry.Register(MaFieldKinds.Url, c => TextEditor(c, UrlValue));
        registry.Register(MaFieldKinds.Integer, c => TextEditor(c, IntegerValue));
        registry.Register(MaFieldKinds.Decimal, c => TextEditor(c, DecimalValue));
        registry.Register(MaFieldKinds.TimeSpan, c => TextEditor(c, TimeSpanValue));
        registry.Register(MaFieldKinds.DateTime, c => TextEditor(c, DateTimeValue));
        registry.Register(MaFieldKinds.Bool, Bool);
        registry.Register(MaFieldKinds.Choice, Choice);
        registry.Register(MaFieldKinds.Path, Path);
        registry.Register(MaFieldKinds.StringList, StringList);
        registry.Register(MaFieldKinds.Dictionary, Dictionary);
        registry.Register(MaFieldKinds.Object, Object);
        registry.Register(MaFieldKinds.Collection, c => new CollectionEditor(c));
        registry.Register(MaFieldKinds.Unknown, ReadOnly);
    }

    /// <summary>A read-only box showing the value as JSON: unknown kinds, and any field whose editor failed.</summary>
    public static FrameworkElement ReadOnly(MaFieldContext context)
    {
        var box = new TextBox { Text = MaFieldContext.Text(context.Value), IsReadOnly = true, TextWrapping = TextWrapping.Wrap };
        box.SetResourceReference(FrameworkElement.StyleProperty, "MaTextBox");
        box.SetResourceReference(Control.ForegroundProperty, "MaMuted");
        return box;
    }

    /// <summary>A text box whose text <paramref name="parse"/> turns into the stored JSON or an error. The document is
    /// written only with a value that parses; the error shows on <see cref="MaSettingsForm.Validate"/>.</summary>
    public static TextBox TextEditor(MaFieldContext context, Func<MaFieldContext, string, (JsonNode? Value, string? Error)> parse, string style = "MaTextBox")
    {
        var box = new TextBox { Text = MaFieldContext.Text(context.Value) };
        box.SetResourceReference(FrameworkElement.StyleProperty, style);
        context.Validator = () => parse(context, box.Text).Error;
        box.TextChanged += (_, _) =>
        {
            var (value, error) = parse(context, box.Text);
            if (error is null) context.Write(value);
            else context.Revalidate();
        };
        return box;
    }

    private static FrameworkElement Multiline(MaFieldContext context)
    {
        var box = TextEditor(context, StringValue, "MaTextArea");
        box.MinHeight = 80;
        box.MaxHeight = 240;
        return box;
    }

    private static (JsonNode?, string?) StringValue(MaFieldContext c, string text) => (JsonValue.Create(text), null);

    private static (JsonNode?, string?) UrlValue(MaFieldContext c, string text)
    {
        var t = text.Trim();
        if (t.Length == 0) return (JsonValue.Create(""), null);
        return Uri.TryCreate(t, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "ws" or "wss" && uri.Host.Length > 0
            ? (JsonValue.Create(t), null)
            : (null, "Enter a full URL, e.g. https://example.com");
    }

    private static (JsonNode?, string?) IntegerValue(MaFieldContext c, string text)
    {
        var t = text.Trim();
        if (t.Length == 0) return c.Nullable ? (null, null) : (null, "Required: enter a whole number");
        if (!long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return (null, "Enter a whole number");
        return RangeError(c, n) is { } e ? (null, e) : (JsonValue.Create(n), null);
    }

    private static (JsonNode?, string?) DecimalValue(MaFieldContext c, string text)
    {
        var t = text.Trim();
        if (t.Length == 0) return c.Nullable ? (null, null) : (null, "Required: enter a number");
        JsonNode value;
        double number;
        if (decimal.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) { value = JsonValue.Create(m); number = (double)m; }
        else if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d)) { value = JsonValue.Create(d); number = d; }
        else return (null, "Enter a number (use . for decimals)");
        return RangeError(c, number) is { } e ? (null, e) : (value, null);
    }

    private static string? RangeError(MaFieldContext c, double n)
    {
        string F(double v) => v.ToString(CultureInfo.InvariantCulture);
        return (c.Min, c.Max) switch
        {
            ({ } min, { } max) when n < min || n > max => $"Must be between {F(min)} and {F(max)}",
            ({ } min, null) when n < min => $"Must be at least {F(min)}",
            (null, { } max) when n > max => $"Must be at most {F(max)}",
            _ => null,
        };
    }

    private static (JsonNode?, string?) TimeSpanValue(MaFieldContext c, string text)
    {
        var t = text.Trim();
        if (t.Length == 0) return c.Nullable ? (null, null) : (null, "Required: enter a duration as hh:mm:ss");
        // A bare number would parse as days; a duration here always has its colons.
        return t.Contains(':') && TimeSpan.TryParse(t, CultureInfo.InvariantCulture, out var ts)
            ? (JsonValue.Create(ts.ToString("c", CultureInfo.InvariantCulture)), null)
            : (null, "Enter a duration as hh:mm:ss (or d.hh:mm:ss)");
    }

    private static (JsonNode?, string?) DateTimeValue(MaFieldContext c, string text)
    {
        var t = text.Trim();
        if (t.Length == 0) return c.Nullable ? (null, null) : (null, "Required: enter a date, e.g. 2026-10-31T09:00:00");
        if (!DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return (null, "Enter a date, e.g. 2026-10-31T09:00:00");
        if (dt.Kind == DateTimeKind.Local && DateTimeOffset.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return (JsonValue.Create(dto), null);
        return (JsonValue.Create(dt), null);
    }

    private static FrameworkElement Bool(MaFieldContext context)
    {
        var check = new CheckBox { IsChecked = context.Value?.GetValueKind() == JsonValueKind.True, HorizontalAlignment = HorizontalAlignment.Left };
        check.SetResourceReference(FrameworkElement.StyleProperty, "MaCheck");
        check.Checked += (_, _) => context.Write(JsonValue.Create(true));
        check.Unchecked += (_, _) => context.Write(JsonValue.Create(false));
        return check;
    }

    private static FrameworkElement Choice(MaFieldContext context)
    {
        var picker = new MaPicker { HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 220 };
        var current = context.Value;
        MaPickerItem? selected = null;
        if (context.Field["choices"] is JsonArray choices)
            foreach (var choice in choices.OfType<JsonObject>())
            {
                var value = choice["value"];
                var item = new MaPickerItem { Content = ManifestJson.Str(choice, "label") ?? MaFieldContext.Text(value), Tag = value };
                picker.Items.Add(item);
                if (selected is null && ManifestJson.SameJson(value, current)) selected = item;
            }
        if (selected is null && current is not null)
        {
            // Keep a value the manifest doesn't list visible and unchanged rather than silently picking another.
            selected = new MaPickerItem { Content = MaFieldContext.Text(current) + " (not a listed choice)", Tag = current };
            picker.Items.Add(selected);
        }
        picker.SelectedItem = selected;
        picker.SelectionChanged += (_, _) =>
        {
            if (picker.SelectedItem is MaPickerItem item) context.Write(item.Tag as JsonNode);
        };
        return picker;
    }

    private static FrameworkElement Path(MaFieldContext context)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var box = TextEditor(context, StringValue);
        AutomationProperties.SetName(box, context.Label);
        var browse = new Button { Content = "Browse…" };
        browse.SetResourceReference(FrameworkElement.StyleProperty, "MaTextButton");
        AutomationProperties.SetName(browse, "Browse for " + context.Label);
        Grid.SetColumn(browse, 1);
        var folder = context.Field["pathKind"] is JsonValue kind
                     && (kind.GetValueKind() == JsonValueKind.Number ? kind.ToJsonString() == "1" : string.Equals(MaFieldContext.Text(kind), "Folder", StringComparison.OrdinalIgnoreCase));
        browse.Click += (_, _) =>
        {
            var request = new MaPathRequest(context.FieldName, folder, ManifestJson.Str(context.Field, "pathFilter"), box.Text);
            var chosen = context.Form.PathPicker is { } picker ? picker(request) : ShowDialog(context.Form, request);
            if (chosen is not null) box.Text = chosen;
        };
        grid.Children.Add(box);
        grid.Children.Add(browse);
        return grid;
    }

    private static string? ShowDialog(FrameworkElement owner, MaPathRequest request)
    {
        var start = request.CurrentPath is { Length: > 0 } p ? (Directory.Exists(p) ? p : System.IO.Path.GetDirectoryName(p)) : null;
        var window = Window.GetWindow(owner);
        if (request.IsFolder)
        {
            var dialog = new OpenFolderDialog { InitialDirectory = start ?? "" };
            return dialog.ShowDialog(window) == true ? dialog.FolderName : null;
        }
        var files = new OpenFileDialog { InitialDirectory = start ?? "", CheckFileExists = false };
        if (!string.IsNullOrEmpty(request.Filter))
            try { files.Filter = request.Filter; }
            catch (ArgumentException) { }
        return files.ShowDialog(window) == true ? files.FileName : null;
    }

    private static FrameworkElement Object(MaFieldContext context)
    {
        var fields = ManifestJson.Fields(context.Field).ToList();
        if (fields.Count == 0) return ReadOnly(context);
        var border = new Border { BorderThickness = new Thickness(2, 0, 0, 0), Padding = new Thickness(10, 0, 0, 0) };
        border.SetResourceReference(Border.BorderBrushProperty, "MaChromeBorder");
        border.Child = context.BuildRows(fields, create => create ? context.GetOrCreateObject() : context.Value as JsonObject);
        return border;
    }

    private static FrameworkElement StringList(MaFieldContext context)
    {
        var list = new EntryList(context, "Add item", 1);
        if (context.Value is JsonArray items)
            foreach (var item in items) list.AddEntry(MaFieldContext.Text(item));
        list.Changed = () =>
        {
            var array = context.GetOrCreateArray();
            array.Clear();
            foreach (var entry in list.Entries) array.Add(JsonValue.Create(entry[0]));
            context.NotifyChanged();
        };
        return list;
    }

    private static FrameworkElement Dictionary(MaFieldContext context)
    {
        var list = new EntryList(context, "Add entry", 2);
        if (context.Value is JsonObject pairs)
            foreach (var (key, value) in pairs) list.AddEntry(key, MaFieldContext.Text(value));
        context.Validator = () =>
        {
            var keys = list.Entries.Select(e => e[0].Trim()).ToList();
            if (keys.Any(k => k.Length == 0)) return "Every entry needs a key";
            return keys.GroupBy(k => k, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1) is { } dup ? $"Duplicate key: {dup.Key}" : null;
        };
        list.Changed = () =>
        {
            var obj = context.GetOrCreateObject();
            obj.Clear();
            foreach (var entry in list.Entries)
            {
                var key = entry[0].Trim();
                if (key.Length > 0 && !obj.ContainsKey(key)) obj[key] = JsonValue.Create(entry[1]);
            }
            context.NotifyChanged();
        };
        return list;
    }
}

/// <summary>Rows of one or two text boxes (a string list's items, a dictionary's keys and values) with Remove on each
/// and Add under them.</summary>
internal sealed class EntryList : StackPanel
{
    private readonly MaFieldContext _context;
    private readonly int _columns;
    private readonly StackPanel _rows = new();

    public EntryList(MaFieldContext context, string addText, int columns)
    {
        _context = context;
        _columns = columns;
        Children.Add(_rows);
        var add = new Button { Content = addText, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };
        add.SetResourceReference(StyleProperty, "MaTextButton");
        AutomationProperties.SetName(add, $"{addText} to {context.Label}");
        add.Click += (_, _) =>
        {
            var boxes = AddEntry(new string[_columns]);
            boxes[0].Focus();
            Changed?.Invoke();
        };
        AddButton = add;
        Children.Add(add);
    }

    public Action? Changed { get; set; }

    public Button AddButton { get; }

    public IEnumerable<string[]> Entries =>
        _rows.Children.OfType<Grid>().Select(g => g.Children.OfType<TextBox>().Select(b => b.Text).ToArray());

    public TextBox[] AddEntry(params string?[] values)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var boxes = new TextBox[_columns];
        for (var i = 0; i < _columns; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_columns == 2 && i == 0 ? 2 : 3, GridUnitType.Star) });
            var box = new TextBox { Text = values.ElementAtOrDefault(i) ?? "", Margin = new Thickness(i == 0 ? 0 : 6, 0, 0, 0) };
            box.SetResourceReference(StyleProperty, "MaTextBox");
            if (_columns == 2) MaTextBox.SetPlaceholder(box, i == 0 ? "Key" : "Value");
            AutomationProperties.SetName(box, _columns == 2 ? (i == 0 ? "Key" : "Value") : _context.Label + " item");
            box.TextChanged += (_, _) => Changed?.Invoke();
            Grid.SetColumn(box, i);
            grid.Children.Add(box);
            boxes[i] = box;
        }
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var remove = new Button { Content = "" };
        remove.SetResourceReference(StyleProperty, "MaToolButton");
        AutomationProperties.SetName(remove, "Remove");
        remove.Click += (_, _) =>
        {
            _rows.Children.Remove(grid);
            Changed?.Invoke();
        };
        Grid.SetColumn(remove, _columns);
        grid.Children.Add(remove);
        _rows.Children.Add(grid);
        return boxes;
    }
}
