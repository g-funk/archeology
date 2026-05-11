# Collection

Fragments are multi-tile shapes hidden beneath the dig grid. The player digs to expose them, and once an entire shape is exposed, clicking any of its cells collects the fragment into a side panel.

> **Design source:** `design/DESIGN.md` §3.3 Constraints (multi-tile rules) + §4 Artifact System
> **Process rules:** `CLAUDE.md` "Fragments"
> **Underlying mechanic:** [ai-docs/excavation.md](excavation.md) — the dig system this is layered on

---

## Files

| Path | Role |
|---|---|
| `src/Archeology.Prototype/scripts/artifacts/Fragment.cs` | `Fragment` class, `FragmentShape` enum, `Template(...)` lookup |
| `src/Archeology.Prototype/scripts/grid/Grid.cs` | Fragment overlay (`_fragmentAt`, `_fragments`, `_collectedFragments`), spawn, collect, hint logic, fragment colors |
| `src/Archeology.Prototype/scripts/ui/CollectionPanel.cs` | Side panel UI: header + vertical slot list |
| `src/Archeology.Prototype/scenes/Main.tscn` | `CollectionPanel` node under the HUD |

---

## Data model

### `FragmentShape` (enum)

Four prototype shapes. `Fragment.Template(shape)` returns the cell offsets relative to a top-left anchor:

| Shape | Tiles | Layout (`X` = filled, `.` = empty) |
|---|---|---|
| `SquareTwo` | 4 | `X X` / `X X` |
| `BoxThree` | 8 | `X X X` / `X . X` / `X X X` |
| `Plus` | 5 | `. X .` / `X X X` / `. X .` |
| `Corner` | 5 | `X X X` / `X . .` / `X . .` |

### `Fragment` (class)

| Member | Type | Description |
|---|---|---|
| `Id` | `int` | Sequential id assigned at spawn |
| `Shape` | `FragmentShape` | Which template was used |
| `Depth` | `int` | Layer the whole shape sits on. Always ≥ 1 (never on the topmost layer) and < `LayerCount` |
| `Cells` | `IReadOnlyList<Vector2I>` | Absolute `(x, y)` grid coordinates of each cell (all at the same depth) |

Templates are looked up via `Fragment.Template(shape)` whenever the relative shape is needed (e.g. when drawing in the side panel).

### Grid overlays

Tracked alongside the layered terrain arrays in `Grid`:

- `Fragment?[,,] _fragmentAt` — per-`(x, y, depth)` pointer to the owning fragment, or `null`
- `List<Fragment> _fragments` — fragments still on the grid
- `List<Fragment> _collectedFragments` — fragments the player has collected (drives the side panel)
- `int FragmentsCollected` — count, also exposed via the `FragmentsChanged(int count)` signal

---

## Spawning

`Grid.SpawnFragments(rng, target)` runs after the terrain is generated:

1. Pick a random shape, a random anchor cell, and a random depth in `[1, LayerCount)` — never depth 0.
2. Compute absolute cells via `Fragment.Template(shape)`.
3. Reject if any cell is out of bounds or already occupied by another fragment **at that depth** (`_fragmentAt[x, y, depth] != null`). Fragments at different depths at the same `(x, y)` are allowed.
4. On success, assign a new `Id`, append to `_fragments`, and write the reference into `_fragmentAt[x, y, depth]`.

Loops until `_fragments.Count == FragmentTarget` (default 6) or 500 attempts have been spent.

---

## Lifecycle of a fragment cell

A fragment cell at `(x, y, Depth)` evolves with the tile's `_depth[x, y]` and what its neighbors have done:

| State | Condition | Color |
|---|---|---|
| Buried (hidden) | `_depth[x, y] < Depth` AND no neighbor's `_depth > Depth` | the floor's normal layer color (camouflaged) |
| Hinted | `_depth[x, y] < Depth` AND some neighbor's `_depth > Depth` (the wall would expose the layer) | muted ochre `(0.60, 0.50, 0.28)` × depth-darken |
| Exposed (partial) | `_depth[x, y] == Depth` AND some sibling cell of this fragment is still buried | standard gold `(1.00, 0.82, 0.32)` × depth-darken |
| Exposed (full) | every cell of the fragment has `_depth == Depth` | bright pale gold `(1.00, 0.92, 0.55)` × depth-darken — collectable |
| Collected | player clicked any cell of a fully-exposed fragment | every cell's `_depth` advances to `Depth + 1`; fragment moved to `_collectedFragments` |

All fragment colors are passed through the same depth-darkening factor as the surrounding floors (see [ai-docs/excavation.md](excavation.md#floor-colors)).

`AnyNeighborDeeperThan(x, y, Depth)` (in `Grid.cs`) drives the hint check; `IsFragmentFullyExposed(frag)` drives the bright/collectable color and the collection gate.

---

## Collection rules

`Grid.HandleClick(cell)` (see [ai-docs/excavation.md](excavation.md)):

1. If `_fragmentAt[x, y, _depth[x, y]] != null` → call `TryCollectFragment(cell)` and return. The fragment **blocks** further digging at this tile; the click can't fall through to `Dig`.
2. Otherwise → call `Dig(cell)` (subject to the step constraint).

`TryCollectFragment`:

1. No fragment at this tile's current depth → `false`.
2. Fragment present but not every cell is at its depth (`IsFragmentFullyExposed` false) → `false` (the click is a silent no-op).
3. Otherwise: for every cell, clear `_fragmentAt[cell, Depth]` and set `_depth[cell] = Depth + 1`. Remove from `_fragments`, append to `_collectedFragments`, increment `FragmentsCollected`, emit `FragmentsChanged`, `QueueRedraw`.

---

## Side panel UI (`CollectionPanel`)

- **Node:** `Control` under `HUD/CollectionPanel` in `Main.tscn`. Initial offsets in the scene are placeholders — the script repositions the panel every frame.
- **Anchoring to the grid:** in `_Process`, `FollowGrid()` projects the grid's top-right and bottom-right corners through `Viewport.GetCanvasTransform()` (which accounts for `Camera2D` zoom/position) and sets:
  - `Position = (gridTopRight.viewportX + GapFromGrid, gridTopRight.viewportY)`
  - `Size = (PanelWidth, gridBottomRight.viewportY - gridTopRight.viewportY)`
  This makes the panel stick to the grid's top-right corner and match its visible height regardless of viewport size, camera zoom, or grid dimensions.
- **Hookup:** subscribes to `Grid.FragmentsChanged` and calls `QueueRedraw` on every emit.
- **Drawing:**
  - Background fill (slightly lighter than the page) over the whole panel.
  - Header text "Fragments: N" using the theme's default font.
  - Below the header, a vertical stack of slots in `_collectedFragments` order. Each slot:
    - Dark inset background.
    - Mini-tile rendering of the shape: each fragment cell drawn as a small gold square at `CellSize`, separated by `CellSpacing`. The shape preserves its template layout (a `SquareTwo` looks visibly smaller than a `BoxThree` inside the same slot).
    - Same gold as an exposed grid tile, so the panel and grid read as the same material.
- **No interaction yet** — slots are purely visual. Click handling will be wired when interpretation lands (CLAUDE.md implementation step 7).

### Tunables (exported on `CollectionPanel`)

- `Padding` — outer panel padding
- `HeaderHeight` — vertical space for the counter text
- `SlotHeight` — height of each slot row
- `SlotSpacing` — gap between slots
- `CellSize` — size of one fragment-cell square inside the slot
- `CellSpacing` — gap between fragment cells
- `GapFromGrid` — pixel gap between the grid's right edge and the panel's left edge
- `PanelWidth` — fixed width of the panel in viewport pixels
- `GridPath` — node path to the `Grid` (defaults to `../../Grid`)

> **Layout coupling:** the camera's `SidePanelWidth` (in `CameraController`) reserves space on the right that the grid won't extend into. Keep `SidePanelWidth ≥ PanelWidth + GapFromGrid` so the panel doesn't overlap the grid. See [ai-docs/excavation.md](excavation.md#camera-cameracontroller).

---

## Cross-feature seam

This feature sits on top of [ai-docs/excavation.md](excavation.md):

- The fragment overlay rides on the same per-`(x, y, depth)` arrays as the layered terrain. Advancing a tile's `_depth` is what reveals, fully exposes, or hints at fragment cells.
- The hint state uses `AnyNeighborDeeperThan(x, y, frag.Depth)` — the wall-exposure check.
- The single-click entry point (`HandleClick`) is shared — collection is the priority branch when a fragment cell is at the current depth.
- Collection ends by **advancing depth past the fragment** (`_depth = Depth + 1`), which interacts with the step constraint just like a regular dig would.

---

## Out of scope (for this feature)

- Player interpretation / labels (CLAUDE.md step 7)
- Fragment matching hints (CLAUDE.md step 8)
- Artifact grouping (multiple fragments composing one artifact, `design/DESIGN.md` §4.1)
- Semantic tags / hidden meaning
- Persistence across sessions

These belong to later iterations.
