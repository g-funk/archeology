# 🏺 Archaeology Game — Design Document  
**Working Title:** *Fragments of the Past*  
**Version:** 0.1  
**Author:** You (+ ChatGPT sparring)

---

# 1. Overview

## 1.1 High Concept

A **single-player archaeology game** where players excavate a layered world, uncovering fragmented artifacts. Each artifact is partially meaningful alone, but gains deeper meaning when combined with fragments discovered by other players asynchronously.

> **Core Pillar:**  
> *Alone you discover. Together you understand.*

---

## 1.2 Player Experience

The player:
- Digs through layered terrain
- Discovers artifact fragments
- Interprets their meaning
- Occasionally connects their findings with others
- Gradually uncovers a larger hidden narrative

---

## 1.3 Platform & Scope

- **Platform:** Mobile (primary), potentially desktop later
- **Mode:** Single-player with async social features
- **Session Length:** 1–15 minutes typical

---

# 2. Core Gameplay Loop

## 2.1 Short Loop (1–3 minutes)

1. Player digs tiles
2. Reveals:
   - empty space
   - material
   - fragment
3. Fragment added to journal
4. Possible hint or match triggered

---

## 2.2 Medium Loop (10–15 minutes)

1. Work on partially discovered artifact
2. Interpret meaning
3. Compare fragments (async)
4. Receive hints / matches
5. Progress deeper layer

---

## 2.3 Long Loop (multi-session)

- Complete artifacts
- Refine interpretations
- Contribute to shared knowledge
- Unlock deeper layers and narrative

---

# 3. Excavation System

## 3.1 Grid

- 2D grid (e.g. 100x100 per layer)
- Multiple depth layers (5–20 initially)

---

## 3.2 Digging Mechanics

- Tap or swipe to excavate clusters (not single pixels)
- Each action clears:
  - small area (3–10 tiles)

---

## 3.3 Constraints

- Cannot dig through artifact fragments directly
- Must excavate surrounding tiles first

---

## 3.4 Materials

Tiles may contain:
- soil (easy)
- stone (slower)
- special layers (later gameplay)

---

# 4. Artifact System (Core System)

## 4.1 Artifact Structure

Each artifact is a **graph of fragments**:

- Nodes = fragments  
- Edges = relationships  

---

## 4.2 Fragment Properties

Each fragment has:

### A. Shape
- silhouette  
- edges that can match  

### B. Visual Identity
- symbols  
- material  
- patterns  

### C. Semantic Tags (hidden)
- culture  
- function  
- meaning  

### D. Player Interpretation
- what the player believes it is  

---

## 4.3 Artifact Types

### Tier 1 — Self-contained
- 2–4 fragments  
- completable solo  

### Tier 2 — Partial
- 4–10 fragments  
- meaningful but incomplete alone  

### Tier 3 — Distributed
- 8–20 fragments  
- require async collaboration for full meaning  

---

## 4.4 Meaning Layers

| Layer | Description |
|------|------------|
| Surface | Immediate visual meaning |
| Interpretation | Player’s guess |
| True Meaning | Revealed through connections |

---

# 5. Interpretation System

## 5.1 Player Interpretation

Players assign meaning to artifacts:
- weapon  
- tool  
- ritual object  
- unknown  

---

## 5.2 Confidence System

Each interpretation has:
- low / medium / high confidence  

Based on:
- fragment completeness  
- matches with others  
- global knowledge  

---

## 5.3 Reinterpretation

When new fragments connect:
- previous interpretations may change  
- player is notified  

> Key emotional moment:  
> “I was wrong.”

---

# 6. Async Social System

## 6.1 Philosophy

- No forced multiplayer  
- No real-time dependency  
- Collaboration emerges naturally  

---

## 6.2 Matching System

### Automatic Detection

System detects:
- shape similarity  
- semantic overlap  

---

### Player Feedback

Player sees:
- “Possible match exists”  
- “Similar fragment found”  

---

### Interaction Options

- View overlay  
- Compare fragments  
- Accept match  

---

## 6.3 Passive Collaboration

Even without interaction:
- system aggregates discoveries  
- unlocks shared insights  

---

## 6.4 Discovery Feed

Examples:
- “Player X discovered a rare fragment”  
- “A new connection was found”  

---

## 6.5 Optional Friend Features

- share fragments  
- send helper bots  
- compare journals  

---

# 7. Helper System

## 7.1 Concept

Players can receive temporary helpers.

---

## 7.2 Types

- Excavator → faster digging  
- Scanner → reveals hidden fragments  
- Preserver → reduces damage risk  

---

## 7.3 Behavior

- time-limited  
- automated actions  
- no decision-making  

---

# 8. Progression System

## 8.1 Player Skills

- Excavation → larger dig area  
- Analysis → better hints  
- Preservation → safer excavation  

---

## 8.2 Knowledge Progression

- symbol decoding  
- cultural understanding  
- pattern recognition  

---

## 8.3 Layer Progression

- deeper layers unlock over time  
- deeper = more complex artifacts  

---

# 9. Content Structure

## 9.1 Initial Scope

- 1 culture  
- 20–40 artifacts  
- 100–200 fragments  
- 3–5 layers  

---

## 9.2 Artifact Clusters

- burial practices  
- tools  
- symbolic language  
- ritual objects  

---

## 9.3 Connection Density

Each artifact connects to:
- 2–5 others  

---

# 10. UX & Presentation

## 10.1 Visual Style

- minimalist  
- clean silhouettes  
- symbolic details  

---

## 10.2 Core Screens

- Dig view (grid)  
- Artifact journal  
- Comparison view  
- Discovery feed  

---

## 10.3 Matching UX

- ghost overlays  
- highlight edges  
- suggest connections  

---

# 11. Monetization (Optional)

## 11.1 Principles

- Do not sell answers  
- Do not block thinking  

---

## 11.2 Options

- energy (soft limit)  
- helper upgrades  
- analysis boosts  
- cosmetics  

---

# 12. Technical Considerations

## 12.1 Backend

- artifact graph storage  
- fragment distribution  
- match detection  
- async communication  

---

## 12.2 Client

- simple grid rendering  
- reusable UI  
- minimal animation  

---

# 13. MVP Definition

## 13.1 Must Have

- digging system  
- fragment discovery  
- artifact journal  
- basic matching  
- hint system  

---

## 13.2 Nice to Have

- async sharing  
- helper system  
- discovery feed  

---

## 13.3 Not Needed Initially

- real-time multiplayer  
- guilds  
- complex economy  

---

# 14. Success Criteria

- players form interpretations  
- players experience “aha” moments  
- players care about connections  
- players share discoveries  

---

# 15. Design Pillars

1. Discovery over collection  
2. Interpretation over certainty  
3. Connection over completion  
4. Simplicity in input, depth in meaning  