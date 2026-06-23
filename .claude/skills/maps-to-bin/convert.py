#!/usr/bin/env python3
"""Convert maps source data (.md) to binary format (design/features/MAPS.md + CONFIG.md).

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
  [map data]
    uint16  map_count
    for each map:
      uint16  id
      uint8   width
      uint8   height
      uint16  name_ptr       (token ID if <20000, else token list index + 20000)
      uint16  desc_ptr
      uint8   layer_count
      for each layer:
        uint8  info_byte     (0=random, 1=data provided)
        if info_byte==1: width*height bytes of tile data
      uint8   shape_count
      for each shape (BMI):
        uint16  item_id
        uint8   layer
        uint8   x
        uint8   y
      uint8   scrap_count

String pointers: value < 20000 is a single token ID; >= 20000 means (value - 20000) is
an index into the token list table.  See CONFIG_STRINGS.md.
"""
import os
import struct
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
from lib.tokens import load_predefined, Tokenizer, encode_header

VERSION = (1, 0)


def parse_maps(text):
    maps = []
    for block in text.split('---'):
        raw   = [l.strip() for l in block.strip().splitlines()]
        lines = [l for l in raw if l and not l.startswith('#')]
        if not lines:
            continue

        m = {'layers': [], 'shapes': [], 'scraps': 0}
        i = 0
        while i < len(lines):
            line = lines[i]

            if line.endswith(':'):
                section = line[:-1].strip()
                i += 1
                if section == 'layers':
                    while i < len(lines) and lines[i].startswith('li='):
                        m['layers'].append(int(lines[i][3:]))
                        i += 1
                elif section == 'shapes':
                    cur = {}
                    while i < len(lines) and '=' in lines[i]:
                        k, _, v = lines[i].partition('=')
                        k = k.strip()
                        if k not in ('id', 'l', 'x', 'y'):
                            break
                        cur[k] = int(v.strip())
                        if len(cur) == 4:
                            m['shapes'].append(cur)
                            cur = {}
                        i += 1
                continue

            if ':' in line and not line.endswith(':'):
                k, _, v = line.partition(':')
                if k.strip() == 'scraps':
                    m['scraps'] = int(v.strip())
                i += 1
                continue

            if '=' in line:
                k, _, v = line.partition('=')
                k, v = k.strip(), v.strip()
                if k == 'id':            m['id'] = int(v)
                elif k == 'w':           m['w']  = int(v)
                elif k == 'h':           m['h']  = int(v)
                elif k == 'name':        m['name']        = v
                elif k == 'description': m['description'] = v
            i += 1

        if 'id' not in m:
            continue
        m.setdefault('name',        '')
        m.setdefault('description', '')
        m.setdefault('w', 0)
        m.setdefault('h', 0)
        maps.append(m)

    return maps


def encode(maps, tokenizer):
    for m in maps:
        m['name_ptr'] = tokenizer.tokenize(m['name'])
        m['desc_ptr'] = tokenizer.tokenize(m['description'])

    buf  = bytearray()
    buf += encode_header(*VERSION)
    buf += tokenizer.encode_tables()

    buf += struct.pack('<H', len(maps))
    for m in maps:
        buf += struct.pack('<H', m['id'])
        buf += struct.pack('<B', m['w'])
        buf += struct.pack('<B', m['h'])
        buf += struct.pack('<H', m['name_ptr'])
        buf += struct.pack('<H', m['desc_ptr'])

        layers = m['layers']
        buf += struct.pack('<B', len(layers))
        for info in layers:
            buf += struct.pack('<B', info)
            if info == 1:
                buf += bytes(m['w'] * m['h'])

        shapes = m['shapes']
        buf += struct.pack('<B', len(shapes))
        for s in shapes:
            buf += struct.pack('<H', s['id'])
            buf += struct.pack('<B', s['l'])
            buf += struct.pack('<B', s['x'])
            buf += struct.pack('<B', s['y'])

        buf += struct.pack('<B', m['scraps'])

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
    maps             = parse_maps(text)
    data             = encode(maps, tokenizer)

    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as f:
            f.write(data)
        print(f"Wrote {len(data)} bytes → {sys.argv[2]}")
    else:
        hex_dump(data)

    print(
        f"{tokenizer.user_token_count} user token(s), "
        f"{tokenizer.token_list_count} token list(s), "
        f"{len(maps)} map(s):",
        file=sys.stderr,
    )
    for m in maps:
        print(
            f"  id={m['id']} '{m['name']}' {m['w']}x{m['h']} "
            f"{len(m['layers'])} layer(s) {len(m['shapes'])} shape(s) scraps={m['scraps']} "
            f"name_ptr={m['name_ptr']} desc_ptr={m['desc_ptr']}",
            file=sys.stderr,
        )


if __name__ == '__main__':
    main()
