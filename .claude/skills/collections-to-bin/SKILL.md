---
model: haiku
---
Convert collections source data to binary format using the script at `.claude/skills/collections-to-bin/convert.py`.

When given a file path as the argument, run:
```
python3 .claude/skills/collections-to-bin/convert.py <input_file> [output_file] [--tokenized]
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
[collection data]
  uint16  collection_count
  for each collection:
    uint16  id
    uint16  name_ptr         ← token ID (<20000) or token list index + 20000
    uint8   difficulty
    uint8   shelf_count
    for each shelf:
      uint8   item_count
      int32   item_ids[item_count]
```

All multi-byte integers are little-endian.
Predefined tokens are loaded from `data/json/predefined_tokens.json`. See CONFIG_STRINGS.md.

# Source format

```
---
id=10000
name=Old decoration
difficulty=0
shelves:
  itemcount=1
  items=1000
  itemcount=1
  items=1001
---
id=10001
name=Ancient animal statue
difficulty=1
shelves:
  itemcount=1
  items=1010
---
```

- Collections are delimited by `---`
- `difficulty` is a byte (0 = easiest)
- `shelves:` section: each shelf is a pair of `itemcount=N` and `items=id1,id2,...`
- Item IDs in shelves are stored as int32
