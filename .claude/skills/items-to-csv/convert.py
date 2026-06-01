#!/usr/bin/env python3
import csv
import sys

def is_shape_row(line):
    stripped = line.strip()
    return bool(stripped) and all(c in '01' for c in stripped)

def parse_items(text):
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
                if key == 'id':
                    item['id'] = value.strip()
                elif key == 'r':
                    item['rarity'] = value.strip()
                elif key == 'name':
                    item['name'] = value.strip()
                elif key == 'description':
                    item['description'] = value.strip()
                elif key == 'p':
                    item['parts'] = value.strip()

        if shape_rows:
            h = len(shape_rows)
            w = max(len(r) for r in shape_rows)
            item['shape_w'] = w
            item['shape_h'] = h
            item['shape_data'] = ''.join(r.ljust(w, '0') for r in shape_rows)

        if 'id' in item:
            item.setdefault('rarity', 'common')
            items.append(item)

    return items

def main():
    if len(sys.argv) < 2:
        print("Usage: convert.py <input_file> [output_file]", file=sys.stderr)
        sys.exit(1)

    with open(sys.argv[1], encoding='utf-8') as f:
        text = f.read()

    items = parse_items(text)

    out = open(sys.argv[2], 'w', newline='', encoding='utf-8') if len(sys.argv) > 2 else sys.stdout
    try:
        writer = csv.writer(out)
        writer.writerow(['id', 'rarity', 'name', 'description', 'shape w', 'shape h', 'shape data', 'parts'])
        for item in items:
            writer.writerow([
                item.get('id', ''),
                item.get('rarity', ''),
                item.get('name', ''),
                item.get('description', ''),
                item.get('shape_w', ''),
                item.get('shape_h', ''),
                item.get('shape_data', ''),
                item.get('parts', ''),
            ])
    finally:
        if len(sys.argv) > 2:
            out.close()

if __name__ == '__main__':
    main()
