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

An ITEM belongs to a SHELF and through that to a COLLECTION.
An ITEM has

* Id
* Name
* Description
* Rarity
* Discovered: Simple items are either discovered or not. Partial items are discovered only if all parts have been discovered.
* Parts: Can be missing. Used to denote the needed ITEMs if this ITEM is partial

### Partial Items

Partial Items consist of multiple parts. These Items can only be discovered when all parts have been found. A part is just another ITEM.

## Events/Signals

The model should emit the following events/signals to the rest of the game:

* OnCollectionUnlocked: When a collection is unlocked
* OnItemDiscovered: When an item has been marked as discovered


## Config

See CONFIG.md for general specifications

First, the Item information is stored:

1. Id: ushort (1000+ for simple items, 10000+ for partial items)
2. Name: ushort (string list pointer)
3. Description: ushort (string list pointer)
4. Rarity: byte
5. if partial (id=10000+), part count
6. Part ids: count * ushort

Then, collections are stored in order:

1. Id: int
2. Name: string
3. State: byte
4. Difficulty: byte
5. Shelf count

SHELVES belonging to that COLLECTION are then placed one by one:

1. Item count: byte
2...N Item: int 

### Future considerations

As the descriptions may contain repeated words, it might make sense to store those as lists of ushorts. So we would have string lists, then lists of string pointers, and ultimately pointers to either directly to string lists, or to lists of string pointers

