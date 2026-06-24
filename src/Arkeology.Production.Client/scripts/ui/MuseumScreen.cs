using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arkeology.Production.Client;

// Museum tab: scrollable list of unlocked collections, each expand/collapse-able.
// Loads its own configs; wires CollectionManager events for live updates.
public partial class MuseumScreen : Control
{
    [Export] public string CollectionsConfigPath { get; set; } = "res://data/bin/collections.bin";
    [Export] public string ItemsConfigPath       { get; set; } = "res://data/bin/items.bin";
    [Export] public string PredefinedTokensPath  { get; set; } = "res://data/json/predefined_tokens.json";
    [Export] public NodePath GridPath            { get; set; } = new("../../../../World/Grid");

    private static readonly Color BgColor      = new(0.10f, 0.08f, 0.07f);
    private static readonly Color HeaderText   = new(0.92f, 0.84f, 0.62f);
    private static readonly Color DimText      = new(0.60f, 0.55f, 0.45f);
    private static readonly Color EntryBg      = new(0.14f, 0.11f, 0.09f);
    private static readonly Color EntryBorder  = new(0.25f, 0.20f, 0.14f);
    private static readonly Color PendingBorder = new(1.00f, 0.82f, 0.32f, 0.55f);
    private static readonly Color TitleBarBg   = new(0.16f, 0.13f, 0.09f);

    private readonly CollectionManager _manager    = new();
    private ScrollContainer?           _scroll;
    private VBoxContainer?             _list;
    private readonly HashSet<int>      _expanded       = new();
    private readonly HashSet<int>      _pendingUnlock  = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        LoadData();
        _manager.ItemDiscovered     += _ => CallDeferred(MethodName.RefreshList);
        _manager.CollectionUnlocked += id => { _pendingUnlock.Add(id); _expanded.Add(id); };

        var grid = GetNodeOrNull<Grid>(GridPath);
        if (grid != null)
            grid.FragmentCollected += itemId => _manager.DiscoverItem(itemId);
        else
            GD.PushWarning($"[Museum] Grid not found at '{GridPath}' — fragment discovery disabled.");

        BuildUi();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && IsVisibleInTree())
            OnTabEntered();
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadData()
    {
        StringTable.Configure(ProjectSettings.GlobalizePath(PredefinedTokensPath));

        var itemCfgs = ReadBin(ItemsConfigPath,
            b => (IReadOnlyList<ItemConfig>)new ItemsConfigReader().Read(new MemoryStream(b)));
        var collCfgs = ReadBin(CollectionsConfigPath,
            b => (IReadOnlyList<CollectionConfig>)new CollectionsConfigReader().Read(new MemoryStream(b)));

        if (itemCfgs == null || collCfgs == null) return;

        var lookup = new Dictionary<int, ItemConfig>(itemCfgs.Count);
        foreach (var c in itemCfgs) lookup[c.Id] = c;
        _manager.LoadFrom(collCfgs, lookup);
    }

    private static T? ReadBin<T>(string resPath, Func<byte[], T> parse) where T : class
    {
        using var f = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
        if (f == null) { GD.PrintErr($"[Museum] not found: {resPath}"); return null; }
        var bytes = f.GetBuffer((long)f.GetLength());
        f.Close();
        return parse(bytes);
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUi()
    {
        foreach (var c in GetChildren().OfType<Node>().ToList()) c.QueueFree();

        var bg = new ColorRect { Color = BgColor };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);

        // Title bar
        var titlePanel = new PanelContainer();
        titlePanel.CustomMinimumSize = new Vector2(0, 60);
        var titleStyle = new StyleBoxFlat { BgColor = TitleBarBg };
        titlePanel.AddThemeStyleboxOverride("panel", titleStyle);
        root.AddChild(titlePanel);

        var titleLabel = new Label
        {
            Text = "MUSEUM",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        titleLabel.AddThemeColorOverride("font_color", HeaderText);
        titleLabel.AddThemeFontSizeOverride("font_size", 26);
        titlePanel.AddChild(titleLabel);

        // Scrollable collection list
        _scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(_scroll);

        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 4);
        _scroll.AddChild(_list);

        RefreshList();
    }

    // ── List population ───────────────────────────────────────────────────────

    private void RefreshList()
    {
        if (_list == null) return;
        foreach (var c in _list.GetChildren().OfType<Node>().ToList()) c.QueueFree();

        var unlocked = _manager.Collections
            .Where(c => c.State == CollectionState.Unlocked)
            .ToList();

        if (unlocked.Count == 0)
            AddEmptyState();
        else
            foreach (var collection in unlocked)
                AddCollectionEntry(collection);

        var nextLocked = _manager.Collections
            .FirstOrDefault(c => c.State == CollectionState.Locked);
        if (nextLocked != null)
            AddLockedPlaceholder(nextLocked);
    }

    private void AddEmptyState()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 48);
        _list!.AddChild(margin);

        var label = new Label
        {
            Text = "Discover your first artifact\nto unlock the museum.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word,
        };
        label.AddThemeColorOverride("font_color", DimText);
        label.AddThemeFontSizeOverride("font_size", 18);
        margin.AddChild(label);
    }

    private void AddLockedPlaceholder(Collection collection)
    {
        var entryStyle = new StyleBoxFlat
        {
            BgColor        = new Color(0.12f, 0.10f, 0.08f),
            BorderColor    = new Color(0.20f, 0.16f, 0.11f),
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderWidthLeft = 1, BorderWidthRight  = 1,
        };
        var entry = new PanelContainer();
        entry.AddThemeStyleboxOverride("panel", entryStyle);
        _list!.AddChild(entry);

        var label = new Label
        {
            Text = $"  ?  {ScrambleName(collection.Name)}",
            CustomMinimumSize = new Vector2(0, 56),
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeColorOverride("font_color", new Color(0.38f, 0.32f, 0.24f));
        label.AddThemeFontSizeOverride("font_size", 20);
        entry.AddChild(label);
    }

    private static string ScrambleName(string name)
    {
        var chars = new System.Text.StringBuilder(name.Length);
        foreach (char c in name)
            chars.Append(c == ' ' ? ' ' : '?');
        return chars.ToString();
    }

    private void AddCollectionEntry(Collection collection)
    {
        bool expanded = _expanded.Contains(collection.Id);
        bool pending  = _pendingUnlock.Contains(collection.Id);

        var borderColor = pending ? PendingBorder : EntryBorder;
        var entryStyle  = new StyleBoxFlat
        {
            BgColor           = EntryBg,
            BorderColor       = borderColor,
            BorderWidthTop    = 1, BorderWidthBottom = 1,
            BorderWidthLeft   = 1, BorderWidthRight   = 1,
        };

        var entry = new PanelContainer();
        entry.AddThemeStyleboxOverride("panel", entryStyle);
        _list!.AddChild(entry);

        var entryBox = new VBoxContainer();
        entryBox.AddThemeConstantOverride("separation", 0);
        entry.AddChild(entryBox);

        entryBox.AddChild(MakeHeader(collection, expanded));

        if (expanded)
        {
            entryBox.AddChild(MakeSeparator());
            entryBox.AddChild(MakeShelvesContent(collection));
        }
    }

    private Button MakeHeader(Collection collection, bool expanded)
    {
        var arrow = expanded ? "▼" : "▶";
        var stars = collection.Difficulty > 0 ? "  " + new string('★', collection.Difficulty) : "";
        var btn = new Button
        {
            Text              = $"  {arrow}  {collection.Name}{stars}",
            CustomMinimumSize = new Vector2(0, 56),
            Flat              = true,
            Alignment         = HorizontalAlignment.Left,
        };
        btn.AddThemeColorOverride("font_color", HeaderText);
        btn.AddThemeColorOverride("font_hover_color", new Color(1.00f, 0.93f, 0.76f));
        btn.AddThemeColorOverride("font_pressed_color", new Color(0.80f, 0.72f, 0.50f));
        btn.AddThemeFontSizeOverride("font_size", 20);
        btn.AddThemeStyleboxOverride("normal",  new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("focus",   new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("hover",   new StyleBoxFlat { BgColor = new Color(0.20f, 0.16f, 0.11f) });
        btn.AddThemeStyleboxOverride("pressed", new StyleBoxFlat { BgColor = new Color(0.17f, 0.13f, 0.09f) });

        int id = collection.Id;
        btn.Pressed += () =>
        {
            if (_expanded.Contains(id)) _expanded.Remove(id);
            else _expanded.Add(id);
            RefreshList();
        };

        return btn;
    }

    private static HSeparator MakeSeparator()
    {
        var sep = new HSeparator();
        var style = new StyleBoxFlat { BgColor = new Color(0.28f, 0.22f, 0.15f), ContentMarginTop = 1f };
        sep.AddThemeStyleboxOverride("separator", style);
        return sep;
    }

    private static MarginContainer MakeShelvesContent(Collection collection)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   12);
        margin.AddThemeConstantOverride("margin_right",  12);
        margin.AddThemeConstantOverride("margin_top",     8);
        margin.AddThemeConstantOverride("margin_bottom",  8);

        var shelvesBox = new VBoxContainer();
        shelvesBox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(shelvesBox);

        foreach (var shelf in collection.Shelves)
        {
            var row = new ShelfRow();
            row.SetItems(shelf.Items);
            shelvesBox.AddChild(row);
        }

        return margin;
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void OnTabEntered()
    {
        if (_pendingUnlock.Count == 0) return;
        RefreshList();
        _pendingUnlock.Clear();
        // TODO: scroll to newly-unlocked collection + glow effect
    }
}
