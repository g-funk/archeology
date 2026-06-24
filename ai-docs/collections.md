# Collections

Data model for the museum collections system. No Godot dependency — plain C# classes.

## Files

- `config/CollectionConfig.cs` — `CollectionConfig`, `ShelfConfig` records (raw binary data)
- `config/CollectionsConfigReader.cs` — reads `data/bin/collections.bin` → `IReadOnlyList<CollectionConfig>`
- `config/ItemConfig.cs` — `ItemConfig`, `Rarity` enum
- `models/collections/Item.cs` — `Item` (runtime instance)
- `models/collections/Shelf.cs` — `Shelf`
- `models/collections/Collection.cs` — `Collection`, `CollectionState` enum
- `models/collections/CollectionManager.cs` — `CollectionManager`

## Data model

```
Collection (Id, Name, State, Difficulty)
  └── Shelf[]
        └── Item[]
              └── Config: ItemConfig (Id, Name, Description, Rarity, Parts?)
```

**CollectionConfig** / **ShelfConfig**: immutable records deserialized from `data/bin/collections.bin`. `CollectionConfig` holds Id, Name, Difficulty, and `IReadOnlyList<ShelfConfig>`. Each `ShelfConfig` holds `IReadOnlyList<int>` item IDs (int32 in the binary).

**ItemConfig**: immutable, deserialized from `data/bin/items.bin`. Holds all static properties: `Id`, `Name`, `Description`, `Rarity`, `Parts` (`IReadOnlyList<int>?` — part IDs only), and `IsPartial`. `Rarity` enum lives here.

**Item**: runtime instance wrapping an `ItemConfig`. Holds only mutable state: `_discovered` flag, `Parts` (`IReadOnlyList<Item>?` — runtime instances for discovery tracking), and `IsDiscovered`/`MarkDiscovered`. `IsPartial` delegates to `Config.IsPartial`. `MarkDiscovered()` only needs to be called on simple items and parts.

**CollectionState**: `Locked` / `Unlocked` (byte-backed enum matching config format). State is a plain settable property; unlock logic lives in `CollectionManager`.

## CollectionManager

Plain C# class. Owns the canonical list of collections and two flat indexes built on `LoadCollections`:

- `_itemIndex`: `int → Item` for O(1) lookup by id — keyed on both shelf items **and** their parts
- `_partOf`: `partId → List<Item>` reverse index mapping a part to the partial items that contain it

### LoadFrom (primary entry point)

```cs
public void LoadFrom(IReadOnlyList<CollectionConfig> configs, IReadOnlyDictionary<int, ItemConfig> itemLookup)
```

Builds the full `Collection` / `Shelf` / `Item` tree from raw configs + an item lookup (from `ItemsConfigReader`). Partial items have their part `Item` instances resolved recursively via `GetOrBuild`. Calls `LoadCollections` internally.

### DiscoverItem(int itemId)
1. Guard: unknown id or already discovered → no-op
2. `item.MarkDiscovered()` on the target item
3. Raises `ItemDiscovered` for the target
4. Checks `_partOf`: for any partial parent now fully discovered, raises `ItemDiscovered` for the parent

### Events
- `event Action<int>? ItemDiscovered` — itemId
- `event Action<int>? CollectionUnlocked` — collectionId (raised by caller when State changes)

Note: `CollectionUnlocked` is exposed on the manager but the unlock transition (Locked → Unlocked) is the caller's responsibility — set `collection.State = CollectionState.Unlocked` and then raise the event.

## Binary format

`data/bin/collections.bin` is produced by `.claude/skills/collections-to-bin/convert.py`. Layout:

```
[header] version (2 bytes) + epoch (8 bytes) + token tables
uint16  collection_count
for each collection:
  uint16  id
  uint16  name_ptr
  uint8   difficulty
  uint8   shelf_count
  for each shelf:
    uint8   item_count
    int32   item_ids[item_count]
```

Source: `test-data/collections-data-source.md` (and future production data file).

## Loading sequence (caller's responsibility)

```
1. StringTable.Configure(predefinedTokensPath)
2. Read items.bin   → IReadOnlyList<ItemConfig> via ItemsConfigReader
3. Read collections.bin → IReadOnlyList<CollectionConfig> via CollectionsConfigReader
4. Build lookup: itemLookup = items.ToDictionary(i => i.Id)
5. collectionManager.LoadFrom(collectionConfigs, itemLookup)
```

## Cross-feature seams

- **Museum UI** (`scripts/ui/MuseumScreen.cs`, `scripts/ui/ShelfRow.cs`): owns a `CollectionManager`, loads both binaries on `_Ready`, subscribes to `ItemDiscovered` and `CollectionUnlocked`. See [ai-docs/museum.md](museum.md).
- **Fragment collection** (not yet wired): when a Fragment is collected, the excavation system will call `CollectionManager.DiscoverItem` with the corresponding item id.
