using Godot;
using System;
using System.Collections.Generic;

namespace Archeology.Prototype;

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
	public FragmentShape Shape { get; }
	public IReadOnlyList<Vector2I> Cells { get; }

	public Fragment(int id, FragmentShape shape, IReadOnlyList<Vector2I> cells)
	{
		Id = id;
		Shape = shape;
		Cells = cells;
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
