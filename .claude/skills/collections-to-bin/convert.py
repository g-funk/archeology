#!/usr/bin/env python3
"""Convert collections source data (.md) to binary format (design/features/COLLECTIONS.md + CONFIG.md).

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
  [collection data]
    uint16  collection_count
    for each collection:
      uint16  id
      uint16  name_ptr       (token ID if <20000, else token list index + 20000)
      uint8   difficulty
      uint8   shelf_count
      for each shelf:
        uint8   item_count
        int32   item_ids[item_count]

String pointers: value < 20000 is a single token ID; >= 20000 means (value - 20000) is
an index into the token list table.  See CONFIG_STRINGS.md.
"""
import argparse
import os
import struct
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
from lib.tokens import load_predefined, Tokenizer, encode_header

VERSION = (1, 0)


def parse_collections(text):
    collections = []
    for block in text.split('---'):
        raw   = [l.strip() for l in block.strip().splitlines()]
        lines = [l for l in raw if l and not l.startswith('#')]
        if not lines:
            continue

        c = {'shelves': []}
        i = 0
        while i < len(lines):
            line = lines[i]

            if line == 'shelves:':
                i += 1
                cur_shelf = None
                while i < len(lines) and '=' in lines[i]:
                    k, _, v = lines[i].partition('=')
                    k, v = k.strip(), v.strip()
                    if k == 'itemcount':
                        cur_shelf = {'item_count': int(v), 'items': []}
                        c['shelves'].append(cur_shelf)
                    elif k == 'items' and cur_shelf is not None:
                        for part in v.split(','):
                            part = part.strip()
                            if part:
                                cur_shelf['items'].append(int(part))
                    i += 1
                continue

            if '=' in line:
                k, _, v = line.partition('=')
                k, v = k.strip(), v.strip()
                if k == 'id':         c['id']         = int(v)
                elif k == 'name':     c['name']       = v
                elif k == 'difficulty': c['difficulty'] = int(v)
            i += 1

        if 'id' not in c:
            continue
        c.setdefault('name',       '')
        c.setdefault('difficulty', 0)
        collections.append(c)

    return collections


def encode(collections, tokenizer):
    for c in collections:
        c['name_ptr'] = tokenizer.tokenize(c['name'])

    buf  = bytearray()
    buf += encode_header(*VERSION)
    buf += tokenizer.encode_tables()

    buf += struct.pack('<H', len(collections))
    for c in collections:
        buf += struct.pack('<H', c['id'])
        buf += struct.pack('<H', c['name_ptr'])
        buf += struct.pack('<B', c['difficulty'])

        shelves = c['shelves']
        buf += struct.pack('<B', len(shelves))
        for shelf in shelves:
            items = shelf['items']
            buf += struct.pack('<B', len(items))
            for item_id in items:
                buf += struct.pack('<i', item_id)

    return bytes(buf)


def hex_dump(data):
    for off in range(0, len(data), 16):
        chunk    = data[off:off + 16]
        hex_part = ' '.join(f'{b:02x}' for b in chunk)
        asc_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
        print(f'{off:04x}  {hex_part:<47}  {asc_part}')


def main():
    parser = argparse.ArgumentParser(description='Convert collections source data to binary config.')
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
    collections      = parse_collections(text)
    data             = encode(collections, tokenizer)

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
        f"{len(collections)} collection(s):",
        file=sys.stderr,
    )
    for c in collections:
        shelf_summary = ', '.join(
            f"[{' '.join(str(x) for x in s['items'])}]" for s in c['shelves']
        )
        print(
            f"  id={c['id']} diff={c['difficulty']} '{c['name']}' "
            f"{len(c['shelves'])} shelf(ves): {shelf_summary} "
            f"name_ptr={c['name_ptr']}",
            file=sys.stderr,
        )


if __name__ == '__main__':
    main()
