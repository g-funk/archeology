---
model: haiku
---
Convert archaeology item source data to CSV using the script at `.claude/skills/items-to-csv/convert.py`.

When given a file path as the argument, run:
```
python3 .claude/skills/items-to-csv/convert.py <input_file> [output_file]
```

If no output file is given the CSV is printed to stdout. Show the output to the user and confirm where it was saved if an output path was provided.

# CSV columns
id, rarity, name, description, shape w, shape h, shape data, parts

# Conversion rules (for reference / script maintenance)
- r becomes rarity; defaults to common if missing.
- Shape rows are consecutive 0/1 lines after description; flattened to a 1D string.
- p becomes parts (comma-separated part IDs).
- Each item has either shape (w, h, data) or parts — not both.

# Example

id=1000
r=common
name=Tile
description=Tile used for decorating walls and floors
01111
11111
11110

-> 1000,common,Tile,Tile used for decorating walls and floors,5,3,011111111111110,