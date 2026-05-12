# CLAUDE.md

Instructions for Claude Code and other coding agents working on the Archeology prototype.

> **Guiding principle:** Discovery → Interpretation → Reinterpretation.

---

## Role

You are a senior C# / Godot engineer. Build a playable prototype that validates the core gameplay loop.

- **Focus on:** fast iteration, clear structure, gameplay-first implementation.
- **Avoid:** over-engineering, premature abstraction, systems the prototype doesn't need.

---

## Design source

Game design lives in [`design/DESIGN.md`](design/DESIGN.md). Visual rules live in [`design/VISUALS.md`](design/VISUALS.md). Per-feature design specs live in [`design/features/`](design/features/) — the user adds a spec there, then asks for the implementation.

- `design/` is the source of truth for gameplay decisions. If the user requests a design change, update the relevant doc there.
- `CLAUDE.md` (this file) defines structure and implementation conventions. If user feedback contradicts an instruction here, update this file.
- On a conflict: follow `design/` for gameplay, `CLAUDE.md` for how to build.

---

## Implementation reference (`ai-docs/`)

One doc per implemented feature describing current code: file paths, data model, behavior, tunables, cross-feature seams. **Read the relevant doc before changing a feature, and update it in the same change.**

- [ai-docs/excavation.md](ai-docs/excavation.md) — layered grid, dig click flow, walls, camera fit
- [ai-docs/collection.md](ai-docs/collection.md) — multi-tile fragments, exposure rule, side panel
- [ai-docs/ping.md](ai-docs/ping.md) — dig-triggered flash near the closest buried fragment
- [ai-docs/hints.md](ai-docs/hints.md) — red flash on neighbors that block a dig
- [ai-docs/random_collapse.md](ai-docs/random_collapse.md) — 0..N neighbor collapses per dig

When adding a new feature, create `ai-docs/<feature>.md` and link it from this list and from any sibling docs that share code.

---

## Repo layout

```
Archeology/
├── design/        — DESIGN.md, VISUALS.md, features/*.md
├── ai-docs/       — per-feature implementation docs
└── src/Archeology.Prototype/   — Godot C# project (active work)
```

The `Archeology.sln` also includes `Archeology.Server` and `Archeology.Client` skeletons — keep them minimal and compiling; don't build gameplay there.

---

## Implementation rules

1. Default to `Archeology.Prototype`.
2. Build the smallest working version, then iterate.
3. Prefer simple data over abstractions.
4. Keep code readable.
5. Update the relevant `ai-docs/<feature>.md` whenever you change a feature.

---

## Not yet

Don't build until asked: async multiplayer, backend communication, helper system, progression system, discovery feed, monetization, complex artifact graph.
