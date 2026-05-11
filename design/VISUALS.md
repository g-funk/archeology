# 🧱 VISUALS.md — Walls & Depth

## Drawing Walls

Walls are drawn to show where the ground drops between tiles.

For each tile:

- Compare it to its four neighbors (up, down, left, right)
- If the tile is deeper than a neighbor:
  → draw a wall on that edge

### Direction

- Draw walls on edges where a deeper tile meets a shallower tile
- No wall is drawn between tiles of equal depth

### Visibility

- Top and left walls are dark (shadowed near edge of the pit)
- Bottom and right walls are bright (far edge catching light)
- The dark-near / bright-far pairing makes a deeper tile read as a recessed pit

### Appearance

- Top/left shadow walls: near-black, much darker than the surrounding floor
- Bottom/right highlight walls: a near-neutral light gray, noticeably brighter than the surrounding floor
- Keep style simple and consistent (a subtle vertical gradient may be added later)

---

## Floor Darkening

Depth is also shown by slightly darkening the floor.

Use a clamped brightness scale:

depth 0 → 1.00
depth 1 → 0.85
depth 2 → 0.75
depth 3+ → 0.70

## Summary

- Walls mark depth differences
- Only draw walls where depth changes
- Top/left walls dark, bottom/right walls bright (deeper tiles read as pits)
- Use subtle floor darkening to reinforce depth