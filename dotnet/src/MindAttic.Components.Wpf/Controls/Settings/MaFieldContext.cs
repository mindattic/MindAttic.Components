using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// One field being edited by an <see cref="MaSettingsForm"/>: its manifest entry, its full name and where its value
/// lives in the settings document. A missing property reads as the field's default; the first write creates it (and
/// any missing parent object or collection item on the way).
/// </summary>
public sealed class MaFieldContext
{
    private readonly Func<bool, JsonObject?> _parent;

    internal MaFieldContext(MaSettingsForm form, JsonObject field, string fieldName, Func<bool, JsonObject?> parent)
    {
        Form = form;
        Field = field;
        FieldName = fieldName;
        _parent = parent;
        Property = ManifestJson.Str(field, "property") ?? "";
        Kind = ManifestJson.Kind(field);
        Label = ManifestJson.Str(field, "label") is { Length: > 0 } label ? label : Property;
        Help = ManifestJson.Str(field, "help");
        ReadOnly = ManifestJson.Bool(field, "readOnly");
        Nullable = ManifestJson.Bool(field, "nullable");
        Min = ManifestJson.Number(field, "min");
        Max = ManifestJson.Number(field, "max");
        Default = field["default"];
    }

    /// <summary>The form the field is on.</summary>
    public MaSettingsForm Form { get; }

    /// <summary>The field's manifest entry (read it for kind-specific keys a custom editor needs).</summary>
    public JsonObject Field { get; }

    /// <summary>The field's full name, with a collection item's index in place of <c>*</c>.</summary>
    public string FieldName { get; }

    /// <summary>The field's name in the manifest (with <c>*</c> for collection item indexes).</summary>
    public string ManifestName => ManifestJson.Str(Field, "name") ?? FieldName;

    /// <summary>The JSON property name inside its parent object.</summary>
    public string Property { get; }
    /// <summary>The field kind (a <see cref="MaFieldKinds"/> name, or a host's own).</summary>
    public string Kind { get; }
    /// <summary>The row's label (the property name when the manifest has none).</summary>
    public string Label { get; }
    /// <summary>The help text, if any.</summary>
    public string? Help { get; }
    /// <summary>True when the field can't be edited.</summary>
    public bool ReadOnly { get; }
    /// <summary>True when an empty value stores null.</summary>
    public bool Nullable { get; }
    /// <summary>The lowest allowed number, if any.</summary>
    public double? Min { get; }
    /// <summary>The highest allowed number, if any.</summary>
    public double? Max { get; }

    /// <summary>The manifest's default (what the app's settings class starts with).</summary>
    public JsonNode? Default { get; }

    /// <summary>The row the editor sits on (null until it's built).</summary>
    public MaSettingRow? Row { get; internal set; }

    /// <summary>True when the document has no such property (so <see cref="Value"/> is the default).</summary>
    public bool IsMissing => _parent(false) is not { } parent || !parent.ContainsKey(Property);

    /// <summary>The current value: the document's, or <see cref="Default"/> when it's missing. Never modify it; write.</summary>
    public JsonNode? Value => _parent(false) is { } parent && parent.TryGetPropertyValue(Property, out var value) ? value : Default;

    /// <summary>Returns an error message for what the editor holds, or null when it's valid. Set by the editor.</summary>
    public Func<string?>? Validator { get; set; }

    /// <summary>Stores <paramref name="value"/> as the property (creating missing parents) and raises
    /// <see cref="MaSettingsForm.FieldChanged"/>. A node that already has a parent is cloned.</summary>
    public void Write(JsonNode? value)
    {
        if (ReadOnly) return;
        var parent = _parent(true) ?? throw new InvalidOperationException($"No parent object for {FieldName}.");
        parent[Property] = value?.Parent is null ? value : value.DeepClone();
        NotifyChanged();
    }

    /// <summary>The property's object in the document to edit in place, created (from a clone of the default when that
    /// is an object) when it is missing or not an object. Call <see cref="NotifyChanged"/> after editing it.</summary>
    public JsonObject GetOrCreateObject() => GetOrCreate(() => new JsonObject());

    /// <summary>The property's array in the document to edit in place, created (from a clone of the default when that
    /// is an array) when it is missing or not an array. Call <see cref="NotifyChanged"/> after editing it.</summary>
    public JsonArray GetOrCreateArray() => GetOrCreate(() => new JsonArray());

    private T GetOrCreate<T>(Func<T> create) where T : JsonNode
    {
        var parent = _parent(true) ?? throw new InvalidOperationException($"No parent object for {FieldName}.");
        if (parent[Property] is T existing) return existing;
        var made = Default is T d ? (T)d.DeepClone() : create();
        parent[Property] = made;
        return made;
    }

    /// <summary>Raises <see cref="MaSettingsForm.FieldChanged"/> for this field and refreshes a shown error.</summary>
    public void NotifyChanged()
    {
        Revalidate();
        Form.RaiseFieldChanged(FieldName);
    }

    /// <summary>Re-checks the row when it is showing an error, so fixing the value clears it.</summary>
    public void Revalidate()
    {
        if (Row is { Error: not null } row) row.Error = Validator?.Invoke();
    }

    /// <summary>Builds the rows for nested <paramref name="fields"/> (an Object field's, or a collection item's) whose
    /// values live in the object <paramref name="parent"/> returns (created when called with true). Nested names are
    /// this field's manifest name, or <paramref name="manifestPrefix"/>, re-rooted at <paramref name="namePrefix"/>.</summary>
    public FrameworkElement BuildRows(IEnumerable<JsonObject> fields, Func<bool, JsonObject?> parent, string? manifestPrefix = null, string? namePrefix = null) =>
        Form.BuildRows(fields, parent, manifestPrefix ?? ManifestName, namePrefix ?? FieldName, nested: true);

    /// <summary>A JSON value as editable text: a string's own text, anything else as JSON, null as "".</summary>
    public static string Text(JsonNode? node) => node switch
    {
        null => "",
        JsonValue v when v.GetValueKind() == JsonValueKind.String => v.GetValue<string>(),
        _ => node.ToJsonString(),
    };
}

/// <summary>Reads the camelCase manifest JSON loosely: a wrong type reads as absent, never throws.</summary>
internal static class ManifestJson
{
    public static string? Str(JsonObject o, string key) =>
        o.TryGetPropertyValue(key, out var n) && n is JsonValue v && v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : null;

    public static bool Bool(JsonObject o, string key) =>
        o.TryGetPropertyValue(key, out var n) && n is JsonValue v && v.GetValueKind() == JsonValueKind.True;

    public static double? Number(JsonObject o, string key) =>
        o.TryGetPropertyValue(key, out var n) && n is JsonValue v && v.GetValueKind() == JsonValueKind.Number
        && double.TryParse(v.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    public static int Order(JsonObject o) => Number(o, "order") is { } d && d is >= int.MinValue and <= int.MaxValue ? (int)d : int.MaxValue;

    public static string Kind(JsonObject o)
    {
        if (Str(o, "kind") is { Length: > 0 } kind) return kind;
        if (Number(o, "kind") is { } i && i >= 0 && i < MaFieldKinds.All.Count && i == Math.Floor(i)) return MaFieldKinds.All[(int)i];
        return MaFieldKinds.Unknown;
    }

    public static IEnumerable<JsonObject> Fields(JsonObject o, string key = "fields") =>
        o.TryGetPropertyValue(key, out var n) && n is JsonArray a ? a.OfType<JsonObject>() : [];

    public static bool SameJson(JsonNode? a, JsonNode? b) =>
        JsonNode.DeepEquals(a, b) || (a?.ToJsonString() ?? "null") == (b?.ToJsonString() ?? "null");
}
