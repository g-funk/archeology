Convert archaeology item source data to binary format using the script at `.claude/skills/items-to-bin/convert.py`.

When given a file path as the argument, run:
```
python3 .claude/skills/items-to-bin/convert.py <input_file> [output_file] [--tokenized]
```

If no output file is given, a hex dump is printed to stdout. Show the hex dump and the summary to the user. If an output path is provided, confirm the byte count and path.

`--tokenized` enables full string tokenization (predefined + user tokens + token lists). Without it, each string is stored as a single UTF-8 user token (naive mode).

# Binary file layout

```
[header]
  uint8   version_major
  uint8   version_minor
  int64   build_epoch        (seconds since Unix epoch, little-endian)
[token table]
  uint16  user_token_count
  for each user token (ID 2000, 2001, ...):
    uint8   byte_length
    utf-8   bytes
[token list table]
  uint16  token_list_count
  for each token list:
    uint8   token_count
    uint16  token_ids[token_count]
[item data]
  uint16  item_count
  for each item:
    uint16  id
    uint8   rarity           (common=0, uncommon=1, rare=2, epic=3, legendary=4)
    uint8   parts_count
    uint16  parts_ids[parts_count]
    uint16  name_ptr         ← token ID (<20000) or token list index + 20000
    uint16  desc_ptr
    uint8   cell_count
    for each cell (omitted when cell_count==0):
      int8    dq             ← cube-coordinate column offset (signed)
      int8    dr             ← cube-coordinate row offset (signed)
```

All multi-byte integers are little-endian.
Shape cells are cube offsets (dq, dr) relative to the item's anchor cell.
Convert to odd-r offset grid: `col = dq + (dr - (dr & 1)) / 2`, `row = dr`.
Predefined tokens are loaded from `data/json/predefined_tokens.json`. See CONFIG_STRINGS.md.

# Source format

```
---
id=1000
r=common
name=Tile
description=Tile used for decorating walls and floors
. X X X X
 X X X X .
X X X X .
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
- Shape rows use `X`=occupied, `.`=empty, space-separated; odd rows (1, 3, 5 ...) are indented by 1 leading space to show the hex stagger
- Cell at column `cx`, row `ry` maps to cube offset `dq = cx - ry // 2`, `dr = ry`
- `p` is a comma-separated list of part item IDs (mutually exclusive with shape rows)
- Items with parts have cell_count=0 and no cell bytes
