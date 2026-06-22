using Godot;
using System;

namespace Arkeology.Production.Client;

// Pointy-top hexagonal grid math, odd-r offset coordinates.
// `R` is the circumradius (center-to-vertex distance).
// Odd rows are shifted right by half a hex width relative to even rows.
public static class HexMetrics
{
    private const float Sqrt3 = 1.7320508f;

    // Pixel center of hex cell (col, row). Cell (0,0) sits at (w/2, R) so that
    // the leftmost edge of column 0 is at x=0 and the top vertex of row 0 is at y=0.
    public static Vector2 CellCenter(int col, int row, float R)
    {
        float w = R * Sqrt3;
        float x = col * w + ((row & 1) == 1 ? w : w * 0.5f);
        float y = row * R * 1.5f + R;
        return new Vector2(x, y);
    }

    // Pixel bounding size of a Width x Height hex grid.
    public static Vector2 GridPixelSize(int width, int height, float R)
    {
        float w = R * Sqrt3;
        return new Vector2((width + 0.5f) * w, R * (height * 1.5f + 0.5f));
    }

    // Nearest hex cell (col, row) for a local pixel position.
    public static Vector2I WorldToCell(Vector2 pos, float R)
    {
        float w = R * Sqrt3;
        // Undo the cell (0,0) center offset so axial coords work from the origin.
        float x = pos.X - w * 0.5f;
        float y = pos.Y - R;

        // Fractional cube coordinates for pointy-top hex.
        float qf = (Sqrt3 / 3f * x - y / 3f) / R;
        float rf = (2f / 3f * y) / R;
        float sf = -qf - rf;

        int q = (int)MathF.Round(qf, MidpointRounding.AwayFromZero);
        int r = (int)MathF.Round(rf, MidpointRounding.AwayFromZero);
        int s = (int)MathF.Round(sf, MidpointRounding.AwayFromZero);

        float dq = MathF.Abs(q - qf);
        float dr = MathF.Abs(r - rf);
        float ds = MathF.Abs(s - sf);

        if (dq > dr && dq > ds) q = -r - s;
        else if (dr > ds) r = -q - s;

        // Cube (q, r) to odd-r offset.
        return new Vector2I(q + (r - (r & 1)) / 2, r);
    }

    // Writes the 6 vertices of a pointy-top hex centered at `center` into `verts`.
    // v0=top, clockwise: v1=upper-right, v2=lower-right, v3=bottom, v4=lower-left, v5=upper-left.
    public static void HexVerticesAt(Vector2 center, float R, Vector2[] verts)
    {
        for (int i = 0; i < 6; i++)
        {
            float angle = -MathF.PI * 0.5f + i * MathF.PI / 3f;
            verts[i] = center + new Vector2(R * MathF.Cos(angle), R * MathF.Sin(angle));
        }
    }

    // Writes the 6 neighbors of (col, row) into `result` (odd-r offset order).
    // Indices: [0]=E, [1]=W, [2]=NE, [3]=NW, [4]=SE, [5]=SW.
    public static void GetNeighbors(int col, int row, Span<Vector2I> result)
    {
        if ((row & 1) == 1) // odd row: shifted right
        {
            result[0] = new Vector2I(col + 1, row);
            result[1] = new Vector2I(col - 1, row);
            result[2] = new Vector2I(col + 1, row - 1);
            result[3] = new Vector2I(col,     row - 1);
            result[4] = new Vector2I(col + 1, row + 1);
            result[5] = new Vector2I(col,     row + 1);
        }
        else // even row
        {
            result[0] = new Vector2I(col + 1, row);
            result[1] = new Vector2I(col - 1, row);
            result[2] = new Vector2I(col,     row - 1);
            result[3] = new Vector2I(col - 1, row - 1);
            result[4] = new Vector2I(col,     row + 1);
            result[5] = new Vector2I(col - 1, row + 1);
        }
    }

    // The two vertex indices (from HexVerticesAt) that form the shared edge with neighbor[k].
    // Neighbor order: [0]=E, [1]=W, [2]=NE, [3]=NW, [4]=SE, [5]=SW.
    public static (int va, int vb) EdgeForNeighbor(int k) => k switch
    {
        0 => (1, 2), // E  → right edge
        1 => (4, 5), // W  → left edge
        2 => (0, 1), // NE → upper-right edge
        3 => (5, 0), // NW → upper-left edge
        4 => (2, 3), // SE → lower-right edge
        5 => (3, 4), // SW → lower-left edge
        _ => (0, 1),
    };

    // True for edges facing "down-right" — highlighted (bright) like Right/Bottom walls
    // in the square grid. E, SE, SW catch the light from the lower-right direction.
    public static bool IsHighlightEdge(int k) => k is 0 or 4 or 5;
}
