# Config

The config supports a few primitive/basic types, strings, and some specific types. This is not a general-purpose format, but specific for this game. It's a binary transport format. The human-readable data will be another format (TBD), from which the binary config is produced

## Supported primitive/basic types

* byte: 1 byte
* int: 4 bytes
* ushort: 2 bytes
* long: 8 bytes
* string: ushort - points to a string table index

### Strings

Strings are stored to a string table. The string table contains

1. string count
2...N the string types

Each string is

1. string length: ushort
2...N string as UTF-8 bytes

In the config data, a single ushort is stored to point to an index in the string table

## Config Header

The config file starts with a header

1. version major: byte
2. version minor: byte
3. build time epoch: long
4. string table
5. maps: see MAPS.md
6. collections: see COLLECTIONS.md
