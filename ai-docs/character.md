# Character (archaeologist)

A simple stick-figure on the grid that walks toward whichever tile the player clicks. Phase 1 — cosmetic only; clicks continue to drive dig / collect independently.

> **Design source:** [`design/features/CHARACTER.md`](../design/features/CHARACTER.md)
> **Built on:** [ai-docs/excavation.md](excavation.md) — the new `Grid.Clicked` signal

---

## Files

| Path | Role |
|---|---|
| `src/Arkeology.Production.Client/scripts/player/PlayerCharacter.cs` | `Node2D` figure: initial placement, click-driven target, frame-step movement, custom-drawn body |
| `src/Arkeology.Production.Client/scripts/grid/Grid.cs` | New `Clicked(int x, int y)` signal emitted from `HandleClick` for every in-bounds click |
| `src/Arkeology.Production.Client/scenes/Main.tscn` | `PlayerCharacter` Node2D under `Grid`, ordered last so it draws on top of overlays |

---

## Behaviour

- **Initial position:** middle of the grid. On `_Ready`, the character snaps to `(Grid.Width / 2, Grid.Height / 2)`.
- **Click handling:** subscribes to `Grid.Clicked` for normal short clicks. Every in-bounds click sets `_targetPosition` to that tile's centre in grid-local coordinates. Out-of-bounds clicks emit nothing, so the character ignores them.
- **Movement:** `_Process` steps `Position` toward `_targetPosition` by `SpeedTilesPerSecond × TileSize × delta` each frame using `Vector2.MoveToward`. Quick (default 10 tiles/sec) but visibly non-instant.
- **Action API** (all called from `ExcavationSystem`):
  - `RequestScanAt(cell)` — sets `_targetPosition` and marks `_scanPendingOnArrival = true`. When the character arrives (now or after walking), `_Process` calls `Grid.TriggerScan(x, y, depth)`. Used for the long-click gesture.
  - `RequestScanHere()` — immediately calls `Grid.TriggerScan(...)` for the character's current tile and that tile's depth, no walking. Used for the `S` key.
  - `RequestStep(dx, dy)` — set the target one tile in the given direction from `CurrentTile()`, but **only when the character has arrived at its current target** (`Position == _targetPosition`). Without that guard a single tap registers as two tiles, because `CurrentTile()` flips at the half-way point and the next poll would push the target forward again. The guard alone isn't enough though — see the continuous-step threshold in `ExcavationSystem` below.
  - `RequestCollect()` — calls `Grid.TryCollectFragment` for the character's current tile. No-op if no fragment / not fully exposed (the grid validates). Used by the C-key trigger.
  - `RequestDigAround()` — fills `_digQueue` with the under-tile first, then the **6 hex neighbors** in `HexMetrics.GetNeighbors` order (E, W, NE, NW, SE, SW; out-of-bounds dropped). Drains the queue in `_Process` one tile at a time on a `DigAnimationMs` cadence. Re-pressing `D` resets the queue.

#### Autodig rules

For each tile in the queue, every `DigAnimationMs`:

- Call `Grid.Dig(cell, allowCollapse: false)` and branch on its `DigResult`:
  - `Cleared` — depth advanced; drop the tile.
  - `Damaged` — stone took its first of two HP hits; keep the tile at the head of the queue for another pass.
  - `Blocked` — bedrock, fragment at current depth, step-constraint blocked, or out of bounds. Drop the tile. `Grid.Dig` has already emitted `DigBlocked` in the first three cases, so the hint system flashes red feedback (see [ai-docs/hints.md](hints.md) for which tile flashes).

Random collapse is suppressed (`allowCollapse: false`) so the sweep stays deterministic.

This naturally gives one animation per HP hit — soil tiles get one strike, stone tiles get two — and the queue moves on as each layer is cleared.

#### Interrupting autodig with a tap

Tapping a different tile while `IsAutoDigging` cancels the current sweep and immediately starts the new command:
- Single tap on another tile → `MoveTo(cell)` (walk, no dig on arrival)
- Double-tap on another tile → `MoveAndDig(cell)` (walk + autodig on arrival)

`CancelAutodig()` clears `_digQueue` and resets `_digQueueTimer`. Tapping the character's own tile calls `RequestDigAround()` which also clears the queue and restarts from the current position.

`IsAutoDigging` is `true` while `_digQueue.Count > 0`.
- **Independence from digging:** the short click that moves the character also fires `Grid.HandleClick` (dig / collect). The character may still be in motion while the dig has already resolved — gating dig on arrival is a future phase.

### Keyboard commands (handled by `ExcavationSystem`)

| Key | Action |
|---|---|
| `S` | Scan at the character's current tile (via `RequestScanHere`). |
| `D` | Dig the under-tile + 6 hex neighbors one-by-one (via `RequestDigAround`). |
| `C` | Collect a fragment at the character's current tile if it's fully exposed (via `RequestCollect`). |
| Arrow keys | Step one tile per press. Holding longer than `ContinuousStepHoldMs` (default 250 ms) chains continuous steps; combinations move diagonally. |

#### Why the continuous-step threshold

A single tap should reliably move the character exactly one tile. The arrival frame is the problem: when a tile's walk time is shorter than the tap (~100 ms vs typical 100–200 ms taps), the poll on the arrival frame sees the key still pressed, the `RequestStep` guard passes (`Position == _targetPosition` just became true), and a second step fires — the player gets two tiles for a single intended tap.

`ExcavationSystem` solves this with two pieces of state:

- `_lastArrowDir` — the arrow direction polled last frame.
- `_arrowPressedAtMs` — when the current press began.

The polling logic:

1. **Press transition or direction change** (`wasIdle || dir != _lastArrowDir`): fire one step immediately, reset `_arrowPressedAtMs`.
2. **Same direction held** for less than `ContinuousStepHoldMs`: don't fire — the guard would let it through, so we explicitly block it. Single taps stop after one tile no matter how the walk-time and tap-duration line up.
3. **Same direction held** for at least `ContinuousStepHoldMs`: fire each frame the polling sees the key. The `RequestStep` guard ensures only arrival frames actually advance the target, so the character chains tiles seamlessly.

Default 250 ms keeps natural taps below the threshold while making "intentional hold" feel responsive. Continuous mode does have a short idle pause (`ContinuousStepHoldMs − tileWalkMs`) before kicking in, then runs at the walk speed.

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
