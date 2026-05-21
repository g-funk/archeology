# ai-docs/stamina.md — Stamina System

## Purpose

Limits how fast the player can dig and slows digging as stamina depletes. Stamina recharges passively over time so the player recovers without requiring the Relaxation Room.

---

## Files

| File | Role |
|---|---|
| `scripts/stamina/StaminaSystem.cs` | Core logic — drain, passive recharge, slowdown |
| `scripts/ui/StaminaBar.cs` | HUD bar that renders current/max stamina |
| `scripts/screens/RelaxationScreen.cs` | Full-refill button (ad gate simulation) |

---

## Data model

`StaminaSystem` (Node, child of `Main`):

| Export | Default | Meaning |
|---|---|---|
| `GridPath` | `../Grid` | Path to `Grid` node for `Dug` signal |
| `StaminaFull` | 100 | Max stamina |
| `StaminaSpend` | 1 | Stamina lost per dig hit |
| `StaminaSlowdownLimit` | 10 | Below this, dig speed degrades |
| `SlowdownTimeMs` | 200 | Extra ms per missing stamina point below the limit |
| `RechargePerSecond` | 1.0 | Passive stamina regained per second (0 = disabled) |

Internal state: `Current int`, `_rechargeAccumulator double`.

---

## Behavior

**Drain:** Each `Grid.Dug` emission subtracts `StaminaSpend` and resets `_rechargeAccumulator` to 0, preventing partial-second credit from carrying over into a dig.

**Passive recharge:** `_Process(delta)` accumulates `delta × RechargePerSecond`. When the accumulator reaches ≥ 1, whole stamina points are added (fractional remainder is kept). Stops when `Current == Max`. Emits `StaminaChanged` on each tick that adds points.

**Slowdown:** `CurrentSlowdownMs()` returns `(SlowdownLimit - Current) × SlowdownTimeMs` when below the limit, 0 otherwise. `PlayerCharacter` adds this to its dig interval.

**Refill:** `Refill()` sets `Current = Max`, clears the accumulator, and emits `StaminaChanged`. Called by `RelaxationScreen`.

---

## Signal

`StaminaChanged(int current, int max)` — emitted on any change (drain, recharge tick, refill). `StaminaBar` subscribes and calls `QueueRedraw()`.

---

## Tunables

- `RechargePerSecond = 1` → full bar in 100 s from empty.
- Set `RechargePerSecond = 0` to disable passive recharge (manual refill only).
- Accumulator reset on dig prevents recharge "credit" from accumulating during rapid digging.
