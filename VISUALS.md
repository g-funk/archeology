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

- Bottom and right edges should be visually stronger (more visible)
- Top and left edges can be slightly lighter or partially hidden

### Appearance

- Walls should be darker than the floor
- Add a subtle vertical gradient (lighter at top, darker at bottom)
- Keep style simple and consistent

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
- Emphasize bottom/right edges for clarity
- Use subtle darkening to support the effect