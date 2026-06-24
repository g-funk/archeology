# Museum

Museum tab UI: scrollable list of collections, each with expand/collapse shelves showing item tiles.

## Files

- `scripts/ui/MuseumScreen.cs` — main museum Control; loads configs, owns CollectionManager, builds UI
- `scripts/ui/ShelfRow.cs` — custom-draw Control for one shelf's item tiles

## Scene wiring

`Museum` node in `scenes/Main.tscn` is a `Control` with `MuseumScreen` script attached. It sits inside the `TabContainer` at tab index 1. Tab visibility is managed by the TabContainer; `MuseumScreen` reacts to `NotificationVisibilityChanged` to process pending unlocks on tab entry.

## MuseumScreen

Owns a `CollectionManager` (not shared with Grid). On `_Ready`:
1. `StringTable.Configure(PredefinedTokensPath)`
2. Reads `data/bin/items.bin` → `ItemsConfigReader`
3. Reads `data/bin/collections.bin` → `CollectionsConfigReader`
4. Calls `_manager.LoadFrom(collectionConfigs, itemLookup)`

Subscribes to:
- `ItemDiscovered` → `CallDeferred(RefreshList)` — redraws the list after any item find
- `CollectionUnlocked` → adds to `_pendingUnlock` + `_expanded`, refreshes on next tab entry

### Expand/collapse state

`_expanded: HashSet<int>` (collection IDs). Clicking a collection header toggles and calls `RefreshList()`, which rebuilds all children under `_list`.

### Pending unlock

`_pendingUnlock: HashSet<int>` — collections unlocked during gameplay while museum is hidden. Processed in `OnTabEntered()` (triggered by `NotificationVisibilityChanged`). On entry: refresh list, clear pending. TODO: scroll-to + glow effect.

### Empty state

When no collections are unlocked, shows a single centered label ("Discover your first artifact...").

## ShelfRow

Custom-draw `Control`. Exposed API: `SetItems(IReadOnlyList<Item>)` — updates `CustomMinimumSize` and calls `QueueRedraw()`.

### Slot layout

`RowHeight = 92px`. Each slot is `72×72px` with `8px` gap and `10px` left padding. Slots are centered vertically.

### Slot types

| Case | Rendering |
|------|-----------|
| Normal, discovered | Gold shape bitmap on dark gold bg |
| Normal, undiscovered | Placeholder square on dark bg |
| Partial, fully discovered | Single filled gold square on dark gold bg |
| Partial, not fully discovered | One slot per part (each discovered or placeholder) |

### Shape rendering

Uses `ItemConfig.CubeOffsets` (cube-coordinate cell list). Each `(dq, dr)` is converted to odd-r offset grid: `col = dq + (dr - (dr & 1)) / 2`, `row = dr`. Bounds are computed, then cells are drawn as gold squares scaled to fit within the slot's available area (`SlotSize - 2 * CellInset`). Odd rows are shifted right by `cell * 0.5` to show the hex stagger.

## CollectionManager unlock detection

`DiscoverItem` now checks for newly-unlocked collections after marking an item discovered:

```cs
foreach (var c in _collections.Where(c => c.State == CollectionState.Locked && !c.IsLocked))
{
    c.State = CollectionState.Unlocked;
    CollectionUnlocked?.Invoke(c.Id);
}
```

## Not yet wired

- `Grid.TryCollectFragment` does not call `_manager.DiscoverItem` — the excavation→museum signal path is not connected. All items show as undiscovered until this is wired.
- Scroll-to-collection on unlock
- Glow/highlight effect on newly-unlocked collection
