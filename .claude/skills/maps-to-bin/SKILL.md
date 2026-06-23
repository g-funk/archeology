Convert maps source data to binary format using the script at `.claude/skills/maps-to-bin/convert.py`.

When given a file path as the argument, run:
```
python3 .claude/skills/maps-to-bin/convert.py <input_file> [output_file]
```

If no output file is given, a hex dump is printed to stdout. Show the hex dump and the summary line to the user. If an output path is provided, confirm the byte count and path.

# Binary file layout

```
uint16  string_count
for each string:
  uint16  byte_length
  utf-8 bytes
uint16  map_count
for each map:
  uint16  id
  uint8   width
  uint8   height
  uint16  name_idx        ← index into string table
  uint16  desc_idx        ← index into string table
  uint8   layer_count
  for each layer:
    uint8  info_byte      (0 = random, 1 = data provided)
    if info_byte == 1: width * height bytes of tile data
  uint8   shape_count
  for each shape (BMI):
    uint16  item_id
    uint8   layer
    uint8   x
    uint8   y
  uint8   scrap_count
```

All multi-byte integers are little-endian.

# Source format (maps_data_source.md)

```
---
id=10000
w=10
h=20
name=Tutorial Map
description=Tutorial Map
layers:
  li=0
  li=0
shapes:
  id=1000
  l=1
  x=3
  y=4
scraps:0
---
```

- Maps are delimited by `---`
- `layers:` section: each `li=N` is one layer info byte (0=random, 1=data provided)
- `shapes:` section: groups of id/l/x/y are Buried Museum Items (BMIs)
- `scraps:N` is the scrap shape count byte
