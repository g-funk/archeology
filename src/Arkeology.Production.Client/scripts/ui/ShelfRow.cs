using Godot;
using System;
using System.Collections.Generic;

namespace Arkeology.Production.Client;

// Draws one shelf's item tiles inside a museum collection entry.
// Each item gets one slot; partial items that are not fully discovered
// show one slot per part instead.
public partial class ShelfRow : Control
{
    public const int RowHeight = 92;
    private const int SlotSize = 72;
    private const int SlotGap  = 8;
    private const int RowPad   = 10;
    private const int CellInset = 6;

    private static readonly Color SlotBg          = new(0.14f, 0.11f, 0.09f);
    private static readonly Color SlotDiscoveredBg = new(0.22f, 0.17f, 0.07f);
    private static readonly Color Gold             = new(1.00f, 0.82f, 0.32f);
    private static readonly Color PlaceholderFill  = new(0.22f, 0.18f, 0.14f);
    private static readonly Color CompleteBg       = new(0.24f, 0.19f, 0.08f);

    private IReadOnlyList<Item> _items = Array.Empty<Item>();

    public void SetItems(IReadOnlyList<Item> items)
    {
        _items = items;
        int slots = CountSlots(items);
        CustomMinimumSize = new Vector2(RowPad + slots * (SlotSize + SlotGap), RowHeight);
        QueueRedraw();
    }

    private static int CountSlots(IReadOnlyList<Item> items)
    {
        int n = 0;
        foreach (var item in items)
            n += (item.IsPartial && !item.IsDiscovered) ? item.Parts!.Count : 1;
        return n;
    }

    public override void _Draw()
    {
        float x = RowPad;
        float y = (Size.Y - SlotSize) * 0.5f;

        foreach (var item in _items)
        {
            if (item.IsPartial)
            {
                if (item.IsDiscovered)
                    DrawCompleteSlot(new Vector2(x, y), ref x);
                else
                    foreach (var part in item.Parts!)
                        DrawItemSlot(new Vector2(x, y), part, ref x);
            }
            else
            {
                DrawItemSlot(new Vector2(x, y), item, ref x);
            }
        }
    }

    private void DrawItemSlot(Vector2 origin, Item item, ref float x)
    {
        DrawRect(new Rect2(origin, new Vector2(SlotSize, SlotSize)),
            item.IsDiscovered ? SlotDiscoveredBg : SlotBg);

        if (item.IsDiscovered)
            DrawShape(origin, item.Config);
        else
            DrawPlaceholder(origin);

        x += SlotSize + SlotGap;
    }

    private void DrawCompleteSlot(Vector2 origin, ref float x)
    {
        DrawRect(new Rect2(origin, new Vector2(SlotSize, SlotSize)), CompleteBg);
        float inset = CellInset + 2f;
        DrawRect(new Rect2(
            origin + new Vector2(inset, inset),
            new Vector2(SlotSize - 2f * inset, SlotSize - 2f * inset)),
            Gold);
        x += SlotSize + SlotGap;
    }

    private void DrawShape(Vector2 origin, ItemConfig cfg)
    {
        if (cfg.ShapeWidth <= 0 || cfg.ShapeHeight <= 0) return;

        float avail = SlotSize - 2f * CellInset;
        float cell  = Mathf.Min(avail / cfg.ShapeWidth, avail / cfg.ShapeHeight);
        float ox = origin.X + CellInset + (avail - cell * cfg.ShapeWidth)  * 0.5f;
        float oy = origin.Y + CellInset + (avail - cell * cfg.ShapeHeight) * 0.5f;

        for (int r = 0; r < cfg.ShapeHeight; r++)
            for (int c = 0; c < cfg.ShapeWidth; c++)
            {
                if (!cfg.IsShapeOccupied(c, r)) continue;
                DrawRect(new Rect2(
                    new Vector2(ox + c * cell, oy + r * cell),
                    new Vector2(cell - 1f, cell - 1f)),
                    Gold);
            }
    }

    private void DrawPlaceholder(Vector2 origin)
    {
        float inset = CellInset + 8f;
        DrawRect(new Rect2(
            origin + new Vector2(inset, inset),
            new Vector2(SlotSize - 2f * inset, SlotSize - 2f * inset)),
            PlaceholderFill);
    }
}
