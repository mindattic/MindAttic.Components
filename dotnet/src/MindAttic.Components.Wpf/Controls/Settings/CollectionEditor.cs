using System.Globalization;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>A Collection field's editor: one card per item (titled by the item's <c>itemLabel</c> property) with Edit
/// and Remove, and an Add button that appends a copy of <c>itemDefault</c>. An item's rows are generated, by the same
/// form code, the first time it is opened.</summary>
internal sealed class CollectionEditor : StackPanel
{
    private readonly MaFieldContext _context;
    private readonly List<JsonObject> _fields;
    private readonly StackPanel _items = new();
    private readonly string _itemName;

    public CollectionEditor(MaFieldContext context)
    {
        _context = context;
        _fields = ManifestJson.Fields(context.Field).ToList();
        _itemName = ManifestJson.Str(context.Field, "itemName") is { Length: > 0 } n ? n : "item";
        Children.Add(_items);

        var add = new Button { Content = "Add " + _itemName, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };
        add.SetResourceReference(StyleProperty, "MaTextButton");
        AutomationProperties.SetName(add, $"Add {_itemName} to {context.Label}");
        add.Click += (_, _) => Add();
        AddButton = add;
        Children.Add(add);

        Rebuild();
    }

    public Button AddButton { get; }

    public string FieldName => _context.FieldName;

    /// <summary>Re-reads the item titles after a field inside one of the items changed.</summary>
    public void OnFieldChanged(string fieldName)
    {
        if (fieldName.StartsWith(_context.FieldName + ":", StringComparison.OrdinalIgnoreCase))
            foreach (var card in Cards) card.RefreshTitle();
    }

    public IReadOnlyList<CollectionItemCard> Cards => _items.Children.OfType<CollectionItemCard>().ToList();

    private JsonArray? Array => _context.Value as JsonArray;

    public void Add()
    {
        var array = _context.GetOrCreateArray();
        var itemDefault = _context.Field["itemDefault"];
        array.Add(itemDefault is JsonObject d ? d.DeepClone() : new JsonObject());
        _context.NotifyChanged();
        Rebuild();
        Cards[^1].Open();
    }

    public void Remove(int index)
    {
        var array = _context.GetOrCreateArray();
        if (index < 0 || index >= array.Count) return;
        array.RemoveAt(index);
        _context.NotifyChanged();
        Rebuild();
    }

    /// <summary>Opens the item a full name points into (<c>{this field}:{index}:...</c>); false when it isn't one of
    /// this collection's items or that item is already open.</summary>
    public bool TryOpenItemFor(string fieldName)
    {
        var prefix = _context.FieldName + ":";
        if (!fieldName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var rest = fieldName[prefix.Length..];
        var end = rest.IndexOf(':');
        var segment = end < 0 ? rest : rest[..end];
        if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= Cards.Count) return false;
        var card = Cards[index];
        if (card.IsOpen) return false;
        card.Open();
        return true;
    }

    private void Rebuild()
    {
        _items.Children.Clear();
        var count = Array?.Count ?? 0;
        for (var i = 0; i < count; i++) _items.Children.Add(new CollectionItemCard(this, i));
    }

    internal string Title(int index)
    {
        var label = ManifestJson.Str(_context.Field, "itemLabel");
        if (label is not null && Item(index, false)?[label] is { } node && MaFieldContext.Text(node) is { Length: > 0 } text) return text;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_itemName) + " " + (index + 1);
    }

    internal JsonObject? Item(int index, bool create)
    {
        var array = create ? _context.GetOrCreateArray() : Array;
        if (array is null || index >= array.Count) return null;
        if (array[index] is JsonObject item) return item;
        if (!create) return null;
        var made = new JsonObject();
        array[index] = made;
        return made;
    }

    internal FrameworkElement BuildItem(int index) =>
        Array?[index] is JsonValue scalar
            ? new TextBlock { Text = MaFieldContext.Text(scalar), TextWrapping = TextWrapping.Wrap }
            : _context.BuildRows(_fields, create => Item(index, create), _context.ManifestName + ":*",
                _context.FieldName + ":" + index.ToString(CultureInfo.InvariantCulture));

    /// <summary>One item: its title, Edit/Done and Remove, and its rows once opened.</summary>
    internal sealed class CollectionItemCard : Border
    {
        private readonly CollectionEditor _owner;
        private readonly int _index;
        private readonly TextBlock _title = new() { FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        private readonly StackPanel _panel = new();
        private FrameworkElement? _body;

        public CollectionItemCard(CollectionEditor owner, int index)
        {
            _owner = owner;
            _index = index;
            SetResourceReference(StyleProperty, "MaCard");

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _title.SetResourceReference(TextBlock.ForegroundProperty, "MaText");
            header.Children.Add(_title);

            EditButton = new Button { Content = "Edit" };
            EditButton.SetResourceReference(StyleProperty, "MaTextButton");
            EditButton.Click += (_, _) => { if (IsOpen) Close(); else Open(); };
            Grid.SetColumn(EditButton, 1);
            header.Children.Add(EditButton);

            RemoveButton = new Button { Content = "Remove" };
            RemoveButton.SetResourceReference(StyleProperty, "MaTextButton");
            RemoveButton.Click += (_, _) => _owner.Remove(_index);
            Grid.SetColumn(RemoveButton, 2);
            header.Children.Add(RemoveButton);

            _panel.Children.Add(header);
            Child = _panel;
            RefreshTitle();
        }

        public Button EditButton { get; }
        public Button RemoveButton { get; }
        public string Title => _title.Text;
        public bool IsOpen => _body is { Visibility: Visibility.Visible };

        public void Open()
        {
            if (_body is null)
            {
                _body = _owner.BuildItem(_index);
                _body.Margin = new Thickness(0, 8, 0, 0);
                _panel.Children.Add(_body);
            }
            _body.Visibility = Visibility.Visible;
            EditButton.Content = "Done";
        }

        public void Close()
        {
            if (_body is not null) _body.Visibility = Visibility.Collapsed;
            EditButton.Content = "Edit";
        }

        public void RefreshTitle()
        {
            _title.Text = _owner.Title(_index);
            AutomationProperties.SetName(this, _title.Text);
            AutomationProperties.SetName(EditButton, "Edit " + _title.Text);
            AutomationProperties.SetName(RemoveButton, "Remove " + _title.Text);
        }

        /// <summary>Opens every collapsed item <paramref name="element"/> sits in.</summary>
        public static void OpenAround(DependencyObject element)
        {
            for (var at = LogicalTreeHelper.GetParent(element); at is not null; at = LogicalTreeHelper.GetParent(at))
                if (at is CollectionItemCard card && !card.IsOpen) card.Open();
        }
    }
}
