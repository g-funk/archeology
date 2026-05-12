# Radar

A sonar-like directional scan that fires on each successful dig. An expanding ring grows out from the dig cell and fades by the time it reaches the scan radius. For each unexposed fragment within range, a brighter wedge (~1/8 of the circle) highlights the 2D direction to its closest cell.

> **Design source:** [`design/features/RADAR.md`](../design/features/RADAR.md)
> **Built on:** [ai-docs/excavation.md](excavation.md) (the `Grid.Dug` signal) and [ai-docs/collection.md](collection.md) (`Grid.Fragments`)

---

## Files

| Path | Role |
|---|---|
| `src/Archeology.Prototype/scripts/radar/RadarSystem.cs` | Listens to `Grid.Dug`, tracks active pulses, draws the rings and wedges |
| `src/Archeology.Prototype/scenes/Main.tscn` | `RadarSystem` Node2D under `Grid` (composited on top of floors, walls, pings, and hints) |

---

## Trigger

`Grid.Dug(x, y, depth)` — exactly the same signal that drives [ping](ping.md). Every successful HP-damage click creates one pulse. Constraint-blocked clicks (`DigBlocked`) and collection clicks do not fire radar.

---

## Behavior

For each `OnDug(digX, digY, digDepth)`:

1. Iterate `Grid.Fragments` and apply the eligibility filter (see below). For each eligible fragment, find its closest cell by 3D distance:
   ```
   dx = c.X - digX
   dy = c.Y - digY
   dz = frag.Depth - digDepth
   distance = sqrt(dx² + dy² + dz²)
   ```
2. Drop the fragment if `distance ≥ ScanRadius`.
3. Compute the 2D direction from the dig to that closest cell: `angle = atan2(c.Y - digY, c.X - digX)`. If the cell is **directly under** the dig (same x and y) there's no horizontal direction — the wedge is skipped for that fragment.
4. Add a `Pulse` carrying `(CenterX, CenterY, angles[], ElapsedMs = 0)`.

A pulse is added even when no fragments are in range — the empty ring still fires as feedback that the radar ticked.

### Eligibility

A fragment participates only if **none** of its cells is exposed (`_grid.GetDepth(c.X, c.Y) != frag.Depth` for every cell). Same as ping's first rule. The ping-only "lock to first cell" rule does **not** apply here — radar is its own beat, can repeatedly highlight the same fragment.

---

## Rendering

Per active pulse, in `_Process` / `_Draw`:

- `t = ElapsedMs / FadeMs` (0 → 1 over the pulse's lifetime).
- `radius = t × ScanRadius × TileSize` — expands from 0 to the scan boundary.
- `alpha = 1 - t` — fades to invisible by the time the ring hits the boundary.
- Ring drawn with `DrawArc(center, radius, 0, 2π, 48, color, RingThickness)` at `alpha × RingBrightness`.
- Each wedge drawn with `DrawArc(center, radius, angle - wedgeHalf, angle + wedgeHalf, 12, color, WedgeThickness)` at `alpha × WedgeBrightness`, where `wedgeHalf = (2π × WedgeFraction) / 2`.

Because `RadarSystem` sits after `PingSystem` and `HintsSystem` as a child of `Grid`, its pulses draw on top of the floor, walls, ping flashes, and hint flashes.

---

## Tunables (exported on `RadarSystem`)

- `GridPath` — node path to the `Grid` (defaults to `..`)
- `ScanRadius` — max 3D distance, in tiles, that a fragment can sit at to be detected. Defaults to 8 (same as ping).
- `FadeMs` — milliseconds from pulse start to fully faded. Defaults to 800.
- `WedgeFraction` — wedge arc width as a fraction of the full circle. Defaults to 0.125 (1/8 = 45°).
- `RingBrightness`, `WedgeBrightness` — peak alpha of the base ring and the wedges. Defaults 0.35 / 0.7.
- `RingThickness`, `WedgeThickness` — pixel stroke width. Defaults 2 / 5.

---

## Cross-feature seam

- `Grid.Dug` is shared with the ping system; both react independently to the same click.
- Eligibility is shared with the ping system's first rule (`any cell exposed → skip`), but the lock-after-first-ping rule is ping-only. Radar fires on every dig regardless.
- Collapses are silent (`Dug` doesn't fire for them — see [random_collapse.md](random_collapse.md)), so radar never pulses from a collapse.

---

## Out of scope (for this feature)

- Audio sweep
- Different trigger sources (e.g., a dedicated "ping" hotkey) — currently piggybacks on dig
- Wedge angular size scaling with distance / fragment size
- Vertical/depth indication (wedges show 2D direction only; depth is implicit)
