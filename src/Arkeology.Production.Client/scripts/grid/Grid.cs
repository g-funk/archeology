using Godot;
using System;
using System.Collections.Generic;

namespace Arkeology.Production.Client;

public partial class Grid : Node2D
{
	[Export] public int Width { get; set; } = 28;
	[Export] public int Height { get; set; } = 16;
	// Hex circumradius (center-to-vertex) in pixels.
	[Export] public int TileSize { get; set; } = 22;
	[Export] public int Seed { get; set; } = 1337;
	[Export] public int MinFragments { get; set; } = 4;
	[Export] public int MaxFragments { get; set; } = 8;
	[Export] public int MinFragmentTiles { get; set; } = 4;
	[Export] public int MaxFragmentTiles { get; set; } = 16;
	[Export] public int LayerCount { get; set; } = 4;
	[Export] public int MaxCollapse { get; set; } = 2;
	[Export] public float CollapseChance { get; set; } = 0.15f;

	[Signal] public delegate void FragmentsChangedEventHandler(int count);
	[Signal] public delegate void DugEventHandler(int x, int y, int depth);
	[Signal] public delegate void DigBlockedEventHandler(int x, int y);
	[Signal] public delegate void ClickedEventHandler(int x, int y);
	[Signal] public delegate void ScanTriggeredEventHandler(int x, int y, int depth);

	private TileType[,,] _layerTypes = new TileType[0, 0, 0];
	private int[,,] _layerHp = new int[0, 0, 0];
	private int[,] _depth = new int[0, 0];
	private Fragment?[,,] _fragmentAt = new Fragment?[0, 0, 0];

	private List<Fragment> _fragments = new();
	private List<Fragment> _collectedFragments = new();
	private Random _rng = new();

	// Reused per _Draw call to avoid per-tile allocation.
	private readonly Vector2[] _hexVerts = new Vector2[6];

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
		if (LayerCount <= 1) return;

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

			var frag = new Fragment(_fragments.Count, FragmentShape.SquareTwo, depth, abs);
			_fragments.Add(frag);
			foreach (var c in abs) _fragmentAt[c.X, c.Y, depth] = frag;
		}
	}

	// Grows a random hex-connected polyomino. Starts with one cell; repeatedly
	// picks a random empty 6-neighbor and adds it. Result is normalized to min X=0, Y=0.
	private static Vector2I[] GenerateRandomShape(int tileCount, Random rng)
	{
		var cells = new HashSet<Vector2I>();
		var perimeter = new List<Vector2I>();

		var seed = new Vector2I(0, 0);
		cells.Add(seed);
		AddHexPerimeter(seed, cells, perimeter);

		while (cells.Count < tileCount && perimeter.Count > 0)
		{
			int idx = rng.Next(perimeter.Count);
			var newCell = perimeter[idx];
			perimeter.RemoveAt(idx);

			if (!cells.Add(newCell)) continue;
			AddHexPerimeter(newCell, cells, perimeter);
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

	private static void AddHexPerimeter(Vector2I cell, HashSet<Vector2I> cells, List<Vector2I> perimeter)
	{
		Span<Vector2I> neighbors = stackalloc Vector2I[6];
		HexMetrics.GetNeighbors(cell.X, cell.Y, neighbors);
		foreach (var n in neighbors)
		{
			if (!cells.Contains(n)) perimeter.Add(n);
		}
	}

	public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

	public TileType GetTile(int x, int y) => InBounds(x, y) && _depth[x, y] < LayerCount
		? _layerTypes[x, y, _depth[x, y]]
		: TileType.Empty;

	public int GetDepth(int x, int y) => InBounds(x, y) ? _depth[x, y] : 0;

	public Vector2I WorldToCell(Vector2 worldPosition)
	{
		return HexMetrics.WorldToCell(ToLocal(worldPosition), TileSize);
	}

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

	public void TriggerScan(int x, int y, int depth)
	{
		EmitSignal(SignalName.ScanTriggered, x, y, depth);
	}

	public enum DigResult { Cleared, Damaged, Blocked }

	public DigResult Dig(Vector2I cell, bool allowCollapse = true)
	{
		if (!InBounds(cell.X, cell.Y)) return DigResult.Blocked;
		int d = _depth[cell.X, cell.Y];
		if (d >= LayerCount)
		{
			EmitSignal(SignalName.DigBlocked, cell.X, cell.Y);
			return DigResult.Blocked;
		}
		if (_fragmentAt[cell.X, cell.Y, d] != null)
		{
			EmitSignal(SignalName.DigBlocked, cell.X, cell.Y);
			return DigResult.Blocked;
		}
		if (!CanDigDeeper(cell.X, cell.Y))
		{
			EmitSignal(SignalName.DigBlocked, cell.X, cell.Y);
			return DigResult.Blocked;
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

	public bool HasFragmentAt(int x, int y, int d)
	{
		if (!InBounds(x, y)) return false;
		if (d < 0 || d >= LayerCount) return false;
		return _fragmentAt[x, y, d] != null;
	}

	private void TryRandomCollapse(int x, int y)
	{
		if (MaxCollapse <= 0 || CollapseChance <= 0f) return;

		Span<Vector2I> dirs = stackalloc Vector2I[6];
		HexMetrics.GetNeighbors(x, y, dirs);

		// Fisher-Yates shuffle to avoid directional bias at the cap.
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
			if (TryCollapse(dir)) collapsed++;
		}
	}

	public bool TryCollapse(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return false;
		int d = _depth[cell.X, cell.Y];
		if (d >= LayerCount) return false;
		if (_fragmentAt[cell.X, cell.Y, d] != null) return false;
		if (!CanDigDeeper(cell.X, cell.Y)) return false;

		_depth[cell.X, cell.Y] = d + 1;
		return true;
	}

	// A tile may advance depth only if every in-bound hex neighbor is already
	// at depth >= this tile's current depth (one-step terrace rule, hex edition).
	private bool CanDigDeeper(int x, int y)
	{
		int d = _depth[x, y];
		Span<Vector2I> neighbors = stackalloc Vector2I[6];
		HexMetrics.GetNeighbors(x, y, neighbors);
		foreach (var n in neighbors)
		{
			if (!InBounds(n.X, n.Y)) continue;
			if (_depth[n.X, n.Y] < d) return false;
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

	// True if any hex neighbor's depth is strictly deeper than `threshold`.
	private bool AnyNeighborDeeperThan(int x, int y, int threshold)
	{
		Span<Vector2I> neighbors = stackalloc Vector2I[6];
		HexMetrics.GetNeighbors(x, y, neighbors);
		foreach (var n in neighbors)
		{
			if (!InBounds(n.X, n.Y)) continue;
			if (_depth[n.X, n.Y] > threshold) return true;
		}
		return false;
	}

	public override void _Draw()
	{
		// Floors first; walls on top to cap the deeper hex edges.
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				var center = HexMetrics.CellCenter(x, y, TileSize);
				HexMetrics.HexVerticesAt(center, TileSize, _hexVerts);
				DrawColoredPolygon(_hexVerts, FloorColorFor(x, y));
			}
		}
		DrawHexWalls();
	}

	private void DrawHexWalls()
	{
		int unit = Math.Max(2, TileSize / 10);
		var shadow    = new Color(0.04f, 0.03f, 0.02f);
		var highlight = new Color(0.75f, 0.75f, 0.73f);

		Span<Vector2I> neighbors = stackalloc Vector2I[6];

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				int d = _depth[x, y];
				var center = HexMetrics.CellCenter(x, y, TileSize);
				HexMetrics.HexVerticesAt(center, TileSize, _hexVerts);
				HexMetrics.GetNeighbors(x, y, neighbors);

				for (int i = 0; i < 6; i++)
				{
					var n = neighbors[i];
					if (!InBounds(n.X, n.Y)) continue;
					int nd = _depth[n.X, n.Y];
					if (d <= nd) continue; // this tile is not deeper than that neighbor

					int wallThick = unit * (d - nd);
					var (va, vb) = HexMetrics.EdgeForNeighbor(i);
					var color = HexMetrics.IsHighlightEdge(i) ? highlight : shadow;
					DrawLine(_hexVerts[va], _hexVerts[vb], color, wallThick);
				}
			}
		}
	}

	private Color FloorColorFor(int x, int y)
	{
		int d = _depth[x, y];

		if (d >= LayerCount) return new Color(0.05f, 0.04f, 0.03f);

		var frag = _fragmentAt[x, y, d];
		if (frag != null)
		{
			return IsFragmentFullyExposed(frag)
				? Darken(new Color(1.00f, 0.92f, 0.55f), d)
				: Darken(new Color(1.00f, 0.82f, 0.32f), d);
		}

		for (int probe = d + 1; probe < LayerCount; probe++)
		{
			var deeperFrag = _fragmentAt[x, y, probe];
			if (deeperFrag == null) continue;
			if (AnyNeighborDeeperThan(x, y, probe))
				return Darken(new Color(0.60f, 0.50f, 0.28f), d);
			break;
		}

		return Darken(MaterialColor(_layerTypes[x, y, d], _layerHp[x, y, d]), d);
	}

	private static Color MaterialColor(TileType type, int hp) => type switch
	{
		TileType.Soil   => new Color(0.42f, 0.30f, 0.18f),
		TileType.Stone  => hp >= 2
			? new Color(0.42f, 0.42f, 0.48f)
			: new Color(0.58f, 0.58f, 0.64f),
		TileType.Empty  => new Color(0.10f, 0.08f, 0.07f),
		_               => Colors.Magenta,
	};

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
}
