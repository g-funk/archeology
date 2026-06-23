Convert archaeology item source data to binary format using the script at `.claude/skills/items-to-bin/convert.py`.

When given a file path as the argument, run:
```
python3 .claude/skills/items-to-bin/convert.py <input_file> [output_file]
```

If no output file is given, a hex dump is printed to stdout. Show the hex dump and the summary to the user. If an output path is provided, confirm the byte count and path.

# Binary file layout

```
uint16  string_count
for each string:
  uint16  byte_length
  utf-8 bytes
uint16  item_count
for each item:
  uint16  id
  uint8   rarity          (common=0, uncommon=1, rare=2, epic=3, legendary=4)
  uint8   parts_count
  uint16  parts_ids[parts_count]
  uint16  name_idx        ← index into string table
  uint16  desc_idx        ← index into string table
  uint8   shape_w
  uint8   shape_h
  bytes   shape_bitmap    ← ceil(w*h/8) bytes; omitted when w*h==0
```

All multi-byte integers are little-endian.
Shape bitmap: MSB first — first cell maps to bit 7 of byte 0, reading left-to-right top-to-bottom.

# Source format

```
---
id=1000
r=common
name=Tile
description=Tile used for decorating walls and floors
01111
11111
11110
---
id=1010
r=uncommon
name=Animal
description=A statue shaped like an animal
p=1011,1012,1013
---
```

- Items are delimited by `---`
- `r` is rarity; defaults to `common` if missing
- Shape rows are consecutive 0/1 lines (trailing spaces ignored)
- `p` is a comma-separated list of part item IDs (mutually exclusive with shape rows)
- Items with parts have shape_w=0, shape_h=0, no bitmap bytes
