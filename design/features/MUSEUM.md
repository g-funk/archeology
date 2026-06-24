# Museum

See also COLLECTIONS.md for the collection/shelf model and ITEMS.md for item types, rarities, and partial item details.

## Purpose

Museum is the visualization of COLLECTIONs, SHELVEs and ITEMs as described in COLLECTIONS.md

## Collections display

For a new player, the museum is disabled/locked as no collections are unlocked. When the first item is collected, there will be a celebration and "Museum unlocked" type of message.

The COLLECTIONs are arranged as a scrollable vertical list. Each COLLECTION is by default collapsed, but can be expanded to display the SHELVEs, ITEMs and PLACEHOLDERs

Each collection has a title bar, which is used to expand/collapse the section. The title shows the name of the COLLECTION. For undiscovered COLLECTIONs, scrambled text is shown.

Locked COLLECTIONs are not visible.

## Events

The MUSEUM UI component listens for events (or possibly Godot signals) and reacts accordingly

* OnCollectionUnlocked: Records 'unseen' collection unlocks. When the player enters the museum the next time, this data is used to show some effects and focus the player to the new COLLECTION. See 'Unlocking' section

## Unlocking

When the player visits the MUSEUM tab for the first time after unlocking a new SECTION, the screen automatically scrolls to that collection and shows a glow or some other effect around the collection. The section is also expanded automatically. 

See 'Events' section for how the MUSEUM knows that a collection has been unlocked

## Shelves

Each COLLECTION contains 1..N SHELVES. Harder collections contain more shelves than easier ones, but the exact amounts are configured in configuration. A SHELF can contain 1-N items. These are from configuration.

## Items and placeholders

If a collection is in expanded state, on each SHELF, for each ITEM:

* If the ITEM is discovered, shows the ITEM graphics
* If it's not discovered, PLACEHOLDER graphics is shown
* If it's a partial item partially discovered, show the parts similarly to normal items: placeholders for undiscovered and item graphics for discovered
* If it's a partial item fully discovered, show the full item graphics only, and not parts




