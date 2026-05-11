# Excavation

The dig mechanic — the player clicks tiles in a 2D grid; each click drains the current layer's HP, and when the layer clears the tile drops to the next depth. The world is layered; players must terrace down step by step.

> **Design source:** `design/DESIGN.md` §3 Excavation System
> **Visual rules:** `design/VISUALS.md` (floor darkening, walls between depth steps)
> **Process rules:** `CLAUDE.md` "Excavation System"
> **Layered feature:** [ai-docs/collection.md](collection.md) — fragments are an overlay on top of this grid

---

## Files

| Path | Role |
|---|---|
| `src/Archeology.Prototype/scripts/grid/TileType.cs` | `TileType` enum: `Empty`, `Soil`, `Stone` |
| `src/Archeology.Prototype/scripts/grid/Grid.cs` | Layered terrain, dig logic, drawing (floors + walls) |
| `src/Archeology.Prototype/scripts/excavation/ExcavationSystem.cs` | Mouse input → forwards to `Grid.HandleClick` |
| `src/Archeology.Prototype/scripts/player/CameraController.cs` | `Camera2D` that auto-fits the grid into the visible region |
| `src/Archeology.Prototype/scenes/Main.tscn` | Grid (Node2D) + ExcavationSystem (Node) + Camera (Camera2D) |

---

## Layered tile model

Each cell carries a stack of layers plus a single "current depth" pointer:

- `TileType _layerTypes[x, y, d]` — material at depth `d`
- `int _layerHp[x, y, d]` — HP of the material at depth `d`
- `int _depth[x, y]` — depth currently visible at this tile

Depths range over `[0, LayerCount)`. Once `_depth == LayerCount` the tile is **bedrock** — no more material remains.

| Material | Initial HP | Clears in |
|---|---|---|
| `Soil` | 1 | 1 dig |
| `Stone` | 2 | 2 digs |

Material distribution is identical at every depth (~32% stone, ~68% soil), rolled once per `(x, y, d)` at generation time. Stone visibly cracks at 1 HP (lighter slate gray) before clearing.

---

## Step constraint ("one deeper than surrounds")

A tile may **advance depth** only when every in-bound 4-neighbor is already at depth ≥ this tile's current depth. Out-of-bounds neighbors don't constrain.

`Grid.CanDigDeeper(x, y)`:

```
return all 4-neighbors n: !InBounds(n) || _depth[n] >= _depth[x, y]
```

If this check fails, `Dig(cell)` is a silent no-op — the player must dig surrounding tiles down first. This forces terraced excavation: a deeper "pit" can only be one step lower than its rim.

---

## Click flow

```
mouse click
  └─> ExcavationSystem._UnhandledInput
        └─> Grid.HandleClick(cell)
              ├─> Grid.TryCollectFragment(cell)   ← see ai-docs/collection.md
              │     (handles the case where a fragment cell is exposed at this tile's depth)
              └─> Grid.Dig(cell)                  ← otherwise
                    - returns if cell is at bedrock
                    - returns if CanDigDeeper is false (step constraint blocks)
                    - decrements _layerHp[x, y, _depth[x, y]]
                    - emits `Dug(x, y, depth)` after the decrement
                    - increments _depth[x, y] when HP drains
```

`HandleClick` is the single entry point. The fragment-collection branch is in `Grid.TryCollectFragment` — see [ai-docs/collection.md](collection.md). The `Dug` signal feeds the ping system — see [ai-docs/ping.md](ping.md).

---

## Rendering

`Grid._Draw` paints in two passes:

1. **Floors** — one `Rect2` per tile via `FloorColorFor(x, y)`.
2. **Walls** — per-tile, on the deeper-than-neighbor edges.

### Floor colors

`FloorColorFor` picks a base color and applies a depth-darkening factor from design/VISUALS.md:

| `_depth[x, y]` | Multiplier |
|---|---|
| 0 | 1.00 |
| 1 | 0.85 |
| 2 | 0.75 |
| 3+ | 0.70 |

Base colors before darkening:

| State | Color |
|---|---|
| Bedrock (`d == LayerCount`) | near-black `(0.05, 0.04, 0.03)` (no darkening applied) |
| Soil | warm earthy brown `(0.42, 0.30, 0.18)` |
| Stone full HP | dark slate gray `(0.42, 0.42, 0.48)` |
| Stone cracked (1 HP) | light slate gray `(0.58, 0.58, 0.64)` |

Fragment-related states (ochre hint, gold exposed, pale gold fully-exposed) are documented in [ai-docs/collection.md](collection.md).

### Walls

`DrawWalls()` per design/VISUALS.md: for each tile, for each of the 4 sides, if this tile is **strictly deeper** than the neighbor on that side, paint a wall on this tile's edge.

| Side | Color | Notes |
|---|---|---|
| Top, Left | dark shadow `(0.04, 0.03, 0.02)` | near edge of the pit, in shadow |
| Right, Bottom | bright highlight `(0.75, 0.75, 0.73)` | far edge of the pit, catching light |

The dark-near / bright-far pairing makes a deeper tile read as a recessed pit.

Wall thickness scales with the depth gap: `unit × (d - nd)`, where `unit = max(2, TileSize / 10)`. A drop of one layer is one unit thick, a drop of two is twice that, etc. No gradient is applied yet — a single flat color per wall.

---

## Layout

| Property | Value | Notes |
|---|---|---|
| Viewport | 1280×720 | from `project.godot` |
| Grid origin | `(40, 80)` | set on the `Grid` Node2D in `Main.tscn` |
| Default `Width` × `Height` | 28 × 16 | Exported on `Grid` |
| Default `TileSize` | 36 px | Exported on `Grid` |
| Default `LayerCount` | 4 | Exported on `Grid` — depths 0..3 plus bedrock |

The grid pixel extent (`Width * TileSize` × `Height * TileSize`) is **not** clamped to the viewport — the camera handles fitting.

### Camera (`CameraController`)

`Camera2D` attached to `Main/Camera`. On `_Ready` it calls `FitGrid` and subscribes to `Viewport.SizeChanged` so the grid refits whenever the viewport resizes (project setting change, window resize). `FitGrid`:

1. Computes the visible region not occupied by the HUD bar (top) or the collection panel (right): `(Margin, HudTopHeight) → (viewport.x - SidePanelWidth, viewport.y - Margin)`.
2. Picks `zoom = min(availableW / gridW, availableH / gridH)`, clamped to `[MinZoom, MaxZoom]`.
3. Offsets `Position` so the grid centers on the *available* region rather than the full viewport — the panel and HUD don't cover the grid.

Changing `Grid.Width`, `Grid.Height`, or `Grid.TileSize` and rerunning automatically refits the view. Click coordinates remain correct because `ExcavationSystem` already routes through `GetGlobalMousePosition()` when a `Camera2D` is active.

The collection panel ([ai-docs/collection.md](collection.md)) reads back the grid's bounds in viewport space each frame and sticks itself to the grid's top-right corner — so changing `SidePanelWidth` here keeps the grid out of the panel's reserved column, and `PanelWidth` on the panel keeps the panel itself within that column. Keep `SidePanelWidth ≥ PanelWidth + GapFromGrid`.

#### Camera tunables (exported on `CameraController`)

- `GridPath` — node path to the `Grid` (defaults to `../Grid`)
- `HudTopHeight` — vertical space reserved for HUD title/instructions (default 80)
- `SidePanelWidth` — horizontal space reserved for the collection panel (default 220)
- `Margin` — outer padding around the grid (default 20)
- `MinZoom` / `MaxZoom` — clamp range for the auto-fit zoom

---

## Tunables (exported on the `Grid` node)

- `Width` — columns
- `Height` — rows
- `TileSize` — pixel size of one tile
- `Seed` — RNG seed (terrain + fragment placement)
- `FragmentTarget` — see [ai-docs/collection.md](collection.md)
- `LayerCount` — number of dig-able layers; depths go `0..LayerCount-1`, then bedrock at `LayerCount`

---

## Out of scope (for this feature)

- Wall vertical gradients (design/VISUALS.md mentions "lighter at top, darker at bottom" — currently flat)
- Material variations beyond soil/stone
- Tools / dig-radius modifiers
- Animation on dig

These belong to later iterations.
