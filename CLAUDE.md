# CLAUDE.md

Instructions for Claude Code and other coding agents working on the Arkeology Simple Prototype.

> **Guiding principle:** Discovery → Interpretation → Reinterpretation.

---

## Active project: Arkeology Simple Prototype

All work goes into `src/Arkeology.Simple.Prototype/`. Do not make gameplay changes to `src/Arkeology.Prototype/`.

The design source of truth is [`design/DESIGN-SIMPLE.md`](design/DESIGN-SIMPLE.md). If anything in other documents conflicts with it, `DESIGN-SIMPLE.md` wins.

---

## Role

You are a senior C# / Godot engineer. Build a playable prototype that validates the core gameplay loop.

- **Focus on:** fast iteration, clear structure, gameplay-first implementation.
- **Avoid:** over-engineering, premature abstraction, systems the prototype doesn't need.

---

## Design source

Game design lives in [`design/DESIGN-SIMPLE.md`](design/DESIGN-SIMPLE.md). Additional detail (where not covered by DESIGN-SIMPLE.md) can be found in [`design/DESIGN.md`](design/DESIGN.md) and [`design/features/`](design/features/).

- `design/DESIGN-SIMPLE.md` is the primary source of truth for gameplay decisions.
- `CLAUDE.md` (this file) defines structure and implementation conventions. If user feedback contradicts an instruction here, update this file.

---

## Implementation reference (`ai-docs/`)

One doc per implemented feature describing current code: file paths, data model, behavior, tunables, cross-feature seams. **Read the relevant doc before changing a feature, and update it in the same change.**

- [ai-docs/excavation.md](ai-docs/excavation.md) — layered grid, dig click flow, walls, camera fit
- [ai-docs/collection.md](ai-docs/collection.md) — multi-tile fragments, exposure rule, side panel
- [ai-docs/ping.md](ai-docs/ping.md) — dig-triggered flash near the closest buried fragment
- [ai-docs/hints.md](ai-docs/hints.md) — red flash on neighbors that block a dig
- [ai-docs/random_collapse.md](ai-docs/random_collapse.md) — 0..N neighbor collapses per dig
- [ai-docs/radar.md](ai-docs/radar.md) — expanding sonar ring on dig, directional wedge per detected fragment
- [ai-docs/character.md](ai-docs/character.md) — archaeologist figure, tap/double-tap movement, autodig
- [ai-docs/stamina.md](ai-docs/stamina.md) — stamina drain, passive recharge, slowdown, HUD bar

When adding a new feature, create `ai-docs/<feature>.md` and link it from this list and from any sibling docs that share code.

---

## Repo layout

```
Arkeology/
├── design/                          — DESIGN-SIMPLE.md, DESIGN.md, VISUALS.md, features/*.md
├── ai-docs/                         — per-feature implementation docs
└── src/Arkeology.Simple.Prototype/  — Godot C# project (active work)
```

The `Arkeology.sln` also includes `Arkeology.Server`, `Arkeology.Client`, and `Arkeology.Prototype` — keep them minimal and compiling; don't build gameplay there.

---

## Implementation rules

1. Default to `src/Arkeology.Simple.Prototype/`.
2. Build the smallest working version, then iterate.
3. Prefer simple data over abstractions.
4. Keep code readable.
5. Update the relevant `ai-docs/<feature>.md` whenever you change a feature.

---

## Not yet

Don't build until asked: async multiplayer, backend communication, helper system, progression system, discovery feed, monetization, complex artifact graph.
