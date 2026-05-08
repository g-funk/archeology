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

	[Signal] public delegate void FragmentsChangedEventHandler(int count);

	// _types is the cover at each cell: Soil, Stone, or Empty (cleared).
	// _fragmentAt is an overlay: a fragment occupies these cells (multi-tile shapes).
	// A fragment is collectable only when *all* of its cells have Empty cover.
	private TileType[,] _types = new TileType[0, 0];
	private int[,] _hp = new int[0, 0];
	private Fragment?[,] _fragmentAt = new Fragment?[0, 0];
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
		_types = new TileType[Width, Height];
		_hp = new int[Width, Height];
		_fragmentAt = new Fragment?[Width, Height];
		_fragments = new List<Fragment>();
		_collectedFragments = new List<Fragment>();
		FragmentsCollected = 0;

		var rng = new Random(Seed);

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				if (rng.NextDouble() < 0.32)
				{
					_types[x, y] = TileType.Stone;
					_hp[x, y] = 2;
				}
				else
				{
					_types[x, y] = TileType.Soil;
					_hp[x, y] = 1;
				}
			}
		}

		SpawnFragments(rng, FragmentTarget);

		EmitSignal(SignalName.FragmentsChanged, FragmentsCollected);
		QueueRedraw();
	}

	private void SpawnFragments(Random rng, int target)
	{
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

			var abs = new Vector2I[template.Count];
			bool fits = true;
			for (int i = 0; i < template.Count; i++)
			{
				int cx = ax + template[i].X;
				int cy = ay + template[i].Y;
				if (!InBounds(cx, cy) || _fragmentAt[cx, cy] != null)
				{
					fits = false;
					break;
				}
				abs[i] = new Vector2I(cx, cy);
			}
			if (!fits) continue;

			var frag = new Fragment(_fragments.Count, shape, abs);
			_fragments.Add(frag);
			foreach (var c in abs) _fragmentAt[c.X, c.Y] = frag;
		}
	}

	public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

	public TileType GetTile(int x, int y) => _types[x, y];

	public Vector2I WorldToCell(Vector2 worldPosition)
	{
		var local = ToLocal(worldPosition);
		return new Vector2I(
			(int)Math.Floor(local.X / TileSize),
			(int)Math.Floor(local.Y / TileSize));
	}

	// Single entry point for a click on a cell.
	// Collects a fully-exposed fragment if one is there; otherwise digs the cover.
	public void HandleClick(Vector2I cell)
	{
		if (TryCollectFragment(cell)) return;
		Dig(cell);
	}

	// Damage the cover on a single tile. Soil clears in 1 hit, stone in 2.
	// No-op on already-cleared tiles.
	public void Dig(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return;
		if (_types[cell.X, cell.Y] == TileType.Empty) return;

		_hp[cell.X, cell.Y]--;
		if (_hp[cell.X, cell.Y] <= 0)
		{
			_types[cell.X, cell.Y] = TileType.Empty;
		}
		QueueRedraw();
	}

	public bool TryCollectFragment(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return false;
		var frag = _fragmentAt[cell.X, cell.Y];
		if (frag == null) return false;
		if (!IsFragmentFullyExposed(frag)) return false;

		foreach (var c in frag.Cells) _fragmentAt[c.X, c.Y] = null;
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
			if (_types[c.X, c.Y] != TileType.Empty) return false;
		}
		return true;
	}

	// True if any 4-neighbor's cover has been cleared.
	// Drives the hint color on a buried fragment cell.
	private bool HasClearedNeighbor(int x, int y)
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
			if (_types[nx, ny] == TileType.Empty) return true;
		}
		return false;
	}

	public override void _Draw()
	{
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				var rect = new Rect2(
					x * TileSize,
					y * TileSize,
					TileSize,
					TileSize);
				DrawRect(rect, ColorFor(x, y));
			}
		}
	}

	private Color ColorFor(int x, int y)
	{
		var frag = _fragmentAt[x, y];
		bool hasFrag = frag != null;

		switch (_types[x, y])
		{
			case TileType.Empty:
				if (hasFrag)
				{
					return IsFragmentFullyExposed(frag!)
						// bright pale gold — whole shape is exposed, click any cell to collect
						? new Color(1.00f, 0.92f, 0.55f)
						// standard gold — this cell is exposed, but other cells of the same fragment still covered
						: new Color(1.00f, 0.82f, 0.32f);
				}
				// near-black brown — empty hole, nothing here
				return new Color(0.10f, 0.08f, 0.07f);

			case TileType.Soil:
				return hasFrag && HasClearedNeighbor(x, y)
					// muted ochre — a neighbor was cleared; a fragment cell hides under this soil
					? new Color(0.60f, 0.50f, 0.28f)
					// warm earthy brown — plain soil (also camouflages buried fragment cells)
					: new Color(0.42f, 0.30f, 0.18f);

			case TileType.Stone:
				if (hasFrag && HasClearedNeighbor(x, y))
					// muted ochre — hint also surfaces through stone cover
					return new Color(0.60f, 0.50f, 0.28f);
				return _hp[x, y] >= 2
					// dark slate gray — undamaged stone
					? new Color(0.42f, 0.42f, 0.48f)
					// light slate gray — cracked/damaged stone (1 hp left)
					: new Color(0.58f, 0.58f, 0.64f);

			default:
				// magenta — error/unhandled case sentinel
				return Colors.Magenta;
		}
	}
}
