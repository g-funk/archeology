# Collections

Data model for the museum collections system. No Godot dependency — plain C# classes.

## Files

- `models/collections/Item.cs` — `Item`, `Rarity` enum
- `models/collections/Shelf.cs` — `Shelf`
- `models/collections/Collection.cs` — `Collection`, `CollectionState` enum
- `models/collections/CollectionManager.cs` — `CollectionManager`

## Data model

```
Collection (Id, Name, State, Difficulty)
  └── Shelf[]
        └── Item[] (Id, Name, Description, Rarity, Parts?)
```

**Item**: either simple (leaf) or partial. A partial item has a `Parts` list of other Items; it is considered discovered only when all its parts are discovered. `IsDiscovered` is derived for partial items — `MarkDiscovered()` only needs to be called on simple items and parts.

**CollectionState**: `Locked` / `Unlocked` (byte-backed enum matching config format). State is a plain settable property; unlock logic lives in `CollectionManager`.

## CollectionManager

Plain C# class. Owns the canonical list of collections and two flat indexes built on `LoadCollections`:

- `_itemIndex`: `int → Item` for O(1) lookup by id
- `_partOf`: `partId → List<Item>` reverse index mapping a part to the partial items that contain it

### DiscoverItem(int itemId)
1. Guard: unknown id or already discovered → no-op
2. `item.MarkDiscovered()` on the target item
3. Raises `ItemDiscovered` for the target
4. Checks `_partOf`: for any partial parent now fully discovered, raises `ItemDiscovered` for the parent

### Events
- `event Action<int>? ItemDiscovered` — itemId
- `event Action<int>? CollectionUnlocked` — collectionId (raised by caller when State changes)

Note: `CollectionUnlocked` is exposed on the manager but the unlock transition (Locked → Unlocked) is the caller's responsibility — set `collection.State = CollectionState.Unlocked` and then raise the event.

## Cross-feature seams

- **Museum UI** (not yet implemented): subscribes to `ItemDiscovered` and `CollectionUnlocked` to update display.
- **Fragment collection** (not yet wired): when a Fragment is collected, the excavation system will call `CollectionManager.DiscoverItem` with the corresponding item id.
