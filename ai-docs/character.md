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
- **Click handling:** subscribes to `Grid.Clicked` for normal short clicks. Every in-bounds click sets `_targetPosition` to that tile's centre in grid-local coordinates. Out-of-bounds clicks emit nothing, so the character ignores them.
- **Movement:** `_Process` steps `Position` toward `_targetPosition` by `SpeedTilesPerSecond × TileSize × delta` each frame using `Vector2.MoveToward`. Quick (default 10 tiles/sec) but visibly non-instant.
- **Scan API** (used by `ExcavationSystem`):
  - `RequestScanAt(cell)` — sets `_targetPosition` and marks `_scanPendingOnArrival = true`. When the character arrives (now or after walking), `_Process` calls `Grid.TriggerScan(x, y, depth)`. Used for the long-click gesture.
  - `RequestScanHere()` — immediately calls `Grid.TriggerScan(...)` for the character's current tile and that tile's depth, no walking. Used for the `S` key.
- **Independence from digging:** the short click that moves the character also fires `Grid.HandleClick` (dig / collect). The character may still be in motion while the dig has already resolved — gating dig on arrival is a future phase.

---

## Drawing

The figure is rendered in `_Draw` relative to local `(0, 0)`. Because the Node2D's `Position` is the local origin in parent space, position changes follow automatically without redraws — `QueueRedraw` is only called when an animation is in motion (limb positions actually change).

Pieces (all in `BodyColor`):

- Head — circle at `~0.16 × TileSize` radius, above the body.
- Body — vertical line from below the head down `~0.40 × TileSize`.
- Arms — two diagonals from just below the shoulders.
- Legs — two diagonals from the body bottom.

Stroke width is `max(2, TileSize × 0.08)` so the figure stays readable at any tile size the user picks.

### Walk animation

While `Position != _targetPosition`:

- `_walkPhase` advances by `π × (distance / TileSize)` each frame — one half-cycle per tile travelled, so each tile produces one leg lift and the next tile produces the opposite leg.
- `|sin(_walkPhase)|` drives a vertical bob (`5%` of `TileSize`) — the figure lifts on each step.
- `max(0, sin(_walkPhase))` and `max(0, -sin(_walkPhase))` shorten the left and right legs respectively, alternating up to `35%` of `legLen` — reads as the leg lifting off the ground.

When the character stops, the legs return to their default length on the next redraw and `QueueRedraw` halts.

### Dig animation

Triggered by `Grid.Dug(x, y, depth)`. Every emit resets `_digElapsedMs` to 0; the animation runs for `DigAnimationMs` (default 300 ms) then auto-clears.

- `digSwing = sin(progress × π)` produces a 0 → 1 → 0 arc over the animation.
- Both arms lerp from their idle splay toward a straight-down strike pose by `digSwing`.
- The body crouches by `6%` of `TileSize` at the peak.

Walk and dig animations overlay: while walking, a `Dug` emit just adds the arm strike + crouch on top of the leg lift / bob. There's no state machine — each animation reads its own parameter and contributes additively.

### Tunable

- `DigAnimationMs` — duration of the dig pose (default 300).

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
- Direction-facing (the figure is symmetric; legs alternate but don't aim)
- Sound
- Cancelling a queued scan when a new short click overrides the target
