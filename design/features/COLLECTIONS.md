# Collections

a COLLECTION is a collection of SHELVES, and a SHELF is a grouping of ITEMs. This document describes each, and how the COLLECTIONS model interacts with the rest of the game

## Collection

A COLLECTION has

* Id
* Name
* State: locked/unlocked. If any of the ITEMS in the collection are discovered, then the COLLECTION is unlocked
* Difficulty

## Shelf

A SHELF is a group of 1-N items in a COLLECTION. It has no other purpose.

## Item

An ITEM belongs to a SHELF and through that to a COLLECTION. See [ITEMS.md](ITEMS.md) for the full ITEM data model, field definitions, rarities, and partial item details.

## Events/Signals

The model should emit the following events/signals to the rest of the game:

* OnCollectionUnlocked: When a collection is unlocked
* OnItemDiscovered: When an item has been marked as discovered


## Config

See CONFIG.md for general specifications. Item config format is defined in [ITEMS.md](ITEMS.md).

Collections are stored in order:

1. Id: ushort
2. Name: string
3. Difficulty: byte
4. Shelf count

SHELVES belonging to that COLLECTION are then placed one by one:

1. Item count: byte
2...N Item: int 

Partial items are only marked with their main item id. The program needs to show the partial items in the collection until they are all collected. See MUSEUM.md for UI details


