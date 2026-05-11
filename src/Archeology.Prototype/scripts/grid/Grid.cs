using Godot;
using System;
using System.Collections.Generic;

namespace Archeology.Prototype;

public partial class Grid : Node2D
{
	[Export] public int Width { get; set; } = 28;
	[Export] public int Height { get; set; } = 16;
	[Export] public int TileSize { get; set; } = 36;
	[Export] public int Seed { get; set; } = 1337;
	[Export] public int FragmentTarget { get; set; } = 6;
	// Number of dig-able layers. Depths range over [0, LayerCount); reaching
	// LayerCount means the tile has been dug past all material (bedrock).
	[Export] public int LayerCount { get; set; } = 4;

	[Signal] public delegate void FragmentsChangedEventHandler(int count);

	// Layered world model:
	//   _layerTypes[x, y, d]  — material at depth d
	//   _layerHp[x, y, d]     — HP of the material at depth d
	//   _depth[x, y]          — the depth currently visible at this tile
	//   _fragmentAt[x, y, d]  — fragment overlay; same fragment can occupy multiple cells at the same depth
	private TileType[,,] _layerTypes = new TileType[0, 0, 0];
	private int[,,] _layerHp = new int[0, 0, 0];
	private int[,] _depth = new int[0, 0];
	private Fragment?[,,] _fragmentAt = new Fragment?[0, 0, 0];

	private List<Fragment> _fragments = new();
	private List<Fragment> _collectedFragments = new();

	public int FragmentsCollected { get; private set; }
	public IReadOnlyList<Fragment> CollectedFragments => _collectedFragments;

	public override void _Ready()
	{
		Generate();
	}

	public void Generate()
	{
		_layerTypes = new TileType[Width, Height, LayerCount];
		_layerHp = new int[Width, Height, LayerCount];
		_depth = new int[Width, Height];
		_fragmentAt = new Fragment?[Width, Height, LayerCount];
		_fragments = new List<Fragment>();
		_collectedFragments = new List<Fragment>();
		FragmentsCollected = 0;

		var rng = new Random(Seed);

		// Lay down terrain at every (x, y, depth).
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				for (int d = 0; d < LayerCount; d++)
				{
					if (rng.NextDouble() < 0.32)
					{
						_layerTypes[x, y, d] = TileType.Stone;
						_layerHp[x, y, d] = 2;
					}
					else
					{
						_layerTypes[x, y, d] = TileType.Soil;
						_layerHp[x, y, d] = 1;
					}
				}
			}
		}

		SpawnFragments(rng, FragmentTarget);

		EmitSignal(SignalName.FragmentsChanged, FragmentsCollected);
		QueueRedraw();
	}

	private void SpawnFragments(Random rng, int target)
	{
		if (LayerCount <= 1) return; // no non-topmost layer to place into

		var shapes = (FragmentShape[])Enum.GetValues(typeof(FragmentShape));
		const int maxAttempts = 500;
		int attempts = 0;
		while (_fragments.Count < target && attempts < maxAttempts)
		{
			attempts++;
			var shape = shapes[rng.Next(shapes.Length)];
			var template = Fragment.Template(shape);
			int ax = rng.Next(Width);
			int ay = rng.Next(Height);
			// Depth 0 is the topmost layer — fragments live on 1..LayerCount-1.
			int depth = 1 + rng.Next(LayerCount - 1);

			var abs = new Vector2I[template.Count];
			bool fits = true;
			for (int i = 0; i < template.Count; i++)
			{
				int cx = ax + template[i].X;
				int cy = ay + template[i].Y;
				if (!InBounds(cx, cy) || _fragmentAt[cx, cy, depth] != null)
				{
					fits = false;
					break;
				}
				abs[i] = new Vector2I(cx, cy);
			}
			if (!fits) continue;

			var frag = new Fragment(_fragments.Count, shape, depth, abs);
			_fragments.Add(frag);
			foreach (var c in abs) _fragmentAt[c.X, c.Y, depth] = frag;
		}
	}

	public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

	public TileType GetTile(int x, int y) => InBounds(x, y) && _depth[x, y] < LayerCount
		? _layerTypes[x, y, _depth[x, y]]
		: TileType.Empty;

	public int GetDepth(int x, int y) => InBounds(x, y) ? _depth[x, y] : 0;

	public Vector2I WorldToCell(Vector2 worldPosition)
	{
		var local = ToLocal(worldPosition);
		return new Vector2I(
			(int)Math.Floor(local.X / TileSize),
			(int)Math.Floor(local.Y / TileSize));
	}

	// Single click entry point. If a fragment cell is at the current depth,
	// try to collect (else block); otherwise dig the current layer's cover.
	public void HandleClick(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return;
		int d = _depth[cell.X, cell.Y];
		if (d < LayerCount && _fragmentAt[cell.X, cell.Y, d] != null)
		{
			TryCollectFragment(cell);
			return;
		}
		Dig(cell);
	}

	// Damage the current layer's HP, advancing depth when it drains to zero.
	// Blocked by the "one deeper than surrounds" constraint and by bedrock.
	public void Dig(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return;
		int d = _depth[cell.X, cell.Y];
		if (d >= LayerCount) return; // bedrock: no material left
		if (!CanDigDeeper(cell.X, cell.Y)) return; // step constraint blocks this dig

		_layerHp[cell.X, cell.Y, d]--;
		if (_layerHp[cell.X, cell.Y, d] <= 0)
		{
			_depth[cell.X, cell.Y] = d + 1;
		}
		QueueRedraw();
	}

	// A tile may advance depth only if every in-bound 4-neighbor is already
	// at depth >= current depth — i.e., the dig won't make this tile more than
	// one layer deeper than any of its surroundings.
	private bool CanDigDeeper(int x, int y)
	{
		int d = _depth[x, y];
		ReadOnlySpan<(int dx, int dy)> n = stackalloc (int, int)[]
		{
			(1, 0), (-1, 0), (0, 1), (0, -1)
		};
		foreach (var (dx, dy) in n)
		{
			int nx = x + dx;
			int ny = y + dy;
			if (!InBounds(nx, ny)) continue;
			if (_depth[nx, ny] < d) return false;
		}
		return true;
	}

	public bool TryCollectFragment(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return false;
		int d = _depth[cell.X, cell.Y];
		if (d >= LayerCount) return false;
		var frag = _fragmentAt[cell.X, cell.Y, d];
		if (frag == null) return false;
		if (!IsFragmentFullyExposed(frag)) return false;

		// Collection consumes the fragment's layer at every cell: depth advances past it.
		foreach (var c in frag.Cells)
		{
			_fragmentAt[c.X, c.Y, frag.Depth] = null;
			_depth[c.X, c.Y] = frag.Depth + 1;
		}
		_fragments.Remove(frag);
		_collectedFragments.Add(frag);
		FragmentsCollected++;
		EmitSignal(SignalName.FragmentsChanged, FragmentsCollected);
		QueueRedraw();
		return true;
	}

	private bool IsFragmentFullyExposed(Fragment frag)
	{
		foreach (var c in frag.Cells)
		{
			if (_depth[c.X, c.Y] != frag.Depth) return false;
		}
		return true;
	}

	// True if any 4-neighbor's current depth is strictly deeper than the threshold.
	// Used to surface fragment hints (neighbor dug past the fragment's layer).
	private bool AnyNeighborDeeperThan(int x, int y, int threshold)
	{
		ReadOnlySpan<(int dx, int dy)> n = stackalloc (int, int)[]
		{
			(1, 0), (-1, 0), (0, 1), (0, -1)
		};
		foreach (var (dx, dy) in n)
		{
			int nx = x + dx;
			int ny = y + dy;
			if (!InBounds(nx, ny)) continue;
			if (_depth[nx, ny] > threshold) return true;
		}
		return false;
	}

	public override void _Draw()
	{
		// Floors first, walls on top so the wall caps the deeper tile's edge.
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				var rect = new Rect2(x * TileSize, y * TileSize, TileSize, TileSize);
				DrawRect(rect, FloorColorFor(x, y));
			}
		}
		DrawWalls();
	}

	private Color FloorColorFor(int x, int y)
	{
		int d = _depth[x, y];

		// Bedrock — dug past every layer.
		if (d >= LayerCount) return new Color(0.05f, 0.04f, 0.03f);

		// Fragment at the current depth: exposed from above.
		var frag = _fragmentAt[x, y, d];
		if (frag != null)
		{
			return IsFragmentFullyExposed(frag)
				// bright pale gold — whole shape exposed, click any cell to collect
				? Darken(new Color(1.00f, 0.92f, 0.55f), d)
				// standard gold — this cell exposed, others still buried
				: Darken(new Color(1.00f, 0.82f, 0.32f), d);
		}

		// Hint: a fragment exists at a deeper layer of this tile, AND a neighbor
		// has been dug past that fragment's layer (so the wall would expose it).
		for (int probe = d + 1; probe < LayerCount; probe++)
		{
			var deeperFrag = _fragmentAt[x, y, probe];
			if (deeperFrag == null) continue;
			if (AnyNeighborDeeperThan(x, y, probe))
			{
				// muted ochre — a fragment hides under this tile and a neighbor revealed it
				return Darken(new Color(0.60f, 0.50f, 0.28f), d);
			}
			break; // only the shallowest fragment in this column matters for the hint
		}

		// Plain material at this layer, depth-darkened.
		return Darken(MaterialColor(_layerTypes[x, y, d], _layerHp[x, y, d]), d);
	}

	private static Color MaterialColor(TileType type, int hp) => type switch
	{
		// warm earthy brown — soil
		TileType.Soil => new Color(0.42f, 0.30f, 0.18f),
		// undamaged vs cracked stone
		TileType.Stone => hp >= 2
			? new Color(0.42f, 0.42f, 0.48f)
			: new Color(0.58f, 0.58f, 0.64f),
		// emptied tile (rare in layered model — placeholder)
		TileType.Empty => new Color(0.10f, 0.08f, 0.07f),
		_ => Colors.Magenta,
	};

	// Floor darkening per depth, per VISUALS.md.
	private static Color Darken(Color c, int depth)
	{
		float factor = depth switch
		{
			0 => 1.00f,
			1 => 0.85f,
			2 => 0.75f,
			_ => 0.70f,
		};
		return new Color(c.R * factor, c.G * factor, c.B * factor, c.A);
	}

	private void DrawWalls()
	{
		int wallSize = Math.Max(2, TileSize / 8);

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				int d = _depth[x, y];
				TryDrawWall(x, y, d, 1, 0, Side.Right, wallSize);
				TryDrawWall(x, y, d, -1, 0, Side.Left, wallSize);
				TryDrawWall(x, y, d, 0, 1, Side.Bottom, wallSize);
				TryDrawWall(x, y, d, 0, -1, Side.Top, wallSize);
			}
		}
	}

	private enum Side { Top, Right, Bottom, Left }

	// Draws a wall on (x, y)'s edge facing (x+dx, y+dy) when this tile is
	// strictly deeper than the neighbor. Bottom/right edges read stronger
	// than top/left, per VISUALS.md.
	private void TryDrawWall(int x, int y, int d, int dx, int dy, Side side, int wallSize)
	{
		int nx = x + dx;
		int ny = y + dy;
		if (!InBounds(nx, ny)) return;
		int nd = _depth[nx, ny];
		if (d <= nd) return;

		var strong = new Color(0.04f, 0.03f, 0.02f);
		var light = new Color(0.12f, 0.10f, 0.08f);
		bool emphasised = side == Side.Right || side == Side.Bottom;
		var color = emphasised ? strong : light;

		Rect2 rect = side switch
		{
			Side.Right => new Rect2(x * TileSize + (TileSize - wallSize), y * TileSize, wallSize, TileSize),
			Side.Left => new Rect2(x * TileSize, y * TileSize, wallSize, TileSize),
			Side.Bottom => new Rect2(x * TileSize, y * TileSize + (TileSize - wallSize), TileSize, wallSize),
			Side.Top => new Rect2(x * TileSize, y * TileSize, TileSize, wallSize),
			_ => default,
		};
		DrawRect(rect, color);
	}
}
