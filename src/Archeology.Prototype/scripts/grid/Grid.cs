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
	// Fragment count is a random pick in [MinFragments, MaxFragments].
	[Export] public int MinFragments { get; set; } = 4;
	[Export] public int MaxFragments { get; set; } = 8;
	// Each fragment is a randomly grown polyomino with a tile count picked
	// from [MinFragmentTiles, MaxFragmentTiles].
	[Export] public int MinFragmentTiles { get; set; } = 4;
	[Export] public int MaxFragmentTiles { get; set; } = 16;
	// Number of dig-able layers. Depths range over [0, LayerCount); reaching
	// LayerCount means the tile has been dug past all material (bedrock).
	[Export] public int LayerCount { get; set; } = 4;
	// Random collapse: 0..MaxCollapse neighbors of the dug tile may also have
	// their current layer disappear, with `CollapseChance` per neighbor.
	[Export] public int MaxCollapse { get; set; } = 2;
	[Export] public float CollapseChance { get; set; } = 0.15f;

	[Signal] public delegate void FragmentsChangedEventHandler(int count);
	// Fires when a click results in a successful dig action (HP damaged on the
	// current layer). Used by the ping system to surface nearby fragments.
	[Signal] public delegate void DugEventHandler(int x, int y, int depth);
	// Fires when a dig attempt is rejected by the step constraint (some neighbor
	// is shallower than this tile). Used by the hints system to flash the
	// preventing neighbor(s).
	[Signal] public delegate void DigBlockedEventHandler(int x, int y);
	// Fires for every in-bounds click on the grid — regardless of whether the
	// click ends up digging, collecting, or being blocked. The character
	// listens for this to move toward the clicked tile.
	[Signal] public delegate void ClickedEventHandler(int x, int y);
	// Fires when the player triggers a scan (S key, or long-click arrival).
	// Carries the tile the scan emanates from and that tile's current depth.
	[Signal] public delegate void ScanTriggeredEventHandler(int x, int y, int depth);

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

	// Persistent RNG; seeded from `Seed` in Generate so terrain, fragment
	// placement, and runtime random collapses are all reproducible per seed.
	private Random _rng = new();

	public int FragmentsCollected { get; private set; }
	public IReadOnlyList<Fragment> CollectedFragments => _collectedFragments;
	public IReadOnlyList<Fragment> Fragments => _fragments;

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

		_rng = new Random(Seed);

		// Lay down terrain at every (x, y, depth).
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				for (int d = 0; d < LayerCount; d++)
				{
					if (_rng.NextDouble() < 0.32)
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

		SpawnFragments(_rng);

		EmitSignal(SignalName.FragmentsChanged, FragmentsCollected);
		QueueRedraw();
	}

	private void SpawnFragments(Random rng)
	{
		if (LayerCount <= 1) return; // no non-topmost layer to place into

		int target = Math.Max(0, MinFragments) + rng.Next(Math.Max(0, MaxFragments - MinFragments) + 1);
		const int maxAttempts = 500;
		int attempts = 0;
		while (_fragments.Count < target && attempts < maxAttempts)
		{
			attempts++;

			int tileCount = Math.Max(1, MinFragmentTiles) + rng.Next(Math.Max(0, MaxFragmentTiles - MinFragmentTiles) + 1);
			var cells = GenerateRandomShape(tileCount, rng);

			int shapeW = 1, shapeH = 1;
			foreach (var c in cells)
			{
				if (c.X + 1 > shapeW) shapeW = c.X + 1;
				if (c.Y + 1 > shapeH) shapeH = c.Y + 1;
			}
			if (shapeW > Width || shapeH > Height) continue;

			int ax = rng.Next(Width - shapeW + 1);
			int ay = rng.Next(Height - shapeH + 1);
			// Depth 0 is the topmost layer — fragments live on 1..LayerCount-1.
			int depth = 1 + rng.Next(LayerCount - 1);

			var abs = new Vector2I[cells.Length];
			bool fits = true;
			for (int i = 0; i < cells.Length; i++)
			{
				int cx = ax + cells[i].X;
				int cy = ay + cells[i].Y;
				if (!InBounds(cx, cy) || _fragmentAt[cx, cy, depth] != null)
				{
					fits = false;
					break;
				}
				abs[i] = new Vector2I(cx, cy);
			}
			if (!fits) continue;

			// FragmentShape is left at a sentinel value — RelativeCells drives rendering now.
			var frag = new Fragment(_fragments.Count, FragmentShape.SquareTwo, depth, abs);
			_fragments.Add(frag);
			foreach (var c in abs) _fragmentAt[c.X, c.Y, depth] = frag;
		}
	}

	// Grows a random connected polyomino: start with one cell, repeatedly pick
	// a random empty 4-neighbor of the current shape and add it. Result is
	// normalized so the minimum X and Y are both 0.
	private static Vector2I[] GenerateRandomShape(int tileCount, Random rng)
	{
		ReadOnlySpan<Vector2I> dirs = stackalloc Vector2I[]
		{
			new Vector2I(1, 0), new Vector2I(-1, 0),
			new Vector2I(0, 1), new Vector2I(0, -1),
		};

		var cells = new HashSet<Vector2I>();
		var perimeter = new List<Vector2I>();

		var seed = new Vector2I(0, 0);
		cells.Add(seed);
		foreach (var dir in dirs) perimeter.Add(seed + dir);

		while (cells.Count < tileCount && perimeter.Count > 0)
		{
			int idx = rng.Next(perimeter.Count);
			var newCell = perimeter[idx];
			perimeter.RemoveAt(idx);

			if (!cells.Add(newCell)) continue; // duplicate entry in the list, skip

			foreach (var dir in dirs)
			{
				var nb = newCell + dir;
				if (!cells.Contains(nb)) perimeter.Add(nb);
			}
		}

		int minX = int.MaxValue, minY = int.MaxValue;
		foreach (var c in cells)
		{
			if (c.X < minX) minX = c.X;
			if (c.Y < minY) minY = c.Y;
		}
		var result = new Vector2I[cells.Count];
		int i2 = 0;
		foreach (var c in cells) result[i2++] = new Vector2I(c.X - minX, c.Y - minY);
		return result;
	}

	public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

	public TileType GetTile(int x, int y) => InBounds(x, y) && _depth[x, y] < LayerCount
		? _layerTypes[x, y, _depth[x, y]]
		: TileType.Empty;

	public int GetDepth(int x, int y) => InBounds(x, y) ? _depth[x, y] : 0;

	public Vector2I WorldToCell(Vector2 worldPosition)
	{
		var local = ToLocal(worldPosition);
		int rawX = (int)Math.Floor(local.X / TileSize);
		int rawY = (int)Math.Floor(local.Y / TileSize);
		var raw = new Vector2I(rawX, rawY);
		if (!InBounds(rawX, rawY)) return raw;

		// Walls live inside the deeper tile's outer pixels (see DrawWalls). A
		// click that lands on a wall reads visually as the shallow neighbor's
		// edge, so map those pixels to that neighbor instead — the tile the
		// player almost certainly intended.
		int d = _depth[rawX, rawY];
		float lx = local.X - rawX * TileSize;
		float ly = local.Y - rawY * TileSize;
		int unit = Math.Max(2, TileSize / 10);
		ReadOnlySpan<(int dx, int dy)> dirs = stackalloc (int, int)[]
		{
			(1, 0), (-1, 0), (0, 1), (0, -1)
		};
		foreach (var (dx, dy) in dirs)
		{
			int nx = rawX + dx;
			int ny = rawY + dy;
			if (!InBounds(nx, ny)) continue;
			int nd = _depth[nx, ny];
			if (d <= nd) continue; // no wall here — neighbor isn't shallower
			float wallSize = unit * (d - nd);
			bool inWall =
				(dx == 1 && lx >= TileSize - wallSize) ||
				(dx == -1 && lx < wallSize) ||
				(dy == 1 && ly >= TileSize - wallSize) ||
				(dy == -1 && ly < wallSize);
			if (inWall) return new Vector2I(nx, ny);
		}
		return raw;
	}

	// Single click entry point. If a fragment cell is at the current depth,
	// try to collect (else block); otherwise dig the current layer's cover.
	public void HandleClick(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return;
		EmitSignal(SignalName.Clicked, cell.X, cell.Y);
		int d = _depth[cell.X, cell.Y];
		if (d < LayerCount && _fragmentAt[cell.X, cell.Y, d] != null)
		{
			TryCollectFragment(cell);
			return;
		}
		Dig(cell);
	}

	// Player-triggered scan emit. The radar listens for ScanTriggered.
	public void TriggerScan(int x, int y, int depth)
	{
		EmitSignal(SignalName.ScanTriggered, x, y, depth);
	}

	public enum DigResult
	{
		// The current layer at this tile cleared; depth advanced.
		Cleared,
		// HP at the current layer took a hit but the layer isn't cleared yet.
		Damaged,
		// No HP change — out of bounds, bedrock, or step-constraint blocked.
		// When the step constraint blocks, `DigBlocked` is emitted (same red
		// flash as manual dig blocks).
		Blocked,
	}

	// Damage the current layer's HP, advancing depth when it drains to zero.
	// Blocked by the "one deeper than surrounds" constraint and by bedrock.
	// `allowCollapse` lets the caller opt out of the random-collapse side
	// effect (e.g. autodig sweeps the area deterministically).
	public DigResult Dig(Vector2I cell, bool allowCollapse = true)
	{
		if (!InBounds(cell.X, cell.Y)) return DigResult.Blocked;
		int d = _depth[cell.X, cell.Y];
		if (d >= LayerCount)
		{
			// Bedrock — emit `DigBlocked` so the hint flash fires; HintsSystem
			// knows to flash the attempted tile itself when there's no preventer.
			EmitSignal(SignalName.DigBlocked, cell.X, cell.Y);
			return DigResult.Blocked;
		}
		if (_fragmentAt[cell.X, cell.Y, d] != null)
		{
			// Fragment at the current depth — Dig would grind through it. Manual
			// clicks route to TryCollectFragment instead (HandleClick), so this
			// only triggers for direct Dig callers like autodig.
			EmitSignal(SignalName.DigBlocked, cell.X, cell.Y);
			return DigResult.Blocked;
		}
		if (!CanDigDeeper(cell.X, cell.Y))
		{
			EmitSignal(SignalName.DigBlocked, cell.X, cell.Y);
			return DigResult.Blocked; // step constraint blocks this dig
		}

		_layerHp[cell.X, cell.Y, d]--;
		EmitSignal(SignalName.Dug, cell.X, cell.Y, d);
		bool cleared = false;
		if (_layerHp[cell.X, cell.Y, d] <= 0)
		{
			_depth[cell.X, cell.Y] = d + 1;
			cleared = true;
		}
		if (allowCollapse) TryRandomCollapse(cell.X, cell.Y);
		QueueRedraw();
		return cleared ? DigResult.Cleared : DigResult.Damaged;
	}

	// True if there's a fragment overlay at `(x, y, d)`. Autodig consults this
	// to avoid grinding through buried fragments at the character's depth.
	public bool HasFragmentAt(int x, int y, int d)
	{
		if (!InBounds(x, y)) return false;
		if (d < 0 || d >= LayerCount) return false;
		return _fragmentAt[x, y, d] != null;
	}

	// Each successful dig may take 0..MaxCollapse neighbors with it. Each
	// in-bound 4-neighbor is rolled independently against CollapseChance, and
	// each successful roll has to pass the same "cannot dig" rules as a manual
	// dig (step constraint, bedrock, fragment block) before its current layer
	// is removed. Directions are shuffled so we don't bias by iteration order
	// when the MaxCollapse cap kicks in.
	private void TryRandomCollapse(int x, int y)
	{
		if (MaxCollapse <= 0 || CollapseChance <= 0f) return;

		Span<Vector2I> dirs = stackalloc Vector2I[]
		{
			new Vector2I(1, 0), new Vector2I(-1, 0),
			new Vector2I(0, 1), new Vector2I(0, -1),
		};
		// Fisher-Yates shuffle so the cap doesn't favour right/down.
		for (int i = dirs.Length - 1; i > 0; i--)
		{
			int j = _rng.Next(i + 1);
			(dirs[i], dirs[j]) = (dirs[j], dirs[i]);
		}

		int collapsed = 0;
		foreach (var dir in dirs)
		{
			if (collapsed >= MaxCollapse) break;
			if (_rng.NextDouble() >= CollapseChance) continue;
			if (TryCollapse(new Vector2I(x + dir.X, y + dir.Y)))
				collapsed++;
		}
	}

	// Advance a single tile's depth by one layer, regardless of remaining HP,
	// as long as the "cannot dig" rules allow it. Silent — no signals fire,
	// so collapses don't generate pings or hint flashes.
	public bool TryCollapse(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return false;
		int d = _depth[cell.X, cell.Y];
		if (d >= LayerCount) return false; // bedrock
		if (_fragmentAt[cell.X, cell.Y, d] != null) return false; // fragment blocks
		if (!CanDigDeeper(cell.X, cell.Y)) return false; // step constraint

		_depth[cell.X, cell.Y] = d + 1;
		return true;
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
		// Per-depth-step thickness. The actual wall width is this × (d - nd).
		int unit = Math.Max(2, TileSize / 10);

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				int d = _depth[x, y];
				TryDrawWall(x, y, d, 1, 0, Side.Right, unit);
				TryDrawWall(x, y, d, -1, 0, Side.Left, unit);
				TryDrawWall(x, y, d, 0, 1, Side.Bottom, unit);
				TryDrawWall(x, y, d, 0, -1, Side.Top, unit);
			}
		}
	}

	private enum Side { Top, Right, Bottom, Left }

	// Draws a wall on (x, y)'s edge facing (x+dx, y+dy) when this tile is
	// strictly deeper than the neighbor. Wall thickness scales with the depth
	// gap so a 3-layer drop reads deeper than a 1-layer drop.
	private void TryDrawWall(int x, int y, int d, int dx, int dy, Side side, int unit)
	{
		int nx = x + dx;
		int ny = y + dy;
		if (!InBounds(nx, ny)) return;
		int nd = _depth[nx, ny];
		if (d <= nd) return;
		int wallSize = unit * (d - nd);

		// Top/left walls are shadowed and bottom/right walls are highlighted —
		// the deeper tile reads as a pit with the bright wall catching light at
		// its far edges.
		var shadow = new Color(0.04f, 0.03f, 0.02f);
		var highlight = new Color(0.75f, 0.75f, 0.73f);
		bool isHighlight = side == Side.Right || side == Side.Bottom;
		var color = isHighlight ? highlight : shadow;

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
