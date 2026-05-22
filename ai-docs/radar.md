# Radar

A sonar-like directional scan that the player triggers explicitly. An expanding ring grows out from the scan origin and fades by the time it reaches the scan radius. For each unexposed fragment within range, a brighter wedge (~1/8 of the circle) highlights the 2D direction to its closest cell.

> **Design source:** [`design/features/RADAR.md`](../design/features/RADAR.md)
> **Built on:** [ai-docs/character.md](character.md) (drives the scan trigger), [ai-docs/excavation.md](excavation.md) (input + the `Grid.ScanTriggered` signal), [ai-docs/collection.md](collection.md) (`Grid.Fragments`)

---

## Files

| Path | Role |
|---|---|
| `src/Arkeology.Production.Client/scripts/radar/RadarSystem.cs` | Listens to `Grid.Dug`, tracks active pulses, draws the rings and wedges |
| `src/Arkeology.Production.Client/scenes/Main.tscn` | `RadarSystem` Node2D under `Grid` (composited on top of floors, walls, pings, and hints) |

---

## Trigger

`Grid.ScanTriggered(x, y, depth)` — emitted via `Grid.TriggerScan(...)` from `PlayerCharacter`:

- **`S` key** → `PlayerCharacter.RequestScanHere()` immediately calls `Grid.TriggerScan` with the character's current tile and that tile's `_depth`.
- **Long-click on a tile** → `PlayerCharacter.RequestScanAt(cell)` sets the walk target and marks the scan as pending; once the character arrives (`Position == _targetPosition`), `Grid.TriggerScan` fires.

Digging no longer triggers radar. Constraint-blocked clicks, fragment collection, and `Grid.Dug` are all unrelated to scan now.

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

- `Grid.ScanTriggered` is the only signal `RadarSystem` listens to. Future scan triggers (tools, abilities, auto-scans) emit the same signal.
- Eligibility shares ping's first rule (`any cell exposed → skip`) — the ping-only "lock to first cell" rule does **not** apply here.
- Long-click sets the character's `_targetPosition` and `_scanPendingOnArrival`; the existing short-click → `Grid.Clicked` → character-moves path is independent. Both gestures move the character, only the long one queues a scan.
- Random collapses and dig damage no longer affect radar — they don't fire `ScanTriggered`.

---

## Out of scope (for this feature)

- Audio sweep
- Different trigger sources (e.g., a dedicated "ping" hotkey) — currently piggybacks on dig
- Wedge angular size scaling with distance / fragment size
- Vertical/depth indication (wedges show 2D direction only; depth is implicit)
