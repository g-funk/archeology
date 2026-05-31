# Config

The config supports a few primitive/basic types, strings, and some specific types. This is not a general-purpose format, but specific for this game. It's a binary transport format. The human-readable data will be another format (TBD), from which the binary config is produced.

## Supported primitive/basic types

* byte: 1 byte
* int: 4 bytes
* ushort: 2 bytes
* long: 8 bytes
* string: ushort — points to a token index, or to a token list index

### Strings

Strings are stored as tokens and token lists. A ushort string pointer < 20000 points into the token table (a single-token string); a pointer >= 20000 points into the token list table (a multi-token string).

#### Token ID ranges

```
0    –  999   predefined, no-space  — hardcoded in encoder/decoder; not stored in binary
1000 – 1999   predefined, normal    — hardcoded in encoder/decoder; not stored in binary
2000 – 19999  user tokens           — stored in binary token table
≥ 20000       token list pointer    — indexes into the token list table
```

Predefined tokens are a versioned contract between encoder and decoder — they are never written to the binary file. Changing the predefined set requires a version bump.

#### Implicit space rule

When rendering a token list, a space is inserted before each token — except when the token's ID is in 0–999 (predefined no-space). No-space tokens attach directly to the preceding token without a leading space.

#### Predefined tokens

No-space (0–999): punctuation that attaches to the preceding word — e.g. `.` `,` `!` `?` `:` `;` `)`

Normal (1000–1999): common words and whitespace — e.g. `the` `a` `an` `The` `A` `An` `\n`

No compound tokens like `'. '` or `', '` are needed. The space after punctuation is provided by the implicit space before the next token.

#### Encoder responsibility

The encoder decides token granularity based on context:
- Punctuation separating words → predefined no-space token (e.g. `end.` → `end` + `.`)
- Punctuation embedded within a word or identifier → encode the whole unit as a single user token (e.g. `major.minor`, `e.g.`, `v1.2` → each a single token)

#### Tokens

User tokens (IDs 2000–19999) are stored in the token table in the binary. The token table contains:

1. Token count: ushort (number of user tokens only; predefined tokens are not counted)
2...N Token data, one per user token:
   1. Token length: byte (max 255 bytes)
   2. Token string: UTF-8 bytes

#### Token Lists

Token lists store strings composed of more than one token. The token list table contains:

1. Token list count: ushort
2...N Token list data:
   1. Token count: byte (max 255; increase if needed)
   2...N Token pointers: ushort (each is a token ID or token list index)

#### Example

Input: `"This, is an example. The next part."`

Predefined tokens used: `,` (ID 2), `.` (ID 3), `The` (ID 1000)

User tokens assigned: `This` (2000), `is` (2001), `an` (2002), `example` (2003), `next` (2004), `part` (2005)

Token list: `2000, 2, 2001, 2002, 2003, 3, 1000, 2004, 2005, 3`

Decoded step by step:
- `2000` This → "This"
- `2`   `,` (no-space) → "This,"
- `2001` is → "This, is"
- `2002` an → "This, is an"
- `2003` example → "This, is an example"
- `3`   `.` (no-space) → "This, is an example."
- `1000` The (normal) → "This, is an example. The"
- `2004` next → "This, is an example. The next"
- `2005` part → "This, is an example. The next part"
- `3`   `.` (no-space) → "This, is an example. The next part."

## Config Header

The config file starts with a header:

1. version major: byte
2. version minor: byte
3. build time epoch: long
4. string table
5. maps: see MAPS.md
6. collections: see COLLECTIONS.md
