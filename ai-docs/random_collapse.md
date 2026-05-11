# Random Collapse

After every successful dig, 0..N neighboring tiles can have their current layer collapse — the dig "shakes loose" some surroundings. Collapses obey the same "cannot dig" rules as a manual dig: step constraint, bedrock, and fragment blocks all prevent a collapse on that tile.

> **Design source:** [`design/features/RANDOM_COLLAPSE.md`](../design/features/RANDOM_COLLAPSE.md)
> **Built on:** [ai-docs/excavation.md](excavation.md) — the dig pipeline; collapses reuse `CanDigDeeper` and the fragment block check

---

## Files

| Path | Role |
|---|---|
| `src/Archeology.Prototype/scripts/grid/Grid.cs` | `TryRandomCollapse` (called from `Dig`) and the public `TryCollapse(cell)` |

No new node or script; the feature is entirely inside `Grid` because it shares state with the dig pipeline.

---

## Trigger

`Grid.TryRandomCollapse(x, y)` runs at the end of every successful `Dig(cell)` — i.e., after a click that produced an actual HP decrement on the current layer. It does **not** run when:

- The dig was blocked by the step constraint (`DigBlocked` instead)
- The clicked tile is bedrock
- The click resolved as `TryCollectFragment`

---

## Behavior

1. Roll the four 4-neighbors of the dug tile in a random order (Fisher–Yates shuffle of the direction list, using the Grid's seeded `_rng`). The shuffle keeps the `MaxCollapse` cap from biasing toward right/down when the limit kicks in.
2. For each direction:
   - If `collapsed == MaxCollapse` → stop.
   - Otherwise, roll `_rng.NextDouble() < CollapseChance`. On a hit, attempt `TryCollapse(neighbor)`.
   - On success, increment `collapsed`.
3. The whole process is silent — no `Dug` or `DigBlocked` signals fire for collapses, so they don't trigger pings or hint flashes.

`TryCollapse(cell)`:

```
if cell out of bounds  → false
if _depth[cell] == LayerCount (bedrock)  → false
if _fragmentAt[cell, _depth[cell]] != null (fragment blocks)  → false
if !CanDigDeeper(cell) (step constraint)  → false
otherwise: _depth[cell] += 1  → true
```

A collapse advances the neighbor's depth by exactly **one** layer, regardless of how much HP that layer had left. Stone with 2 HP gets fully removed in one collapse — by definition a "shake loose", not a slow chip.

---

## Tunables (exported on `Grid`)

- `MaxCollapse` — cap on collapsed neighbors per dig. Set to 0 to disable. Default 2.
- `CollapseChance` — per-neighbor probability per dig. Default 0.15 (so each dig on average loses ~0.6 neighbors, capped at 2).

---

## Cross-feature seam

- `TryCollapse` enforces the **same** invariants as a manual dig: the step constraint never gets violated by collapse, so the lit-from-lower-right wall rendering still holds and `HintsSystem` doesn't spuriously fire.
- Fragments are safe: a fragment cell at the neighbor's current depth blocks the collapse. This means collapses can never destroy progress toward an exposure.
- `_rng` is the Grid's persistent random, seeded from `Seed` in `Generate`. Same seed + same click sequence = same collapse pattern, which is handy when reproducing a bug.

---

## Out of scope (for this feature)

- Cascading collapses (a collapse advancing a tile doesn't trigger further collapses around *it*)
- Visual cue / animation for the collapsed tile (the depth change is visible immediately via the floor color + walls; no extra effect)
- Distance > 1 (only direct 4-neighbors are eligible)
- Probability shaped by depth or material (uniform `CollapseChance` for all candidates)
