# Items

This document describes the different aspects of items


## Types of Items

### Regular Items

These are the "normal" items. They come in predefined forms from config. They have different rarities as described below.

### Scrap Items

These are filler items that do not go to museum. The collected scrap items can be turned into gems when completing the map. The scrap items are always common rarity.

### Partial Items

Partial items are made of 2-N parts. They are shown in the shelf as descibed in MUSEUM.md. Partial items can be of different rarities as well. The rarity of partial items determines it's spread on the timeline, ie. how many maps on average need to be dug out to find a part. Note that the total number of maps to explore is determined by both the rarity and the number of items, approximately in pseudo:

total maps = part count * rarity

Average maps needed to find one part:

Common - 1
Uncommon - 2
Rare - 3
Epic - 4
Legendary - 5

Note that although this is linear right now, this may change in future

# Item Rarities

## Rarities

Common - white(ish)
Uncommon - green
Rare - Purple
Legendary - Gold

Later on, for events and such we can add more rarities like Mythic, Ultimate, ....

## Grid

In the grid the items are colored using their rarities

## Museum

The items are colored using their rarities

## Scrap Items

Scrap items have the color of common items


# Config

Items are defined in their own config file (see also CONFIG.md), the header following the config file format. For each item stored the following information is stored:

Id: ushort
Rarity: byte
Parts count: byte
Parts ids: 0..N ushort
Name: ushort (token or list pointer)
Description: ushort (token list pointer)


# TODO and future considerations

In future, we may have globally available items with rarities going beyond Legendary. For example:

* Mythic - Single items spread to 1/10 of player base
* Mythic parts - Parts of Mythic items spread to players, and players need to combine them - everyone who works together gets the part
* Ultimate - Single items spread to 1/100 of player base
* Mythic Ultimate - 5 globally

These items could come with perks, like double, triple, quadruple etc max stamina for a limited time