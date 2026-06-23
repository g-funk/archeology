# MAPS

This describes what maps are and how they are designed to work with the game

## A Map

A MAP is equivalent to the Grid on the UI. There are a number of maps, for the game progress. These are stored in and read from config. Each Map has 

* Id
* Name (?)
* Layer count
* An 1..N list of buried museum items (BMI, see below)
* An 1..N list of buried crap items (BCI)
* Random seed

Layers are indexed 0..N, 0 being the topmost layer and so on.
When digging out the item, player doesn't know if it's a BMI or BCI. Only when collected, the item may turn out to be a BMI.

### Buried Museum Item (BMI)

This is what the player is searching for. Typically there may be just one BMI in each map, but we'll support more. Maybe in the very early maps there are more than one.

A BMI has the following information:

* Item ID
* Layer (index)
* Item top-left coordinate, as if it was a rectangle that fits the whole item

BMIs and BCI can not be distinguished from each other before collected. They can even have the same shapes

### Buried Crap Item (BCI)

These are items the player can dig out, but they don't go to the museum.
They could give some bonuses? More stamina maybe?

### Random Seed

This is a number that is used for generating the random layers

# Future considerations

In future we will want to design the maps better, or change the procedural generation to be more deterministic so that the rocks for example are not scattered everywhere randomly.

# Config

Id: ushort
Width: byte
Height: byte 
Name: ushort token/list pointer
Description: ushort token/list pointer
Layer count: byte
For each layer:
  Info byte: byte
  Data bytes: 0 or W * Y bytes
Shape count: byte
For each shape:
  Id: ushort, points to Items config
  Layer: byte
  X: byte, top-left corner
  Y: byte, top-left corner

 Layers start numbering from top down, index 0 is the topmost layer. In the layer data the "info byte" is read as follows:
 0=the layer is random generated like currently. There are no data bytes following.
 1=the layer data is provided in the next bytes. There are Width * Height bytes following

 The data bytes correspond to TileType enum.

 ## Future considerations

 The layer data currently wastes data and can be compacted. But until we settle with the number of materials, we can just use a byte per tile.