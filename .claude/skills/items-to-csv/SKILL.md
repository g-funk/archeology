When given archaeology item source data, convert it into CSV with columns:
id, rarity, name, description, shape w, shape h, shape data, parts.

Rules:
- r becomes rarity.
- Shape is consecutive 0/1 rows after description.
- p becomes parts.
- Each item has either shape (w,h,data) or parts.
- Preserve IDs as numbers.
- Quote CSV fields when needed.
- If rarity is missing, default to common
- Shape w is the width of the data in two dimensional array
- Shape h is the height of the data in two dimensional array
- Shape data is one-dimensional array
- Parts should be stored comma-separated, e.g. 1011,1012,1013.


# Example

id=1000
r=common
name=Tile
description=Tile used for decorating walls and floors
01111
11111
11110

-> 1000,common,Tile,"Tile used for decoration walls and floors",5,3,011111111111110,,