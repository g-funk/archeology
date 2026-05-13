# Radar

A directional scan triggered explicitly by the player.

## Triggers

- **`S` key** — fires immediately at the character's current tile.
- **Long-click on a tile** — the character walks to that tile, and the scan fires when it arrives. A short click still digs as before.

## Behaviour

- The scan origin emits an expanding circle that fades by the time it reaches the scan radius (8 tiles by default).
- For each fragment within scan radius, a brighter wedge (~1/8 of the circle) highlights the approximate 2D direction to that fragment's closest cell. The wedge fades with the circle.
- Multiple fragments → one wedge each.
- A fragment is ignored once any of its cells is exposed (same as ping's "any-exposed" rule).
- Scan radius, fade length, wedge size, and brightnesses are configurable.
