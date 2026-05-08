# Excavation

The dig mechanic — the player clicks tiles in a 2D grid; each click damages a tile's cover until it clears.

> **Design source:** `DESIGN.md` §3 Excavation System
> **Process rules:** `CLAUDE.md` "Excavation System"
> **Layered feature:** [ai-docs/collection.md](collection.md) — fragments are an overlay on top of this grid

---

## Files

| Path | Role |
|---|---|
| `src/Archeology.Prototype/scripts/grid/TileType.cs` | `TileType` enum: `Empty`, `Soil`, `Stone` |
| `src/Archeology.Prototype/scripts/grid/Grid.cs` | Terrain grid, dig logic, drawing |
| `src/Archeology.Prototype/scripts/excavation/ExcavationSystem.cs` | Mouse input → forwards to `Grid.HandleClick` |
| `src/Archeology.Prototype/scripts/player/CameraController.cs` | `Camera2D` that auto-fits the grid into the visible region |
| `src/Archeology.Prototype/scenes/Main.tscn` | Grid (Node2D) + ExcavationSystem (Node) + Camera (Camera2D) |

---

## Tile model

Each cell in the grid has:

- `TileType _types[x, y]` — the cover type (`Soil`, `Stone`, or `Empty` once cleared)
- `int _hp[x, y]` — remaining hit points of the cover

| Cover | Initial HP | Clears in |
|---|---|---|
| `Soil` | 1 | 1 dig |
| `Stone` | 2 | 2 digs |
| `Empty` | — | already cleared |

Stone visibly cracks at 1 HP (lighter slate gray) before clearing.

---

## Click flow

```
mouse click
  └─> ExcavationSystem._UnhandledInput
        └─> Grid.HandleClick(cell)
              ├─> Grid.TryCollectFragment(cell)   ← see ai-docs/collection.md
              │     (returns true if a fragment was collected → done)
              └─> Grid.Dig(cell)                  ← if no fragment was collected
                    - returns immediately if cell is Empty
                    - decrements _hp[cell]
                    - sets _types[cell] = Empty when _hp ≤ 0
```

`HandleClick` is the single entry point. The fragment-collection branch lives in `Grid.TryCollectFragment` and is documented in [ai-docs/collection.md](collection.md). When no fragment is collected, the click falls through to `Dig`.

---

## Generation

`Grid.Generate()` allocates the arrays and fills the terrain via a seeded `Random`:

| Outcome | Probability |
|---|---|
| `Stone` (HP 2) | 32% |
| `Soil` (HP 1) | 68% |

Then `SpawnFragments` overlays multi-tile fragments on top — see [ai-docs/collection.md](collection.md).

The same seed always produces the same map, so iteration is reproducible.

---

## Rendering

`Grid._Draw` redraws the entire grid every time `QueueRedraw` is invoked (after every dig / collection). For each cell, `ColorFor(x, y)` picks a color from the table below.

| State | Color |
|---|---|
| `Empty`, no fragment overlay | near-black brown `(0.10, 0.08, 0.07)` |
| `Soil`, no hint | warm earthy brown `(0.42, 0.30, 0.18)` |
| `Stone` full HP | dark slate gray `(0.42, 0.42, 0.48)` |
| `Stone` cracked (1 HP) | light slate gray `(0.58, 0.58, 0.64)` |

Fragment-related states (ochre hint, gold exposed, pale gold fully-exposed) are documented in [ai-docs/collection.md](collection.md).

---

## Layout

| Property | Value | Notes |
|---|---|---|
| Viewport | 1280×720 | from `project.godot` |
| Grid origin | `(40, 80)` | set on the `Grid` Node2D in `Main.tscn` |
| Default `Width` × `Height` | 28 × 16 | Exported on `Grid` |
| Default `TileSize` | 36 px | Exported on `Grid` |

The grid pixel extent (`Width * TileSize` × `Height * TileSize`) is **not** clamped to the viewport. Without a camera, larger grids would extend past the visible area.

### Camera (`CameraController`)

`Camera2D` attached to `Main/Camera`. On `_Ready` it calls `FitGrid` and subscribes to `Viewport.SizeChanged` so the grid refits whenever the viewport resizes (project setting change, window resize). `FitGrid`:

1. Computes the visible region not occupied by the HUD bar (top) or the collection panel (right): `(Margin, HudTopHeight) → (viewport.x - SidePanelWidth, viewport.y - Margin)`.
2. Picks `zoom = min(availableW / gridW, availableH / gridH)`, clamped to `[MinZoom, MaxZoom]`.
3. Offsets `Position` so the grid centers on the *available* region rather than the full viewport — the panel and HUD don't cover the grid.

This means changing `Grid.Width`, `Grid.Height`, or `Grid.TileSize` and rerunning automatically refits the view: a 100×100 grid zooms out, a 10×10 grid zooms in. Click coordinates remain correct because `ExcavationSystem` already routes through `GetGlobalMousePosition()` when a `Camera2D` is active.

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

---

## Out of scope (for this feature)

- Multiple depth layers (`DESIGN.md` §3.1 mentions 5–20 layers — prototype is single-layer)
- Material variations beyond soil/stone
- Tools / dig-radius modifiers
- Animation on dig

These belong to later iterations.
