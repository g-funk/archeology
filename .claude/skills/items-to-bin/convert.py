#!/usr/bin/env python3
"""Convert items source data (.md) to binary format (design/features/ITEMS.md + CONFIG.md).

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
      uint8   shape_w
      uint8   shape_h
      bytes   shape_bitmap   (ceil(w*h/8) bytes, MSB first; omitted when w*h==0)

String pointers: value < 20000 is a single token ID; >= 20000 means (value - 20000) is
an index into the token list table.  See CONFIG_STRINGS.md.
"""
import os
import struct
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
from lib.tokens import load_predefined, Tokenizer, encode_header

RARITIES = {'common': 0, 'uncommon': 1, 'rare': 2, 'epic': 3, 'legendary': 4}
VERSION  = (1, 0)


def is_shape_row(line):
    s = line.strip()
    return bool(s) and all(c in '01' for c in s)


def shape_to_bitmap(shape_data):
    bits    = [int(c) for c in shape_data]
    n_bytes = (len(bits) + 7) // 8
    result  = bytearray(n_bytes)
    for i, b in enumerate(bits):
        if b:
            result[i // 8] |= 0x80 >> (i % 8)
    return bytes(result)


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
                shape_rows.append(stripped)
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
            h = len(shape_rows)
            w = max(len(r) for r in shape_rows)
            item['shape_w']    = w
            item['shape_h']    = h
            item['shape_data'] = ''.join(r.ljust(w, '0') for r in shape_rows)

        if 'id' not in item:
            continue

        item.setdefault('rarity',      'common')
        item.setdefault('name',        '')
        item.setdefault('description', '')
        item.setdefault('parts',       [])
        item.setdefault('shape_w',     0)
        item.setdefault('shape_h',     0)
        item.setdefault('shape_data',  '')
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

        w, h = item['shape_w'], item['shape_h']
        buf += struct.pack('<B', w)
        buf += struct.pack('<B', h)
        if w * h > 0:
            buf += shape_to_bitmap(item['shape_data'])

    return bytes(buf)


def hex_dump(data):
    for off in range(0, len(data), 16):
        chunk    = data[off:off + 16]
        hex_part = ' '.join(f'{b:02x}' for b in chunk)
        asc_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
        print(f'{off:04x}  {hex_part:<47}  {asc_part}')


def main():
    if len(sys.argv) < 2:
        print("Usage: convert.py <input_file> [output_file]", file=sys.stderr)
        sys.exit(1)

    with open(sys.argv[1], encoding='utf-8') as f:
        text = f.read()

    no_space, normal = load_predefined()
    tokenizer        = Tokenizer(no_space, normal)
    items            = parse_items(text)
    data             = encode(items, tokenizer)

    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as f:
            f.write(data)
        print(f"Wrote {len(data)} bytes → {sys.argv[2]}")
    else:
        hex_dump(data)

    print(
        f"{tokenizer.user_token_count} user token(s), "
        f"{tokenizer.token_list_count} token list(s), "
        f"{len(items)} item(s):",
        file=sys.stderr,
    )
    for item in items:
        shape = f"{item['shape_w']}x{item['shape_h']}" if not item['parts'] else f"parts={item['parts']}"
        print(
            f"  id={item['id']} r={item['rarity']} '{item['name']}' {shape} "
            f"name_ptr={item['name_ptr']} desc_ptr={item['desc_ptr']}",
            file=sys.stderr,
        )


if __name__ == '__main__':
    main()
