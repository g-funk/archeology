#!/usr/bin/env python3
"""Convert maps_data_source.md to binary format as specified in design/features/MAPS.md.

File layout:
  uint16  string_count
  for each string: uint16 length + UTF-8 bytes
  uint16  map_count
  for each map:
    uint16  id
    uint8   width
    uint8   height
    uint16  name_idx  (index into string table)
    uint16  desc_idx  (index into string table)
    uint8   layer_count
    for each layer:
      uint8  info_byte  (0=random, 1=provided)
      if info_byte==1: width*height bytes of tile data
    uint8   shape_count
    for each shape:
      uint16  item_id
      uint8   layer
      uint8   x
      uint8   y
    uint8   scrap_count
"""
import struct
import sys


def parse_maps(text):
    strings = []
    str_index = {}

    def intern_str(s):
        if s not in str_index:
            str_index[s] = len(strings)
            strings.append(s)
        return str_index[s]

    maps = []
    for block in text.split('---'):
        raw = [l.strip() for l in block.strip().splitlines()]
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
                elif k == 'w':           m['w'] = int(v)
                elif k == 'h':           m['h'] = int(v)
                elif k == 'name':        m['name'] = v
                elif k == 'description': m['description'] = v
            i += 1

        if 'id' not in m:
            continue
        m.setdefault('name', '')
        m.setdefault('description', '')
        m.setdefault('w', 0)
        m.setdefault('h', 0)
        m['name_idx'] = intern_str(m['name'])
        m['desc_idx'] = intern_str(m['description'])
        maps.append(m)

    return maps, strings


def encode(maps, strings):
    buf = bytearray()

    buf += struct.pack('<H', len(strings))
    for s in strings:
        encoded = s.encode('utf-8')
        buf += struct.pack('<H', len(encoded))
        buf += encoded

    buf += struct.pack('<H', len(maps))
    for m in maps:
        buf += struct.pack('<H', m['id'])
        buf += struct.pack('<B', m['w'])
        buf += struct.pack('<B', m['h'])
        buf += struct.pack('<H', m['name_idx'])
        buf += struct.pack('<H', m['desc_idx'])

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

    maps, strings = parse_maps(text)
    data = encode(maps, strings)

    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as f:
            f.write(data)
        print(f"Wrote {len(data)} bytes → {sys.argv[2]}")
    else:
        hex_dump(data)

    print(f"{len(strings)} string(s), {len(maps)} map(s):", file=sys.stderr)
    for m in maps:
        print(
            f"  id={m['id']} '{m['name']}' {m['w']}x{m['h']} "
            f"{len(m['layers'])} layer(s) {len(m['shapes'])} shape(s) scraps={m['scraps']}",
            file=sys.stderr,
        )


if __name__ == '__main__':
    main()
