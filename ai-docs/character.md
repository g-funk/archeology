# Character (archaeologist)

A simple stick-figure on the grid that walks toward whichever tile the player clicks. Phase 1 — cosmetic only; clicks continue to drive dig / collect independently.

> **Design source:** [`design/features/CHARACTER.md`](../design/features/CHARACTER.md)
> **Built on:** [ai-docs/excavation.md](excavation.md) — the new `Grid.Clicked` signal

---

## Files

| Path | Role |
|---|---|
| `src/Archeology.Prototype/scripts/player/PlayerCharacter.cs` | `Node2D` figure: initial placement, click-driven target, frame-step movement, custom-drawn body |
| `src/Archeology.Prototype/scripts/grid/Grid.cs` | New `Clicked(int x, int y)` signal emitted from `HandleClick` for every in-bounds click |
| `src/Archeology.Prototype/scenes/Main.tscn` | `PlayerCharacter` Node2D under `Grid`, ordered last so it draws on top of overlays |

---

## Behaviour

- **Initial position:** middle-left tile. On `_Ready`, the character snaps to `(0, Grid.Height / 2)`.
- **Click handling:** subscribes to `Grid.Clicked`. Every in-bounds click sets `_targetPosition` to that tile's centre in grid-local coordinates. Out-of-bounds clicks emit nothing, so the character ignores them.
- **Movement:** `_Process` steps `Position` toward `_targetPosition` by `SpeedTilesPerSecond × TileSize × delta` each frame using `Vector2.MoveToward`. Quick (default 10 tiles/sec) but visibly non-instant.
- **Independence from digging:** the click that moves the character also fires the existing `Dig` / `TryCollectFragment` path inside `Grid.HandleClick`. The character may still be in motion while the dig has already resolved — gating dig on arrival is a future phase.

---

## Drawing

The figure is rendered in `_Draw` relative to local `(0, 0)`. Because the Node2D's `Position` is the local origin in parent space, the same draw commands follow the character automatically — no `QueueRedraw` needed each frame.

Pieces (all in `BodyColor`):

- Head — circle at `~0.16 × TileSize` radius, above the body.
- Body — vertical line from below the head down `~0.40 × TileSize`.
- Arms — two diagonals from just below the shoulders.
- Legs — two diagonals from the body bottom.

Stroke width is `max(2, TileSize × 0.08)` so the figure stays readable at any tile size the user picks.

---

## Tunables (exported on `PlayerCharacter`)

- `GridPath` — node path to the `Grid` (defaults to `..`)
- `SpeedTilesPerSecond` — travel speed; default 10
- `BodyColor` — colour of head/lines; default a light cream `(0.95, 0.92, 0.85)`

---

## Cross-feature seam

- `Grid.Clicked` is the only new addition to `Grid`. It fires at the **top** of `HandleClick` after the in-bounds check — *before* the dig / collect branching — so the character moves toward any click that lands on a real tile, whether the click ends up digging, collecting, or being blocked by a constraint.
- The character is a sibling of `HintsSystem` and `RadarSystem` under `Grid` and placed **last** in the scene order, so its draw composites on top of hint flashes and radar pulses.

---

## Out of scope (for this phase)

- Gating dig / collect on arrival at the clicked tile
- Pathfinding (no walls or obstacles considered — character moves in a straight line)
- Walking animation, idle pose, direction-facing
- Sound
