# Arkeology - Design Document for Simple Version

This document describes a simple version of the Arkeology game. The DESIGN.md describes the larger version, but it only needs to be consulted for some details that are not covered in this doc. 

## Documents

These documents can be consulted when details are needed. If anything is conflicting, THIS document usually takes precedence when implementing the simple version. Anything unclear should be checked with the user.

* /design/DESIGN.md - the design for the large version of the game
* /design/features/ - contains feature-specific specs
* /ai-docs/ - a log of what the AI agents (Claude) have been doing


# Platforms

Consider Mobile as the primary platform, but a web version is possible at some point too.


# Mechanics

## Stamina

* Stamina is used for digging. For now 1 stamina for each tile (configurable variable staminaSpend).
* Stamina is an energy mechanism in this game. Full stamina is 100 for now (configurable variable staminaFull). 
* The digging speed starts to slow down when stamina is less than 10 (configurable variable staminaSlowdownLimit). For example tile dig time could be t = baseT + (staminaSlowdownLimit - stamina) * slowdownTime.
* slowdownTime is also configurable


Examples, slowdowntime is 200ms
stamina is 9 -> digging one tile takes 0.2s longer
stamina is 0 -> digging one tile takes 2s longer


# UI/UX

UI is based on portrait orientation mobile

## Usage

Mobile first, so we'll need to consider touchscreen based interface primarily. For example:
* Tap - move
* Double-tap - move + dig (walk to the tile, then autodig on arrival)
* Tap character - dig
* Long-tap character - scan
* Tap fully exposed fragment anywhere - collect

## Screens

1. Grid (main screen)
2. Museum - collected items
3. Relaxation room - watch ads to recharge stamina


### Grid screen

* Grid, size configurable, starting size for example 20x40
* Bottom tabs (Grid, Museum, Relaxation Room)
* Top part contains the collections from current dig session
* Very top part contains the stamina of the character
