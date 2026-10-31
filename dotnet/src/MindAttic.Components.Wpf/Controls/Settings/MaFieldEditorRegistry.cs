using System.Windows;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>The field kinds a settings manifest uses (MindAttic.Vault's <c>SettingKind</c>, as JSON strings).</summary>
public static class MaFieldKinds
{
#pragma warning disable CS1591 // each constant is its kind's own name
    public const string Text = "Text", Multiline = "Multiline", Bool = "Bool", Integer = "Integer", Decimal = "Decimal",
        Choice = "Choice", StringList = "StringList", Path = "Path", Url = "Url", TimeSpan = "TimeSpan",
        DateTime = "DateTime", Object = "Object", Collection = "Collection", Dictionary = "Dictionary", Unknown = "Unknown";
#pragma warning restore CS1591

    /// <summary>Every kind in <c>SettingKind</c> order, so a kind written as its number still resolves.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Text, Multiline, Bool, Integer, Decimal, Choice, StringList, Path, Url, TimeSpan, DateTime, Object, Collection, Dictionary, Unknown];
}

/// <summary>Builds the editor for one field. It reads <see cref="MaFieldContext.Value"/>, writes edits back with
/// <see cref="MaFieldContext.Write"/> (or edits <see cref="MaFieldContext.GetOrCreateObject"/> / <see cref="MaFieldContext.GetOrCreateArray"/> in place and calls
/// <see cref="MaFieldContext.NotifyChanged"/>), and may set <see cref="MaFieldContext.Validator"/>.</summary>
public delegate FrameworkElement MaFieldEditorFactory(MaFieldContext context);

/// <summary>The editor for each field kind (M.COMP-T159), case-insensitive. <see cref="CreateDefault"/> holds the built-in
/// editors; a host adds a kind of its own (e.g. Vault.Desktop's credential assignment) with <see cref="Register"/>, or
/// replaces a built-in one. A kind with no editor renders read-only.</summary>
public sealed class MaFieldEditorRegistry
{
    private readonly Dictionary<string, MaFieldEditorFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A registry holding the built-in editor for every <see cref="MaFieldKinds"/> kind.</summary>
    public static MaFieldEditorRegistry CreateDefault()
    {
        var registry = new MaFieldEditorRegistry();
        MaFieldEditors.RegisterBuiltIns(registry);
        return registry;
    }

    /// <summary>The kinds with an editor.</summary>
    public IReadOnlyCollection<string> Kinds => _factories.Keys;

    /// <summary>Sets the editor for <paramref name="kind"/>, replacing any.</summary>
    public void Register(string kind, MaFieldEditorFactory factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        _factories[kind] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>Removes the editor for <paramref name="kind"/>; its fields then render read-only.</summary>
    public bool Remove(string kind) => _factories.Remove(kind);

    /// <summary>The editor for <paramref name="kind"/>, if any.</summary>
    public bool TryGet(string kind, out MaFieldEditorFactory factory) => _factories.TryGetValue(kind, out factory!);
}

/// <summary>Raised by <see cref="MaSettingsForm.FieldChanged"/>: the full name of the field the user changed (a
/// collection item's index in place of <c>*</c>, e.g. <c>MindAttic:Settings:App:profiles:0:name</c>). Never the value.</summary>
public sealed class MaFieldChangedEventArgs(string fieldName) : EventArgs
{
    /// <summary>The changed field's full name.</summary>
    public string FieldName { get; } = fieldName;
}

/// <summary>One invalid field from <see cref="MaSettingsForm.Validate"/>.</summary>
public sealed record MaSettingError(string FieldName, string Message);

/// <summary>What a Path field's Browse button asks <see cref="MaSettingsForm.PathPicker"/> for.</summary>
public sealed record MaPathRequest(string FieldName, bool IsFolder, string? Filter, string? CurrentPath);
