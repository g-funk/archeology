# CLAUDE.md

This file provides instructions for Claude Code and other coding agents working on the Archeology prototype repository.

---

# Role and Objective

You are acting as a senior C# / Godot software engineer.

Your primary goal is:

Build a playable gameplay prototype that validates the core loop defined in DESIGN.md.

Focus on:
- Fast iteration
- Clear structure
- Gameplay-first implementation

Avoid:
- Over-engineering
- Premature abstraction
- Building systems not required for the prototype

---

# Design Reference

The game design is defined in DESIGN.md.

- DESIGN.md defines what to build
- CLAUDE.md defines how to build it

If there is a conflict:
- Follow DESIGN.md for gameplay decisions
- Follow CLAUDE.md for structure and implementation

If user requests changes to design, update DESIGN.md accordingly. 
If user requests changes that conflict or modfify instructions in CLAUDE.md, update the document

---

# Implementation Reference (ai-docs/)

`ai-docs/` contains one document per implemented feature describing the current code: file paths, data model, behavior, tunables, and cross-feature seams. Read the relevant doc before changing a feature, and update it when the implementation changes.

- [ai-docs/excavation.md](ai-docs/excavation.md) — dig mechanic, tile types, click flow
- [ai-docs/collection.md](ai-docs/collection.md) — multi-tile fragments, full-exposure rule, side panel UI

When adding a new feature, create `ai-docs/<feature>.md` and link it from this list and from any related ai-docs file.

---

# Solution Structure

Archeology/
├── Archeology.sln
├── CLAUDE.md
├── DESIGN.md
├── src/
│   ├── Archeology.Server/
│   ├── Archeology.Client/
│   └── Archeology.Prototype/
└── tests/
    ├── Archeology.Server.Tests/
    ├── Archeology.Client.Tests/
    └── Archeology.Prototype.Tests/

---

# Project Responsibilities

## Archeology.Server

- .NET backend skeleton
- No gameplay logic at this stage
- Keep minimal and compilable

Future responsibilities:
- artifact graph storage
- fragment distribution
- async matching system

---

## Archeology.Client

- Future production client
- Godot C# project
- Keep minimal and compiling

Do NOT:
- implement gameplay systems here yet
- copy prototype code unless explicitly asked

---

## Archeology.Prototype

- Primary development project
- Godot C# application
- All gameplay experimentation happens here

---

# Core Prototype Goal

The prototype must validate the core gameplay loop:

1. Player digs tiles on a grid
2. Tiles reveal:
   - empty space
   - material
   - fragment
3. Fragments are collected
4. Player interprets fragments or artifacts
5. Player may later revise interpretation

---

# Gameplay Constraints

## Excavation System

- 2D grid (tile-based)
- Start with a single layer
- Each dig action targets a single tile

Tile types:
- soil (fast)
- stone (slower)

Fragments:
- multi-tile shapes (prototype set: 2x2 square, 3x3 hollow box, plus, corner)
- each fragment tile is buried under a soil or stone cover
- digging a fragment tile clears its cover (same speed as plain soil/stone)
- a fragment can only be collected once *all* its tiles are exposed; clicking any cell collects the whole fragment
- clearing a tile next to a buried fragment tile surfaces a hint on that tile

---

## Artifact System

Artifacts are composed of fragments.

Future model:
- graph structure (nodes = fragments)

Prototype model:
- simple grouping

Each fragment must support:
- shape
- visual identity
- semantic tags
- player interpretation

Do NOT build:
- full graph system
- complex matching logic

---

## Interpretation System

This is a core pillar.

The player must be able to:
- assign meaning
- change interpretation later

Prototype:
- simple label (string or enum)
- editable at any time

Optional:
- confidence level

---

## Matching System

Prototype only:
- simulate locally
- provide hints
- simple comparison view

No backend.

---

# Godot Project Structure

scenes/
  Main.tscn
  gameplay/
  ui/
  debug/

scripts/
  grid/
  excavation/
  artifacts/
  interpretation/
  player/
  ui/
  debug/

assets/
resources/

---

# Suggested Core Classes

Grid
Tile
ExcavationSystem

Fragment
Artifact
ArtifactCollection

Interpretation
InterpretationSystem

PlayerController

---

# Implementation Order

1. Grid system
2. Digging mechanic
3. Tile types
4. Fragment spawning
5. Fragment collection
6. Artifact grouping
7. Interpretation UI
8. Matching hint

---

# Testing

Use xUnit.

Projects:
- Archeology.Server.Tests
- Archeology.Client.Tests
- Archeology.Prototype.Tests

For now:
- one placeholder test per project

---

# Implementation Rules

1. Default to Archeology.Prototype
2. Build smallest working version
3. Prefer simple data
4. Optimize for iteration speed
5. Keep code readable
6. Avoid premature abstraction

---

# What NOT To Build Yet

- async multiplayer
- backend communication
- helper system
- progression system
- discovery feed
- monetization
- complex artifact graph

---

# Prototype Success Criteria

- player can dig and reveal fragments
- player can assign meaning
- player can change interpretation
- player experiences discovery, uncertainty, reinterpretation

---

# Guiding Principle

Discovery -> Interpretation -> Reinterpretation
