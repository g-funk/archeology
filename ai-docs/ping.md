# Ping

After each successful dig, a brief flash appears on the floor above the nearest fragment — a sonar-like cue that something is buried within reach. Occasionally a "fake" ping fires on a random tile to keep the signal noisy.

> **Design source:** [`design/features/PING.md`](../design/features/PING.md)
> **Built on:** [ai-docs/excavation.md](excavation.md) (digs emit `Grid.Dug`) and [ai-docs/collection.md](collection.md) (fragments expose `Grid.Fragments`)

---

## Files

| Path | Role |
|---|---|
| `src/Archeology.Prototype/scripts/ping/PingSystem.cs` | Listens to `Grid.Dug`, manages active ping overlays, custom-draws the flashes |
| `src/Archeology.Prototype/scripts/grid/Grid.cs` | Adds the `Dug(int x, int y, int depth)` signal and exposes `Fragments` for nearest-search |
| `src/Archeology.Prototype/scenes/Main.tscn` | `PingSystem` Node2D under `Grid` (so its draw is composited on top of floors and walls) |

---

## Trigger

`Grid.Dug(x, y, depth)` fires from `Grid.Dig` after a successful HP damage on the current layer — i.e., every click that actually does something. It does **not** fire for:

- Clicks blocked by the step constraint (`CanDigDeeper` false)
- Clicks on bedrock
- Clicks that resolve to `TryCollectFragment` (collecting is not digging)

---

## Behavior

`PingSystem.OnDug(digX, digY, digDepth)`:

1. With probability `FakePingChance`, fire a **fake ping**: pick a random in-bounds tile within `±PingRadius` of the dig, flash it with a random brightness in `[0, PingPeakBrightness)`. Return early.
2. Otherwise, scan every cell of every fragment in `Grid.Fragments` and pick the one with the smallest 3D distance to the dig:
   ```
   dx = cell.X - digX
   dy = cell.Y - digY
   dz = frag.Depth - digDepth
   distance = sqrt(dx² + dy² + dz²)
   ```
   Depth differences count exactly the same as horizontal differences (one layer = one tile-width).
3. If that closest cell is already **exposed** (`_grid.GetDepth(fx, fy) == frag.Depth`), skip the ping entirely — the player can see it on the floor; no hint needed. The next-closest cell is not considered.
4. Otherwise, if the closest cell is within `PingRadius`, emit a real ping at that cell's `(x, y)` with linear-falloff brightness:
   ```
   brightness = PingPeakBrightness × (1 - distance / PingRadius)
   ```
5. Otherwise, no ping.

Pings stack — multiple can be active at the same `(x, y)` if the player digs rapidly nearby. Later draws paint over earlier ones.

---

## Rendering

Each active `Ping` carries `X`, `Y`, `PeakBrightness`, `ElapsedMs`. Per frame:

- `_Process` increments `ElapsedMs` by `delta * 1000`. Pings with `ElapsedMs ≥ FadeMs` are removed.
- `_Draw` paints a near-white rect `(1, 1, 0.95, alpha)` over each ping's tile, where `alpha = PeakBrightness × (1 - ElapsedMs / FadeMs)`.

Because `PingSystem` is a child of `Grid`, its local origin matches the grid's and its draw is composited *after* `Grid._Draw` — so flashes render on top of the layered floor and the wall steps.

---

## Tunables (exported on `PingSystem`)

- `GridPath` — node path to the `Grid` (defaults to `..`, i.e. the parent)
- `PingRadius` — max 3D distance (in tiles) for a real ping. Defaults to 8
- `PingPeakBrightness` — alpha of the white overlay at distance 0; linear falloff to 0 at the radius. Defaults to 0.5
- `FadeMs` — milliseconds from peak to fully faded. Defaults to 600
- `FakePingChance` — probability per dig of producing a fake ping instead of a real one. Defaults to 0.05

---

## Cross-feature seam

- `Grid.Dug` is the only signal `PingSystem` listens to. Adding new dig sources (auto-dig tools, AOE diggers, etc.) just needs to emit `Dug` and the ping reacts automatically.
- `Grid.Fragments` exposes only fragments still on the board — collected ones are no longer searched, so already-found fragments never ping.
- The fade-only animation owns the visual; no shader or particle system is needed, and no work happens while the ping list is empty.

---

## Out of scope (for this feature)

- Audio cue / sonar sound
- Distinct visual for fake pings (currently indistinguishable by design — false positives are part of the gameplay)
- Cooldown / throttling for rapid digs
- Direction indicator (no arrow / radar — the ping is purely positional on the floor)
