# Hints

Subtle, transient cues that surface invalid actions. Currently one rule: when the player's dig is rejected by the step constraint, the *preventing* neighbors (the tiles that are too shallow) briefly flash red. This is intended to communicate "you can't dig here yet because of these tiles" without text or sound.

> **Design source:** [`design/features/HINTS.md`](../design/features/HINTS.md)
> **Built on:** [ai-docs/excavation.md](excavation.md) — uses the step-constraint check inside `Grid.Dig`

---

## Files

| Path | Role |
|---|---|
| `src/Arkeology.Prototype/scripts/hints/HintsSystem.cs` | Listens to `Grid.DigBlocked`, manages active red flashes, custom-draws them |
| `src/Arkeology.Prototype/scripts/grid/Grid.cs` | Emits `DigBlocked(int x, int y)` from `Dig` when `CanDigDeeper` returns false |
| `src/Arkeology.Prototype/scenes/Main.tscn` | `HintsSystem` Node2D under `Grid` (composited on top of floors, walls, and pings) |

---

## Trigger

`Grid.DigBlocked(x, y)` fires from `Grid.Dig` in three cases:

- **Step constraint** — at least one in-bound 4-neighbor is shallower than this tile's current depth.
- **Bedrock** — the tile is already at `LayerCount`.
- **Fragment at current depth** — `Dig` would grind through a fragment.

In all three cases the signal carries the **attempted** tile's coords. Autodig calls `Grid.Dig` directly and so triggers all three; manual clicks route through `HandleClick`, which deals with the fragment case via `TryCollectFragment` and so only reaches the step-constraint / bedrock branches.

`DigBlocked` does **not** fire for:

- Bedrock clicks (silent no-op)
- Out-of-bounds clicks
- Clicks that resolve to `TryCollectFragment`

---

## Behavior

`HintsSystem.OnDigBlocked(x, y)`:

1. Read the attempted tile's depth `d = _grid.GetDepth(x, y)`.
2. If the tile is at bedrock (`d >= LayerCount`) or has a fragment at the current depth (`_grid.HasFragmentAt(x, y, d)`) — there's no preventing neighbour to highlight, so flash the **attempted tile itself** and return.
3. Otherwise (step constraint), iterate the 4-neighbors:
   - Skip out-of-bounds and any neighbour with `depth >= d` (those aren't preventers).
   - Add a `Flash` at each shallower neighbour.
4. If no preventer was found (unusual but defensive), fall back to flashing the attempted tile.
5. `QueueRedraw`.

A subsequent blocked dig can re-flash the same tile (resets its timer in practice, since a new `Flash` is added on top).

---

## Rendering

Same fade-overlay pattern as [ai-docs/ping.md](ping.md). Each active `Flash` carries `X`, `Y`, `ElapsedMs`. Per frame:

- `_Process` ages every flash by `delta * 1000`; flashes with `ElapsedMs ≥ FadeMs` are removed.
- `_Draw` paints `FlashColor` at `alpha = FlashPeakBrightness × (1 - ElapsedMs / FadeMs)` over each flashed tile.

Because `HintsSystem` is a child of `Grid` and sits below `PingSystem` in the scene tree order, hint flashes draw **above** floors and walls but **below** ping flashes if both fire on the same tile in the same frame. In practice they can't co-occur — a blocked dig produces no `Dug` signal, so no ping fires alongside the hint.

---

## Tunables (exported on `HintsSystem`)

- `GridPath` — node path to the `Grid` (defaults to `..`, the parent)
- `FlashPeakBrightness` — alpha of the overlay at `ElapsedMs = 0`. Defaults to 0.6
- `FadeMs` — milliseconds from peak to fully faded. Defaults to 400 (snappier than ping's 600 — feedback for an invalid action)
- `FlashColor` — RGB of the overlay; alpha is recomputed each frame. Defaults to `(1, 0.2, 0.2)`

---

## Cross-feature seam

- `Grid.DigBlocked` is the only signal the hints system listens to. Future invalid-action cues (e.g. clicking on a partial-exposed fragment, clicking bedrock) can add new signals and a corresponding case here without touching the rendering loop.
- The "fragment hint" ochre tile on the floor (documented in [ai-docs/collection.md](collection.md)) is unrelated and belongs to the fragment lifecycle — same word, different mechanic.

---

## Out of scope (for this feature)

- Cooldown / throttling for rapid blocked clicks (currently re-fires every click)
- Different colors per blocked-action kind
- Audio cue
- Persistent tutorial-style hints (these flashes are momentary only)
