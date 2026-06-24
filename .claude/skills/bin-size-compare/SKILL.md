Compare naive vs tokenized byte sizes for all three binary converters using test-data source files.

Run all six variants, using `/dev/null` as the output path so nothing is written to disk but byte counts are still reported:

```
python3 .claude/skills/items-to-bin/convert.py test-data/items-data-source.md /dev/null
python3 .claude/skills/items-to-bin/convert.py test-data/items-data-source.md /dev/null --tokenized
python3 .claude/skills/maps-to-bin/convert.py test-data/maps_data_source.md /dev/null
python3 .claude/skills/maps-to-bin/convert.py test-data/maps_data_source.md /dev/null --tokenized
python3 .claude/skills/collections-to-bin/convert.py test-data/collections-data-source.md /dev/null
python3 .claude/skills/collections-to-bin/convert.py test-data/collections-data-source.md /dev/null --tokenized
```

Parse the "Wrote N bytes" line from each run and present a comparison table:

| File | naive | tokenized | diff | % |
|------|-------|-----------|------|---|
| items.bin       | N | N | ±N | ±X% |
| maps.bin        | N | N | ±N | ±X% |
| collections.bin | N | N | ±N | ±X% |

Show positive diff as `+N (+X%)` and negative as `-N (-X%)`.
