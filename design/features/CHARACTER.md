# Character

A man-like figure — the archaeologist — visible on the grid.

- Starts on the middle tile
- When the player clicks any tile, the character walks toward that tile — quickly, but not instantly.
- Clicks still drive the existing dig / collect behavior; this first phase is purely visual. Future phases may tie digging to character position.

## Keyboard Commands

- S - Scan (implemented elsewhere)
- D - Autodig, digs all 9 tiles around and under the character one by one. The dig animation is triggered for each tile.
- C - Collect (if collectable)
- Arrows - move one tile at a time when pressed. Holding down continues movement

### Autodig Details

- The tile order: under the character first, then the ring **anti-clockwise** starting from east (E → NE → N → NW → W → SW → S → SE)
- If the tile being digged is stone, does the required amount of passes to dig it
- Random collapse is not in effect when autodigging
- Movement is not available while digging is happening
- Even when a tile cannot be digged, it should flash an indicated that it is being attempted
