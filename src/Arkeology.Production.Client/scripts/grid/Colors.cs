using Godot;

namespace Arkeology.Production.Client;

public static class GameColors
{
    // ── Surroundings ──────────────────────────────────────────────────────────
    public static readonly Color Grass = new(0.42f, 0.65f, 0.26f);
    public static readonly Color Sand  = new(0.86f, 0.78f, 0.54f);

    // ── Tile materials ────────────────────────────────────────────────────────
    public static readonly Color Soil         = new(0.92f, 0.84f, 0.60f);
    public static readonly Color StoneFull    = new(0.52f, 0.52f, 0.58f);
    public static readonly Color StoneDamaged = new(0.66f, 0.66f, 0.71f);
    public static readonly Color Void         = new(0.13f, 0.10f, 0.08f); // TileType.Empty
    public static readonly Color DepthFloor   = new(0.08f, 0.06f, 0.04f); // fully excavated

    // ── Fragment states ───────────────────────────────────────────────────────
    public static readonly Color FragmentExposed = new(1.00f, 0.92f, 0.55f);
    public static readonly Color FragmentBuried  = new(1.00f, 0.82f, 0.32f);
    public static readonly Color FragmentHint    = new(0.80f, 0.70f, 0.42f); // deeper fragment visible through neighbor

    // ── Walls ─────────────────────────────────────────────────────────────────
    public static readonly Color WallShadow    = new(0.04f, 0.03f, 0.02f);
    public static readonly Color WallHighlight = new(0.75f, 0.75f, 0.73f);

    // ── Outlines ──────────────────────────────────────────────────────────────
    public static readonly Color HexOutline = new(0.38f, 0.24f, 0.10f);
}
