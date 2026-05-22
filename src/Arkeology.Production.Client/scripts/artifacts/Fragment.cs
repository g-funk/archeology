using Godot;
using System;
using System.Collections.Generic;

namespace Arkeology.Production.Client;

public enum FragmentShape
{
	SquareTwo,
	BoxThree,
	Plus,
	Corner,
}

public class Fragment
{
	public int Id { get; }
	// Legacy: only meaningful when a predefined Template was used. With random
	// generation this is left at a default and not consulted for rendering.
	public FragmentShape Shape { get; }
	// Layer the whole shape lives on. All cells share the same depth.
	// Always > 0 — fragments are never on the topmost layer.
	public int Depth { get; }
	public IReadOnlyList<Vector2I> Cells { get; }

	public Fragment(int id, FragmentShape shape, int depth, IReadOnlyList<Vector2I> cells)
	{
		Id = id;
		Shape = shape;
		Depth = depth;
		Cells = cells;
	}

	// `Cells` shifted so the minimum X and Y are 0 — i.e. the shape's intrinsic
	// layout independent of where it sits on the grid. Computed lazily.
	private IReadOnlyList<Vector2I>? _relativeCells;
	public IReadOnlyList<Vector2I> RelativeCells
	{
		get
		{
			if (_relativeCells != null) return _relativeCells;
			int minX = int.MaxValue, minY = int.MaxValue;
			foreach (var c in Cells)
			{
				if (c.X < minX) minX = c.X;
				if (c.Y < minY) minY = c.Y;
			}
			var rel = new Vector2I[Cells.Count];
			for (int i = 0; i < Cells.Count; i++)
			{
				rel[i] = new Vector2I(Cells[i].X - minX, Cells[i].Y - minY);
			}
			_relativeCells = rel;
			return _relativeCells;
		}
	}

	// Cell offsets from the top-left anchor for each supported shape.
	public static IReadOnlyList<Vector2I> Template(FragmentShape shape) => shape switch
	{
		// 2x2 square — 4 tiles
		// X X
		// X X
		FragmentShape.SquareTwo => new[]
		{
			new Vector2I(0, 0), new Vector2I(1, 0),
			new Vector2I(0, 1), new Vector2I(1, 1),
		},

		// 3x3 hollow box — 8 tiles, empty middle
		// X X X
		// X . X
		// X X X
		FragmentShape.BoxThree => new[]
		{
			new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0),
			new Vector2I(0, 1),                     new Vector2I(2, 1),
			new Vector2I(0, 2), new Vector2I(1, 2), new Vector2I(2, 2),
		},

		// Plus — 5 tiles inside a 3x3
		// . X .
		// X X X
		// . X .
		FragmentShape.Plus => new[]
		{
								new Vector2I(1, 0),
			new Vector2I(0, 1), new Vector2I(1, 1), new Vector2I(2, 1),
								new Vector2I(1, 2),
		},

		// Corner — 5 tiles inside a 3x3 (top row + left column)
		// X X X
		// X . .
		// X . .
		FragmentShape.Corner => new[]
		{
			new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0),
			new Vector2I(0, 1),
			new Vector2I(0, 2),
		},

		_ => Array.Empty<Vector2I>(),
	};
}
