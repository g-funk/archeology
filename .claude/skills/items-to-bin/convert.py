#!/usr/bin/env python3
"""Convert items source data (.md) to binary format as specified in design/features/ITEMS.md.

File layout:
  uint16  string_count
  for each string: uint16 byte_length + UTF-8 bytes
  uint16  item_count
  for each item:
    uint16  id
    uint8   rarity         (common=0, uncommon=1, rare=2, epic=3, legendary=4)
    uint8   parts_count
    uint16  parts_ids[parts_count]
    uint16  name_idx       (index into string table)
    uint16  desc_idx       (index into string table)
    uint8   shape_w
    uint8   shape_h
    bytes   shape_bitmap   (ceil(w*h/8) bytes, MSB first; empty when w*h==0)

All multi-byte integers are little-endian.
Shape bitmap: first cell → bit 7 of byte 0, reading left-to-right top-to-bottom.
"""
import struct
import sys

RARITIES = {'common': 0, 'uncommon': 1, 'rare': 2, 'epic': 3, 'legendary': 4}


def is_shape_row(line):
    s = line.strip()
    return bool(s) and all(c in '01' for c in s)


def shape_to_bitmap(shape_data):
    bits = [int(c) for c in shape_data]
    n_bytes = (len(bits) + 7) // 8
    result = bytearray(n_bytes)
    for i, b in enumerate(bits):
        if b:
            result[i // 8] |= 0x80 >> (i % 8)
    return bytes(result)


def parse_items(text):
    strings = []
    str_index = {}

    def intern_str(s):
        if s not in str_index:
            str_index[s] = len(strings)
            strings.append(s)
        return str_index[s]

    items = []
    for block in text.split('---'):
        lines = block.strip().splitlines()
        if not lines:
            continue

        item = {}
        shape_rows = []
        in_shape = False

        for line in lines:
            stripped = line.strip()
            if not stripped or stripped.startswith('#'):
                continue

            if is_shape_row(line):
                in_shape = True
                shape_rows.append(stripped)
            elif '=' in line and not in_shape:
                key, _, value = line.partition('=')
                key = key.strip()
                value = value.strip()
                if key == 'id':          item['id'] = int(value)
                elif key == 'r':         item['rarity'] = value
                elif key == 'name':      item['name'] = value
                elif key == 'description': item['description'] = value
                elif key == 'p':
                    item['parts'] = [int(x.strip()) for x in value.split(',') if x.strip()]

        if shape_rows:
            h = len(shape_rows)
            w = max(len(r) for r in shape_rows)
            item['shape_w'] = w
            item['shape_h'] = h
            item['shape_data'] = ''.join(r.ljust(w, '0') for r in shape_rows)

        if 'id' not in item:
            continue

        item.setdefault('rarity', 'common')
        item.setdefault('name', '')
        item.setdefault('description', '')
        item.setdefault('parts', [])
        item.setdefault('shape_w', 0)
        item.setdefault('shape_h', 0)
        item.setdefault('shape_data', '')
        item['name_idx'] = intern_str(item['name'])
        item['desc_idx'] = intern_str(item['description'])
        items.append(item)

    return items, strings


def encode(items, strings):
    buf = bytearray()

    buf += struct.pack('<H', len(strings))
    for s in strings:
        encoded = s.encode('utf-8')
        buf += struct.pack('<H', len(encoded))
        buf += encoded

    buf += struct.pack('<H', len(items))
    for item in items:
        buf += struct.pack('<H', item['id'])
        buf += struct.pack('<B', RARITIES.get(item['rarity'], 0))

        parts = item['parts']
        buf += struct.pack('<B', len(parts))
        for pid in parts:
            buf += struct.pack('<H', pid)

        buf += struct.pack('<H', item['name_idx'])
        buf += struct.pack('<H', item['desc_idx'])

        w, h = item['shape_w'], item['shape_h']
        buf += struct.pack('<B', w)
        buf += struct.pack('<B', h)
        if w * h > 0:
            buf += shape_to_bitmap(item['shape_data'])

    return bytes(buf)


def hex_dump(data):
    for off in range(0, len(data), 16):
        chunk = data[off:off + 16]
        hex_part = ' '.join(f'{b:02x}' for b in chunk)
        asc_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
        print(f'{off:04x}  {hex_part:<47}  {asc_part}')


def main():
    if len(sys.argv) < 2:
        print("Usage: convert.py <input_file> [output_file]", file=sys.stderr)
        sys.exit(1)

    with open(sys.argv[1], encoding='utf-8') as f:
        text = f.read()

    items, strings = parse_items(text)
    data = encode(items, strings)

    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as f:
            f.write(data)
        print(f"Wrote {len(data)} bytes → {sys.argv[2]}")
    else:
        hex_dump(data)

    print(f"{len(strings)} string(s), {len(items)} item(s):", file=sys.stderr)
    for item in items:
        parts = item['parts']
        shape = f"{item['shape_w']}x{item['shape_h']}" if not parts else f"parts={parts}"
        print(f"  id={item['id']} r={item['rarity']} '{item['name']}' {shape}", file=sys.stderr)


if __name__ == '__main__':
    main()
