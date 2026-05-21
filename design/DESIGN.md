# 🏺 Arkeology — Design Document
**Working Title:** *Fragments of the Past*

> **Implementation status:** see [ai-docs/](../ai-docs/) for per-feature documentation of what is currently built.
> - [ai-docs/excavation.md](../ai-docs/excavation.md) — the Excavation section
> - [ai-docs/collection.md](../ai-docs/collection.md) — the Fragments & Artifacts section
> - [ai-docs/ping.md](../ai-docs/ping.md), [ai-docs/hints.md](../ai-docs/hints.md), [ai-docs/random_collapse.md](../ai-docs/random_collapse.md) — feedback feature docs

---

## Design Pillars

1. Discovery over collection.
2. Interpretation over certainty.
3. Connection over completion.
4. Simplicity in input, depth in meaning.

> *Alone you discover. Together you understand.*

---

## 1. Overview

A single-player archaeology game where players excavate a layered world, uncovering fragmented artifacts. Each artifact is partially meaningful alone and gains deeper meaning when combined with fragments discovered by other players asynchronously.

- **Platform:** Desktop / Steam
- **Mode:** Single-player with async social features
- **Session length:** 1–15 minutes typical

The player digs through layered terrain, discovers artifact fragments, interprets their meaning, and occasionally connects findings with others — gradually uncovering a larger hidden narrative.

---

## 2. Gameplay Loop

- **Short (minutes):** dig tiles → reveal empty space, material, or fragment → fragment enters journal → optional hint or match.
- **Medium (a session):** work on a partial artifact → interpret meaning → compare async → receive matches → progress deeper.
- **Long (sessions):** complete artifacts, refine interpretations, contribute to shared knowledge, unlock deeper layers and narrative.

---

## 3. Excavation

- 2D grid (~100×100 per layer) with 5–20 depth layers.
- Tap to dig one tile at a time. Soil clears in one hit, stone in more.
- A tile can only be dug one layer deeper than its surroundings — players must terrace down.
- Fragments occupy multi-tile shapes buried under soil or stone. A fragment can only be collected once *all* its tiles are exposed.
- Clearing a tile adjacent to a buried fragment surfaces a hint on that tile.

---

## 4. Fragments & Artifacts

An **artifact** is a graph of fragments: nodes are fragments, edges are relationships.

Each **fragment** has:
- a shape (silhouette / matchable edges),
- a visual identity (symbols, material, patterns),
- hidden semantic tags (culture, function, meaning),
- a player interpretation.

Artifact tiers:

| Tier | Fragments | Completion |
|---|---|---|
| 1 — Self-contained | 2–4 | solo |
| 2 — Partial | 4–10 | meaningful but incomplete alone |
| 3 — Distributed | 8–20 | requires async collaboration |

Meaning layers: **surface** (immediate visual) → **interpretation** (player's guess) → **true meaning** (revealed by connections).

---

## 5. Interpretation

Players assign meaning to artifacts: weapon, tool, ritual object, unknown.

Each interpretation carries a confidence (low / medium / high), informed by fragment completeness, matches with others, and global knowledge.

When new fragments connect, prior interpretations may change. The player is notified.

> Key emotional moment: *"I was wrong."*

---

## 6. Async Social

- No forced multiplayer, no real-time dependency — collaboration emerges from passive aggregation and explicit opt-in.
- The system detects shape similarity and semantic overlap and surfaces match suggestions ("Possible match exists", "Similar fragment found").
- Players can view ghost overlays, compare fragments, and accept matches.
- A discovery feed aggregates notable finds and new connections across the player base.

---

## 7. Progression

- **Player skills:** larger dig area, better hints, safer excavation.
- **Knowledge:** symbol decoding, cultural understanding, pattern recognition.
- **Layers:** deeper layers unlock over time and host more complex artifacts.
- **Optional helpers:** time-limited bots that boost dig speed, reveal hidden fragments, or reduce damage.

---

## 8. Content Scope (initial target)

- 1 culture
- 20–40 artifacts, 100–200 fragments, 3–5 layers
- Each artifact connects to 2–5 others
- Clusters: burial practices, tools, symbolic language, ritual objects

---

## 9. UX

- **Visual style:** minimalist, clean silhouettes, symbolic details.
- **Core screens:** dig view (grid), artifact journal, comparison view, discovery feed.
- **Matching UX:** ghost overlays, highlighted edges, suggested connections.

---

## 10. Monetization Principles

If/when monetized: **do not sell answers, do not block thinking.** Cosmetics, soft energy limits, and helper boosts are acceptable; paying to skip interpretation or to auto-solve artifacts is not.

---

## 11. Success Criteria

- Players form their own interpretations.
- Players experience "aha" moments.
- Players care about the connections, not just the count.
- Players share discoveries.
