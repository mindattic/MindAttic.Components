using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T159: MaSettingsForm renders any app's settings manifest as rows and edits the settings JSON in place.</summary>
public sealed class MaSettingsFormTests
{
    private const string P = "MindAttic:Settings:SampleApp";

    // The shape MindAttic.Vault's SettingsManifestBuilder writes for SettingsManifestTests' SampleSettings.
    private const string ManifestJson = """
    {
      "schemaVersion": 1, "appId": "SampleApp", "displayName": "Sample App", "icon": "cog", "settingsFile": "settings.json",
      "configurationPrefix": "MindAttic:Settings:SampleApp",
      "fields": [
        { "name": "MindAttic:Settings:SampleApp:darkTheme", "property": "darkTheme", "label": "Dark theme", "group": "General", "order": 100002, "kind": "Bool", "readOnly": false, "nullable": false, "default": true },
        { "name": "MindAttic:Settings:SampleApp:maxLossPerTrade", "property": "maxLossPerTrade", "label": "Max loss per trade", "group": "Risk", "help": "Dollars", "order": 1, "kind": "Decimal", "readOnly": false, "nullable": false, "default": 50, "min": 0, "max": 10000 },
        { "name": "MindAttic:Settings:SampleApp:mode", "property": "mode", "label": "Mode", "group": "Risk", "order": 2, "kind": "Choice", "readOnly": false, "nullable": false, "default": 0,
          "choices": [ { "value": 0, "label": "Paper" }, { "value": 1, "label": "Live" } ] },
        { "name": "MindAttic:Settings:SampleApp:retryCount", "property": "retryCount", "label": "Retry count", "group": "General", "order": 100003, "kind": "Integer", "readOnly": false, "nullable": false, "default": 3, "min": 0, "max": 10 },
        { "name": "MindAttic:Settings:SampleApp:dataFolder", "property": "dataFolder", "label": "Data folder", "group": "General", "order": 100004, "kind": "Path", "readOnly": false, "nullable": true, "default": "C:\\Data", "pathKind": "Folder" },
        { "name": "MindAttic:Settings:SampleApp:exe", "property": "exe", "label": "Exe", "group": "General", "order": 100005, "kind": "Path", "readOnly": false, "nullable": true, "default": "", "pathKind": 0, "pathFilter": "Executables|*.exe" },
        { "name": "MindAttic:Settings:SampleApp:endpoint", "property": "endpoint", "label": "Endpoint", "group": "General", "order": 100006, "kind": "Url", "readOnly": false, "nullable": true, "default": "https://example.com" },
        { "name": "MindAttic:Settings:SampleApp:notes", "property": "notes", "label": "Notes", "group": "General", "order": 100007, "kind": "Multiline", "readOnly": false, "nullable": true, "default": "first line" },
        { "name": "MindAttic:Settings:SampleApp:size", "property": "size", "label": "Size", "group": "General", "order": 100008, "kind": "Choice", "readOnly": false, "nullable": true, "default": "small",
          "choices": [ { "value": "small", "label": "small" }, { "value": "large", "label": "large" } ] },
        { "name": "MindAttic:Settings:SampleApp:pollInterval", "property": "pollInterval", "label": "Poll interval", "group": "General", "order": 100009, "kind": "TimeSpan", "readOnly": false, "nullable": false, "default": "00:00:30" },
        { "name": "MindAttic:Settings:SampleApp:startAt", "property": "startAt", "label": "Start at", "group": "General", "order": 100010, "kind": "DateTime", "readOnly": false, "nullable": false, "default": "2026-01-01T00:00:00" },
        { "name": "MindAttic:Settings:SampleApp:symbols", "property": "symbols", "label": "Symbols", "group": "General", "order": 100011, "kind": "StringList", "readOnly": false, "nullable": true, "default": [ "SPY" ] },
        { "name": "MindAttic:Settings:SampleApp:aliases", "property": "aliases", "label": "Aliases", "group": "General", "order": 100012, "kind": "Dictionary", "readOnly": false, "nullable": true, "default": { "es": "ES=F" } },
        { "name": "MindAttic:Settings:SampleApp:server", "property": "server", "label": "Server", "group": "General", "order": 100013, "kind": "Object", "readOnly": false, "nullable": true, "default": { "port": 8080 },
          "fields": [ { "name": "MindAttic:Settings:SampleApp:server:port", "property": "port", "label": "Port", "group": "General", "order": 100000, "kind": "Integer", "readOnly": false, "nullable": false, "default": 8080, "min": 1, "max": 65535 } ] },
        { "name": "MindAttic:Settings:SampleApp:profiles", "property": "profiles", "label": "Profiles", "group": "General", "order": 100014, "kind": "Collection", "readOnly": false, "nullable": true, "default": [],
          "itemLabel": "name", "itemName": "profile", "itemDefault": { "name": "New profile", "enabled": true },
          "fields": [
            { "name": "MindAttic:Settings:SampleApp:profiles:*:name", "property": "name", "label": "Profile name", "group": "General", "order": 100000, "kind": "Text", "readOnly": false, "nullable": true, "default": "New profile" },
            { "name": "MindAttic:Settings:SampleApp:profiles:*:enabled", "property": "enabled", "label": "Enabled", "group": "General", "order": 100001, "kind": "Bool", "readOnly": false, "nullable": false, "default": true } ] },
        { "name": "MindAttic:Settings:SampleApp:connectionString", "property": "connectionString", "label": "Local db", "group": "General", "order": 100015, "kind": "Text", "readOnly": false, "nullable": true, "default": "Data Source=app.db" },
        { "name": "MindAttic:Settings:SampleApp:blob", "property": "blob", "label": "Blob", "group": "General", "order": 100016, "kind": "Unknown", "readOnly": true, "nullable": true, "default": { "a": 1 } }
      ],
      "credentials": [ { "providerId": "claude", "displayName": "Claude", "required": true, "purpose": "Writes summaries" } ]
    }
    """;

    private sealed class Ctx(StaHost host, MaSettingsForm form, JsonObject doc, List<string> changed)
    {
        public StaHost H { get; } = host;
        public MaSettingsForm Form { get; } = form;
        public JsonObject Doc { get; } = doc;
        public List<string> Changed { get; } = changed;
        public MaSettingRow Row(string property) => Form.FindRow(P + ":" + property) ?? throw new InvalidOperationException("no row " + property);
        public T Editor<T>(string property) where T : FrameworkElement => (T)Row(property).Editor;
        public string Json(string property) => Doc[property]?.ToJsonString() ?? "<missing>";
    }

    private static void Run(Action<Ctx> test, string doc = "{}", string manifest = ManifestJson, Action<MaSettingsForm>? setup = null, double formHeight = double.NaN) =>
        StaHost.Run(h =>
        {
            var form = new MaSettingsForm { Height = formHeight };
            setup?.Invoke(form);
            var document = JsonNode.Parse(doc)!.AsObject();
            form.Load(JsonNode.Parse(manifest)!.AsObject(), document);
            var changed = new List<string>();
            form.FieldChanged += (_, e) => changed.Add(e.FieldName);
            h.Show(form);
            test(new Ctx(h, form, document, changed));
        }, 700, 600);

    // ---- layout ----

    [Fact]
    public void One_row_per_field_grouped_under_headings_and_sorted_by_order() => Run(c =>
    {
        var top = ((Panel)((ScrollViewer)c.Form.ScrollViewer).Content).Children.OfType<Panel>().Single().Children;
        var sequence = top.OfType<FrameworkElement>()
            .Select(e => e is TextBlock t ? "#" + AutomationProperties.GetName(t) : ((MaSettingRow)e).Context.Property).ToList();
        Assert.Equal(["#Risk", "maxLossPerTrade", "mode", "#General", "darkTheme", "retryCount", "dataFolder", "exe", "endpoint", "notes",
            "size", "pollInterval", "startAt", "symbols", "aliases", "server", "profiles", "connectionString", "blob"], sequence);
        Assert.All(top.OfType<MaSettingRow>(), r => Assert.Equal(HorizontalAlignment.Stretch, r.HorizontalAlignment));
        Assert.Empty(c.Changed);
        Assert.Equal("{}", c.Doc.ToJsonString());
    });

    [Fact]
    public void A_row_shows_the_label_the_copyable_full_name_and_the_help() => Run(c =>
    {
        var row = c.Row("maxLossPerTrade");
        var texts = StaHost.Descendants<TextBlock>(row).Select(t => t.Text).ToList();
        Assert.Contains("Max loss per trade", texts);
        Assert.Contains("Dollars", texts);
        Assert.Equal(P + ":maxLossPerTrade", row.NameBox.Text);
        Assert.True(row.NameBox.IsReadOnly);
        row.NameBox.SelectAll();
        Assert.Equal(P + ":maxLossPerTrade", row.NameBox.SelectedText);
        Assert.Equal("Max loss per trade", AutomationProperties.GetName(row.Editor));
        Assert.Equal("Dollars", AutomationProperties.GetHelpText(row.Editor));
    });

    // ---- each kind: default shown when missing, edit writes the right JSON ----

    [Fact]
    public void Text_shows_its_default_and_writes_a_string() => Run(c =>
    {
        var box = c.Editor<TextBox>("connectionString");
        Assert.Equal("Data Source=app.db", box.Text);
        box.Text = "Data Source=other.db";
        Assert.Equal("\"Data Source=other.db\"", c.Json("connectionString"));
        Assert.Equal([P + ":connectionString"], c.Changed);
    });

    [Fact]
    public void A_value_in_the_document_wins_over_the_default() => Run(c =>
    {
        Assert.Equal("mine.db", c.Editor<TextBox>("connectionString").Text);
        Assert.Equal("7", c.Editor<TextBox>("retryCount").Text);
        Assert.False(c.Editor<CheckBox>("darkTheme").IsChecked);
    }, """{ "connectionString": "mine.db", "retryCount": 7, "darkTheme": false }""");

    [Fact]
    public void Multiline_is_a_text_area_and_writes_a_string() => Run(c =>
    {
        var box = c.Editor<TextBox>("notes");
        Assert.Same(c.H.Window.FindResource("MaTextArea"), box.Style);
        Assert.Equal("first line", box.Text);
        box.Text = "a\nb";
        Assert.Equal("a\nb", c.Doc["notes"]!.GetValue<string>());
    });

    [Fact]
    public void Bool_is_an_MaCheck_and_writes_true_or_false() => Run(c =>
    {
        var check = c.Editor<CheckBox>("darkTheme");
        Assert.Same(c.H.Window.FindResource("MaCheck"), check.Style);
        Assert.True(check.IsChecked);
        check.IsChecked = false;
        Assert.Equal("false", c.Json("darkTheme"));
        check.IsChecked = true;
        Assert.Equal("true", c.Json("darkTheme"));
        Assert.Equal(2, c.Changed.Count);
    });

    [Fact]
    public void Integer_parses_invariantly_and_writes_a_number() => Run(c =>
    {
        var box = c.Editor<TextBox>("retryCount");
        Assert.Equal("3", box.Text);
        box.Text = "5";
        Assert.Equal(JsonValueKind.Number, c.Doc["retryCount"]!.GetValueKind());
        Assert.Equal("5", c.Json("retryCount"));
    });

    [Fact]
    public void Decimal_parses_invariantly_and_writes_a_number() => Run(c =>
    {
        var box = c.Editor<TextBox>("maxLossPerTrade");
        Assert.Equal("50", box.Text);
        box.Text = "12.75";
        Assert.Equal("12.75", c.Json("maxLossPerTrade"));
        Assert.Equal(JsonValueKind.Number, c.Doc["maxLossPerTrade"]!.GetValueKind());
    });

    [Fact]
    public void Invalid_numbers_are_not_written_and_show_on_Validate() => Run(c =>
    {
        c.Editor<TextBox>("retryCount").Text = "five";
        c.Editor<TextBox>("maxLossPerTrade").Text = "12,5";
        Assert.Equal("<missing>", c.Json("retryCount"));
        Assert.Equal("<missing>", c.Json("maxLossPerTrade"));
        Assert.Empty(c.Changed);

        var errors = c.Form.Validate();
        Assert.Contains(new MaSettingError(P + ":retryCount", "Enter a whole number"), errors);
        Assert.Contains(new MaSettingError(P + ":maxLossPerTrade", "Enter a number (use . for decimals)"), errors);
        Assert.Equal("Enter a whole number", c.Row("retryCount").Error);
    });

    [Fact]
    public void Choice_of_a_numeric_enum_writes_back_the_number() => Run(c =>
    {
        var picker = c.Editor<MaPicker>("mode");
        Assert.Equal("Paper", ((MaPickerItem)picker.SelectedItem).Content);
        picker.SelectedIndex = 1;
        Assert.Equal("1", c.Json("mode"));
        Assert.Equal(JsonValueKind.Number, c.Doc["mode"]!.GetValueKind());
        Assert.Equal([P + ":mode"], c.Changed);
    });

    [Fact]
    public void Choice_selects_the_documents_value_and_writes_a_string_choice() => Run(c =>
    {
        Assert.Equal("Live", ((MaPickerItem)c.Editor<MaPicker>("mode").SelectedItem).Content);
        var size = c.Editor<MaPicker>("size");
        Assert.Equal("small", ((MaPickerItem)size.SelectedItem).Content);
        size.SelectedIndex = 1;
        Assert.Equal("\"large\"", c.Json("size"));
    }, """{ "mode": 1 }""");

    [Fact]
    public void Choice_keeps_an_unlisted_value_visible_and_unchanged() => Run(c =>
    {
        var picker = c.Editor<MaPicker>("mode");
        Assert.Equal(3, picker.Items.Count);
        Assert.Contains("7", (string)((MaPickerItem)picker.SelectedItem).Content);
        Assert.Equal("7", c.Json("mode"));
    }, """{ "mode": 7 }""");

    [Fact]
    public void Url_writes_a_valid_url_and_rejects_anything_else() => Run(c =>
    {
        var box = c.Editor<TextBox>("endpoint");
        Assert.Equal("https://example.com", box.Text);
        box.Text = "https://api.example.com/v1";
        Assert.Equal("\"https://api.example.com/v1\"", c.Json("endpoint"));
        box.Text = "not a url";
        Assert.Equal("\"https://api.example.com/v1\"", c.Json("endpoint"));
        Assert.Equal([new MaSettingError(P + ":endpoint", "Enter a full URL, e.g. https://example.com")], c.Form.Validate());
    });

    [Fact]
    public void TimeSpan_shows_and_writes_the_hh_mm_ss_string_System_Text_Json_uses() => Run(c =>
    {
        var box = c.Editor<TextBox>("pollInterval");
        Assert.Equal("00:00:30", box.Text);
        box.Text = "0:5:0";
        Assert.Equal("\"00:05:00\"", c.Json("pollInterval"));
        box.Text = "1.02:00:00";
        Assert.Equal("\"1.02:00:00\"", c.Json("pollInterval"));
        Assert.Equal(TimeSpan.FromHours(26), JsonSerializer.Deserialize<TimeSpan>(c.Json("pollInterval")));
        box.Text = "30";
        Assert.Single(c.Form.Validate());
    });

    [Fact]
    public void DateTime_shows_and_writes_an_ISO_date() => Run(c =>
    {
        var box = c.Editor<TextBox>("startAt");
        Assert.Equal("2026-01-01T00:00:00", box.Text);
        box.Text = "2026-10-31T09:30:00";
        Assert.Equal(new DateTime(2026, 10, 31, 9, 30, 0), JsonSerializer.Deserialize<DateTime>(c.Json("startAt")));
        box.Text = "2026-10-31T09:30:00+02:00";
        Assert.Equal(new DateTimeOffset(2026, 10, 31, 9, 30, 0, TimeSpan.FromHours(2)), JsonSerializer.Deserialize<DateTimeOffset>(c.Json("startAt")));
        box.Text = "someday";
        Assert.Equal("Enter a date, e.g. 2026-10-31T09:00:00", c.Form.Validate().Single().Message);
    });

    [Fact]
    public void Path_writes_typed_text_and_Browse_asks_the_picker_for_a_folder_or_a_filtered_file() => Run(c =>
    {
        var requests = new List<MaPathRequest>();
        c.Form.PathPicker = r => { requests.Add(r); return r.IsFolder ? @"D:\Data" : null; };

        var folder = c.Editor<Grid>("dataFolder");
        var box = folder.Children.OfType<TextBox>().Single();
        Assert.Equal(@"C:\Data", box.Text);
        c.H.Click(folder.Children.OfType<Button>().Single());
        Assert.Equal(new MaPathRequest(P + ":dataFolder", true, null, @"C:\Data"), requests[0]);
        Assert.Equal(@"D:\Data", c.Doc["dataFolder"]!.GetValue<string>());

        var exe = c.Editor<Grid>("exe");
        c.H.Click(exe.Children.OfType<Button>().Single());
        Assert.Equal(new MaPathRequest(P + ":exe", false, "Executables|*.exe", ""), requests[1]);
        Assert.Equal("<missing>", c.Json("exe"));
        exe.Children.OfType<TextBox>().Single().Text = @"C:\tools\x.exe";
        Assert.Equal(@"C:\tools\x.exe", c.Doc["exe"]!.GetValue<string>());
    });

    [Fact]
    public void StringList_adds_edits_and_removes_items() => Run(c =>
    {
        var list = c.Row("symbols").Editor;
        TextBox[] Boxes() => StaHost.Descendants<TextBox>(list).ToArray();
        Assert.Equal(["SPY"], Boxes().Select(b => b.Text));

        c.H.Click(StaHost.Descendants<Button>(list).Single(b => (string)b.Content == "Add item"));
        Boxes()[1].Text = "QQQ";
        Assert.Equal("""["SPY","QQQ"]""", c.Json("symbols"));

        Boxes()[0].Text = "IWM";
        Assert.Equal("""["IWM","QQQ"]""", c.Json("symbols"));

        c.H.Click(StaHost.Descendants<Button>(list).First(b => AutomationProperties.GetName(b) == "Remove"));
        Assert.Equal("""["QQQ"]""", c.Json("symbols"));
        Assert.All(c.Changed, n => Assert.Equal(P + ":symbols", n));
    });

    [Fact]
    public void Dictionary_adds_edits_and_removes_pairs_and_flags_duplicate_or_empty_keys() => Run(c =>
    {
        var editor = c.Row("aliases").Editor;
        TextBox[] Boxes() => StaHost.Descendants<TextBox>(editor).ToArray();
        Assert.Equal(["es", "ES=F"], Boxes().Select(b => b.Text));

        c.H.Click(StaHost.Descendants<Button>(editor).Single(b => (string)b.Content == "Add entry"));
        Assert.Equal("Every entry needs a key", c.Form.Validate().Single().Message);
        Boxes()[2].Text = "nq";
        Boxes()[3].Text = "NQ=F";
        Assert.Equal("""{"es":"ES=F","nq":"NQ=F"}""", c.Json("aliases"));
        Assert.Null(c.Row("aliases").Error);

        Boxes()[2].Text = "es";
        Assert.Equal("Duplicate key: es", c.Form.Validate().Single().Message);
        Boxes()[2].Text = "ym";
        Assert.Null(c.Row("aliases").Error);
        Assert.Equal("""{"es":"ES=F","ym":"NQ=F"}""", c.Json("aliases"));

        c.H.Click(StaHost.Descendants<Button>(editor).First(b => AutomationProperties.GetName(b) == "Remove"));
        Assert.Equal("""{"ym":"NQ=F"}""", c.Json("aliases"));
    });

    [Fact]
    public void Object_renders_nested_rows_and_creates_the_object_from_its_default_on_first_edit() => Run(c =>
    {
        var port = c.Form.FindRow(P + ":server:port")!;
        var box = (TextBox)port.Editor;
        Assert.Equal("8080", box.Text);
        Assert.Equal("<missing>", c.Json("server"));

        box.Text = "9090";
        Assert.Equal("""{"port":9090}""", c.Json("server"));
        Assert.Equal([P + ":server:port"], c.Changed);

        box.Text = "70000";
        Assert.Equal("Must be between 1 and 65535", c.Form.Validate().Single().Message);
    });

    [Fact]
    public void Object_edits_an_existing_nested_object_in_place() => Run(c =>
    {
        var server = c.Doc["server"];
        ((TextBox)c.Form.FindRow(P + ":server:port")!.Editor).Text = "81";
        Assert.Same(server, c.Doc["server"]);
        Assert.Equal("""{"port":81,"host":"x"}""", c.Json("server"));
    }, """{ "server": { "port": 80, "host": "x" } }""");

    [Fact]
    public void Collection_lists_items_titled_by_their_label_and_edits_an_items_fields() => Run(c =>
    {
        var editor = c.Row("profiles").Editor;
        var titles = StaHost.Descendants<TextBlock>(editor).Where(t => t.FontWeight == FontWeights.SemiBold).Select(t => t.Text).ToList();
        Assert.Equal(["Alpha", "Profile 2"], titles);
        Assert.Null(c.Form.FindRow(P + ":profiles:0:name"));

        c.H.Click(StaHost.Descendants<Button>(editor).First(b => AutomationProperties.GetName(b) == "Edit Alpha"));
        var name = c.Form.FindRow(P + ":profiles:0:name")!;
        Assert.Equal("Alpha", ((TextBox)name.Editor).Text);
        ((TextBox)name.Editor).Text = "Beta";
        Assert.Equal("Beta", c.Doc["profiles"]![0]!["name"]!.GetValue<string>());
        Assert.Equal([P + ":profiles:0:name"], c.Changed);
        Assert.Contains(StaHost.Descendants<TextBlock>(editor), t => t.Text == "Beta");

        c.H.Click(StaHost.Descendants<Button>(editor).First(b => AutomationProperties.GetName(b) == "Edit Profile 2"));
        var enabled = c.Form.FindRow(P + ":profiles:1:enabled")!;
        Assert.True(((CheckBox)enabled.Editor).IsChecked); // missing in the item: the field default
        ((CheckBox)enabled.Editor).IsChecked = false;
        Assert.Equal("""[{"name":"Beta","enabled":true},{"enabled":false}]""", c.Json("profiles"));
    }, """{ "profiles": [ { "name": "Alpha", "enabled": true }, {} ] }""");

    [Fact]
    public void Collection_add_appends_a_copy_of_itemDefault_and_opens_it() => Run(c =>
    {
        var editor = c.Row("profiles").Editor;
        c.H.Click(StaHost.Descendants<Button>(editor).Single(b => (string)b.Content == "Add profile"));
        Assert.Equal("""[{"name":"New profile","enabled":true}]""", c.Json("profiles"));
        Assert.Equal([P + ":profiles"], c.Changed);
        var name = c.Form.FindRow(P + ":profiles:0:name")!;
        Assert.True(name.IsVisible);

        ((TextBox)name.Editor).Text = "Swing";
        c.H.Click(StaHost.Descendants<Button>(editor).Single(b => (string)b.Content == "Add profile"));
        Assert.Equal("""[{"name":"Swing","enabled":true},{"name":"New profile","enabled":true}]""", c.Json("profiles"));
    });

    [Fact]
    public void Collection_remove_drops_the_item_and_renumbers_the_rest() => Run(c =>
    {
        var editor = c.Row("profiles").Editor;
        c.H.Click(StaHost.Descendants<Button>(editor).Single(b => AutomationProperties.GetName(b) == "Remove A"));
        Assert.Equal("""[{"name":"B"},{"name":"C"}]""", c.Json("profiles"));
        Assert.Equal([P + ":profiles"], c.Changed);

        c.H.Click(StaHost.Descendants<Button>(editor).Single(b => AutomationProperties.GetName(b) == "Edit C"));
        ((TextBox)c.Form.FindRow(P + ":profiles:1:name")!.Editor).Text = "C2";
        Assert.Equal("""[{"name":"B"},{"name":"C2"}]""", c.Json("profiles"));
    }, """{ "profiles": [ { "name": "A" }, { "name": "B" }, { "name": "C" } ] }""");

    [Fact]
    public void Unknown_kinds_render_read_only_and_never_write() => Run(c =>
    {
        var box = c.Editor<TextBox>("blob");
        Assert.True(box.IsReadOnly);
        Assert.Equal("""{"a":1}""", box.Text);
        box.Text = "changed";
        Assert.Equal("<missing>", c.Json("blob"));
        Assert.Empty(c.Changed);
    });

    [Fact]
    public void A_malformed_manifest_never_crashes() => Run(c =>
    {
        Assert.True(c.Editor<TextBox>("future").IsReadOnly);
        Assert.Equal("""[1,2]""", c.Editor<TextBox>("future").Text);
        Assert.True(c.Editor<TextBox>("noKind").IsReadOnly);
        Assert.IsType<CheckBox>(c.Editor<FrameworkElement>("numericKind"));
        Assert.True(c.Editor<TextBox>("emptyObject").IsReadOnly);
        Assert.Equal(4, c.Form.Rows.Count());
    }, """{ "future": [1,2] }""", """
    { "configurationPrefix": "MindAttic:Settings:SampleApp",
      "fields": [ 42, "x", null,
        { "name": "MindAttic:Settings:SampleApp:future", "property": "future", "label": "Future", "kind": "Matrix" },
        { "name": "MindAttic:Settings:SampleApp:noKind", "property": "noKind" },
        { "name": "MindAttic:Settings:SampleApp:numericKind", "property": "numericKind", "kind": 2, "order": "first", "min": "low" },
        { "name": "MindAttic:Settings:SampleApp:emptyObject", "property": "emptyObject", "kind": "Object", "fields": "none" } ] }
    """);

    [Fact]
    public void An_editor_that_throws_falls_back_to_read_only() => Run(c =>
    {
        Assert.True(c.Editor<TextBox>("connectionString").IsReadOnly);
    }, setup: f => f.Editors.Register(MaFieldKinds.Text, _ => throw new InvalidOperationException("broken editor")));

    [Fact]
    public void ReadOnly_fields_cannot_be_edited() => Run(c =>
    {
        Assert.True(c.Editor<TextBox>("connectionString").IsReadOnly);
        Assert.False(c.Editor<CheckBox>("darkTheme").IsEnabled);
    }, manifest: ManifestJson.Replace("\"label\": \"Local db\", \"group\": \"General\", \"order\": 100015, \"kind\": \"Text\", \"readOnly\": false",
        "\"label\": \"Local db\", \"group\": \"General\", \"order\": 100015, \"kind\": \"Text\", \"readOnly\": true")
        .Replace("\"kind\": \"Bool\", \"readOnly\": false, \"nullable\": false, \"default\": true }", "\"kind\": \"Bool\", \"readOnly\": true, \"nullable\": false, \"default\": true }"));

    [Fact]
    public void A_host_registers_an_editor_for_a_kind_of_its_own() => Run(c =>
    {
        var row = c.Form.FindRow(P + ":keys:claude")!;
        var button = (Button)row.Editor;
        c.H.Click(button);
        Assert.Equal("""["key1","2026-10-31 IdiotProof"]""", c.Doc["keys"]!.ToJsonString());
        Assert.Equal([P + ":keys:claude"], c.Changed);
    }, manifest: """
    { "configurationPrefix": "MindAttic:Settings:SampleApp",
      "fields": [ { "name": "MindAttic:Settings:SampleApp:keys:claude", "property": "keys", "label": "Claude keys", "kind": "CredentialAssignment" } ] }
    """, setup: f => f.Editors.Register("CredentialAssignment", ctx =>
    {
        var button = new Button { Content = "Assign" };
        button.Click += (_, _) => ctx.Write(new JsonArray("key1", "2026-10-31 IdiotProof"));
        return button;
    }));

    // ---- validation ----

    [Fact]
    public void Validate_reports_range_required_and_parse_errors_and_fixing_a_value_clears_its_row() => Run(c =>
    {
        var retry = c.Editor<TextBox>("retryCount");
        var loss = c.Editor<TextBox>("maxLossPerTrade");
        retry.Text = "11";
        loss.Text = "";
        c.Editor<TextBox>("pollInterval").Text = "soon";

        var errors = c.Form.Validate();
        Assert.Equal(3, errors.Count);
        Assert.Contains(new MaSettingError(P + ":retryCount", "Must be between 0 and 10"), errors);
        Assert.Contains(new MaSettingError(P + ":maxLossPerTrade", "Required: enter a number"), errors);
        Assert.Equal("Must be between 0 and 10", c.Row("retryCount").Error);
        Assert.Equal("<missing>", c.Json("retryCount"));

        retry.Text = "10";
        Assert.Null(c.Row("retryCount").Error);
        Assert.Equal("10", c.Json("retryCount"));
        Assert.NotNull(c.Row("maxLossPerTrade").Error);

        c.Form.ClearErrors();
        Assert.All(c.Form.Rows, r => Assert.Null(r.Error));
        Assert.Equal(2, c.Form.Validate(show: false).Count);
        Assert.All(c.Form.Rows, r => Assert.Null(r.Error));
    });

    [Fact]
    public void A_clean_form_validates_with_no_errors() => Run(c => Assert.Empty(c.Form.Validate()));

    // ---- deep links ----

    [Fact]
    public void ScrollTo_brings_the_row_into_view_and_highlights_it_briefly() => Run(c =>
    {
        c.Form.HighlightDuration = TimeSpan.FromMilliseconds(150);
        Assert.Equal(0, c.Form.ScrollViewer.VerticalOffset);
        Assert.True(c.Form.ScrollTo(P + ":connectionString"));
        c.H.Pump();
        var row = c.Row("connectionString");
        Assert.True(c.Form.ScrollViewer.VerticalOffset > 0);
        var top = row.TranslatePoint(new Point(0, 0), c.Form.ScrollViewer).Y;
        Assert.InRange(top, -1, c.Form.ScrollViewer.ViewportHeight);
        Assert.True(row.IsHighlighted);
        Assert.Same(c.H.Window.FindResource("MaHover"), row.Background);

        Assert.True(c.H.PumpUntil(() => !row.IsHighlighted));
        Assert.False(c.Form.ScrollTo(P + ":nope"));
        Assert.False(c.Form.ScrollTo(""));
    }, formHeight: 240);

    [Fact]
    public void ScrollTo_is_case_insensitive_and_opens_the_collection_item_a_field_is_in() => Run(c =>
    {
        Assert.Null(c.Form.FindRow(P + ":profiles:1:enabled"));
        Assert.True(c.Form.ScrollTo((P + ":profiles:1:enabled").ToUpperInvariant()));
        c.H.Pump();
        var row = c.Form.FindRow(P + ":profiles:1:enabled")!;
        Assert.True(row.IsVisible);
        Assert.True(row.IsHighlighted);
        Assert.Null(c.Form.FindRow(P + ":profiles:0:enabled"));
    }, """{ "profiles": [ { "name": "A" }, { "name": "B" } ] }""", formHeight: 240);

    [Fact]
    public void ScrollTo_an_item_name_with_a_star_or_a_missing_index_goes_to_the_collection_row() => Run(c =>
    {
        Assert.True(c.Form.ScrollTo(P + ":profiles:*:name"));
        Assert.True(c.Row("profiles").IsHighlighted);
        Assert.True(c.Form.ScrollTo(P + ":profiles:9:name"));
    }, """{ "profiles": [ { "name": "A" } ] }""", formHeight: 240);

    [Fact]
    public void ScrollTo_reopens_a_closed_item_whose_rows_were_built() => Run(c =>
    {
        var editor = c.Row("profiles").Editor;
        var edit = StaHost.Descendants<Button>(editor).Single(b => AutomationProperties.GetName(b) == "Edit A");
        c.H.Click(edit);
        c.H.Click(edit);
        var row = c.Form.FindRow(P + ":profiles:0:name")!;
        Assert.False(row.IsVisible);
        Assert.True(c.Form.ScrollTo(P + ":profiles:0:name"));
        c.H.Pump();
        Assert.True(row.IsVisible);
    }, """{ "profiles": [ { "name": "A" } ] }""");

    [Fact]
    public void Reloading_replaces_the_rows() => Run(c =>
    {
        var other = new JsonObject { ["retryCount"] = 9 };
        c.Form.Load(JsonNode.Parse(ManifestJson)!.AsObject(), other);
        c.H.Pump();
        Assert.Equal("9", ((TextBox)c.Form.FindRow(P + ":retryCount")!.Editor).Text);
        Assert.Same(other, c.Form.Document);
        Assert.Single(c.Form.Rows, r => r.FieldName == P + ":retryCount");
    });
}
