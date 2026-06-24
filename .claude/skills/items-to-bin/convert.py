#!/usr/bin/env python3
"""Convert items source data (.md) to binary format.

File layout:
  [header]
    uint8   version_major
    uint8   version_minor
    int64   build_epoch (seconds since Unix epoch, little-endian)
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
      uint8   rarity         (common=0, uncommon=1, rare=2, epic=3, legendary=4)
      uint8   parts_count
      uint16  parts_ids[parts_count]
      uint16  name_ptr       (token ID if <20000, else token list index + 20000)
      uint16  desc_ptr
      uint8   cell_count
      for each cell (omitted when cell_count==0):
        int8    dq            (cube-coordinate column offset, signed)
        int8    dr            (cube-coordinate row offset, signed)

String pointers: value < 20000 is a single token ID; >= 20000 means (value - 20000) is
an index into the token list table.  See CONFIG_STRINGS.md.

Shape format: cube offsets (dq, dr) relative to the item's anchor cell.
Convert to odd-r offset grid: col = dq + (dr - (dr & 1)) / 2, row = dr.
Convert back to cube from anchor (anchorCol, anchorRow):
  q0 = anchorCol - (anchorRow - (anchorRow & 1)) / 2
  gx = (q0 + dq) + (tr - (tr & 1)) / 2   where tr = anchorRow + dr
  gy = anchorRow + dr
"""
import argparse
import os
import struct
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
from lib.tokens import load_predefined, Tokenizer, encode_header

RARITIES = {'common': 0, 'uncommon': 1, 'rare': 2, 'epic': 3, 'legendary': 4}
VERSION  = (1, 1)


def is_shape_row(line):
    """A shape row contains only X, . and spaces with at least one X or dot."""
    s = line.strip()
    return bool(s) and all(c in 'X. ' for c in s) and ('X' in s or '.' in s)


def parse_shape_cells(rows):
    """Convert stagger-aware ASCII rows to list of (dq, dr) cube offsets.

    Odd source rows are indented 1 space (visual stagger only — parsing uses
    the column index cx and the formula dq = cx - ry // 2, dr = ry).
    """
    cells = []
    for ry, raw_line in enumerate(rows):
        tokens = raw_line.split()
        for cx, token in enumerate(tokens):
            if token == 'X':
                dq = cx - ry // 2
                dr = ry
                cells.append((dq, dr))
    return cells


def parse_items(text):
    items = []
    for block in text.split('---'):
        lines = block.strip().splitlines()
        if not lines:
            continue

        item      = {}
        shape_rows = []
        in_shape   = False

        for line in lines:
            stripped = line.strip()
            if not stripped or stripped.startswith('#'):
                continue

            if is_shape_row(line):
                in_shape = True
                shape_rows.append(line)
            elif '=' in line and not in_shape:
                key, _, value = line.partition('=')
                key = key.strip(); value = value.strip()
                if key == 'id':            item['id']          = int(value)
                elif key == 'r':           item['rarity']       = value
                elif key == 'name':        item['name']         = value
                elif key == 'description': item['description']  = value
                elif key == 'p':
                    item['parts'] = [int(x.strip()) for x in value.split(',') if x.strip()]

        if shape_rows:
            item['cells'] = parse_shape_cells(shape_rows)

        if 'id' not in item:
            continue

        item.setdefault('rarity',      'common')
        item.setdefault('name',        '')
        item.setdefault('description', '')
        item.setdefault('parts',       [])
        item.setdefault('cells',       [])
        items.append(item)

    return items


def encode(items, tokenizer):
    for item in items:
        item['name_ptr'] = tokenizer.tokenize(item['name'])
        item['desc_ptr'] = tokenizer.tokenize(item['description'])

    buf  = bytearray()
    buf += encode_header(*VERSION)
    buf += tokenizer.encode_tables()

    buf += struct.pack('<H', len(items))
    for item in items:
        buf += struct.pack('<H', item['id'])
        buf += struct.pack('<B', RARITIES.get(item['rarity'], 0))

        parts = item['parts']
        buf += struct.pack('<B', len(parts))
        for pid in parts:
            buf += struct.pack('<H', pid)

        buf += struct.pack('<H', item['name_ptr'])
        buf += struct.pack('<H', item['desc_ptr'])

        cells = item['cells']
        buf += struct.pack('<B', len(cells))
        for dq, dr in cells:
            buf += struct.pack('<b', dq)
            buf += struct.pack('<b', dr)

    return bytes(buf)


def hex_dump(data):
    for off in range(0, len(data), 16):
        chunk    = data[off:off + 16]
        hex_part = ' '.join(f'{b:02x}' for b in chunk)
        asc_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
        print(f'{off:04x}  {hex_part:<47}  {asc_part}')


def main():
    parser = argparse.ArgumentParser(description='Convert items source data to binary config.')
    parser.add_argument('input_file')
    parser.add_argument('output_file', nargs='?')
    parser.add_argument('--tokenized', action='store_true',
                        help='Split strings into predefined+user tokens. '
                             'Without this flag each string is stored as a single UTF-8 user token.')
    args = parser.parse_args()

    with open(args.input_file, encoding='utf-8') as f:
        text = f.read()

    no_space, normal = load_predefined()
    tokenizer        = Tokenizer(no_space, normal, tokenized=args.tokenized)
    items            = parse_items(text)
    data             = encode(items, tokenizer)

    if args.output_file:
        with open(args.output_file, 'wb') as f:
            f.write(data)
        print(f"Wrote {len(data)} bytes → {args.output_file}")
    else:
        hex_dump(data)

    mode = 'tokenized' if args.tokenized else 'naive'
    print(
        f"[{mode}] {tokenizer.user_token_count} user token(s), "
        f"{tokenizer.token_list_count} token list(s), "
        f"{len(items)} item(s):",
        file=sys.stderr,
    )
    for item in items:
        shape = f"cells={len(item['cells'])}" if not item['parts'] else f"parts={item['parts']}"
        print(
            f"  id={item['id']} r={item['rarity']} '{item['name']}' {shape} "
            f"name_ptr={item['name_ptr']} desc_ptr={item['desc_ptr']}",
            file=sys.stderr,
        )


if __name__ == '__main__':
    main()
