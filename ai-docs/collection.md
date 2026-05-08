# Collection

Fragments are multi-tile shapes hidden beneath the dig grid. The player digs to expose them, and once an entire shape is exposed, clicking any of its cells collects the fragment into a side panel.

> **Design source:** `DESIGN.md` §3.3 Constraints (multi-tile rules) + §4 Artifact System
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
| `Cells` | `IReadOnlyList<Vector2I>` | Absolute grid coordinates of each cell |

Templates are looked up via `Fragment.Template(shape)` whenever the relative shape is needed (e.g. when drawing in the side panel).

### Grid overlays

Tracked alongside the cover arrays in `Grid`:

- `Fragment?[,] _fragmentAt` — per-cell pointer to the owning fragment, or `null`
- `List<Fragment> _fragments` — fragments still on the grid
- `List<Fragment> _collectedFragments` — fragments the player has collected (drives the side panel)
- `int FragmentsCollected` — count, also exposed via the `FragmentsChanged(int count)` signal

---

## Spawning

`Grid.SpawnFragments(rng, target)` runs after the terrain is generated:

1. Pick a random shape and a random anchor cell.
2. Compute absolute cells via `Fragment.Template(shape)`.
3. Reject if any cell is out of bounds or already occupied by another fragment.
4. On success, assign a new `Id`, append to `_fragments`, and write the reference into `_fragmentAt`.

Loops until `_fragments.Count == FragmentTarget` (default 6) or 500 attempts have been spent.

---

## Lifecycle of a fragment cell

| State | Trigger | Color |
|---|---|---|
| Buried | cover present, no 4-neighbor cleared | matches the cover (camouflaged) |
| Hinted | cover present, at least one 4-neighbor cleared | muted ochre `(0.60, 0.50, 0.28)` |
| Exposed (partial) | own cover cleared; *some* sibling cells of the same fragment still covered | standard gold `(1.00, 0.82, 0.32)` |
| Exposed (full) | own cover cleared **and** all sibling cells cleared | bright pale gold `(1.00, 0.92, 0.55)` — collectable |
| Collected | player clicked any cell of a fully-exposed fragment | cell becomes plain `Empty`; fragment moved to `_collectedFragments` |

The "hint" propagates through the shape naturally: dig one cell of a fragment → its in-shape neighbors now have a cleared neighbor → they go ochre too.

`HasClearedNeighbor(x, y)` (in `Grid.cs`) drives the hint check; `IsFragmentFullyExposed(frag)` drives the bright/collectable color and the collection gate.

---

## Collection rules

`Grid.HandleClick(cell)` (see [ai-docs/excavation.md](excavation.md)) calls `TryCollectFragment(cell)` first:

1. If the clicked cell has no fragment → return false (falls through to `Dig`).
2. If the fragment isn't fully exposed → return false.
3. Otherwise: null out every cell in `_fragmentAt`, remove the fragment from `_fragments`, append it to `_collectedFragments`, increment `FragmentsCollected`, emit `FragmentsChanged`, and `QueueRedraw`.

Clicking a partial-exposed fragment cell is a no-op (the click silently does nothing — `TryCollectFragment` returns false and `Dig` is a no-op on `Empty`).

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

- The fragment overlay rides on the same per-cell arrays. Clearing a cell's *cover* is what reveals or fully exposes the fragment cell beneath.
- The hint state uses the dig system's "neighbor is `Empty`" check.
- The single-click entry point (`HandleClick`) is shared — collection is just the first branch.

---

## Out of scope (for this feature)

- Player interpretation / labels (CLAUDE.md step 7)
- Fragment matching hints (CLAUDE.md step 8)
- Artifact grouping (multiple fragments composing one artifact, `DESIGN.md` §4.1)
- Semantic tags / hidden meaning
- Persistence across sessions

These belong to later iterations.
