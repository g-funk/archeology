# Config

The config supports a few primitive/basic types, strings, and some specific types. This is not a general-purpose format, but specific for this game. It's a binary transport format. The human-readable data will be another format (TBD), from which the binary config is produced.

## Supported primitive/basic types

* byte: 1 byte
* int: 4 bytes
* ushort: 2 bytes
* long: 8 bytes
* string: ushort — points to a token index, or to a token list index; see [CONFIG_STRINGS.md](CONFIG_STRINGS.md) for the full encoding spec

## Config Header

The config file starts with a header:

1. version major: byte
2. version minor: byte
3. config build time epoch: long
4. token table
5. token list table

## Data

The follows after the Header. The data is specific to the config in questions. There will be different config files for at least the following:

* Items - see ITEMS.md
* Maps - see MAPS.md