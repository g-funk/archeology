Regenerate all three binary data files from their default test-data sources.

Run the following three commands in order, appending any arguments passed to this skill (e.g. `--tokenized`):

```
python3 .claude/skills/items-to-bin/convert.py test-data/items-data-source.md src/Arkeology.Production.Client/data/bin/items.bin [args]
python3 .claude/skills/maps-to-bin/convert.py test-data/maps_data_source.md src/Arkeology.Production.Client/data/bin/maps.bin [args]
python3 .claude/skills/collections-to-bin/convert.py test-data/collections-data-source.md src/Arkeology.Production.Client/data/bin/collections.bin [args]
```

Report the "Wrote N bytes → path" line from each run. If any command fails, show the error and stop.
