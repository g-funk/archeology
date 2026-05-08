using Godot;
using System;

namespace Archeology.Prototype;

public partial class Grid : Node2D
{
	[Export] public int Width { get; set; } = 28;
	[Export] public int Height { get; set; } = 16;
	[Export] public int TileSize { get; set; } = 36;
	[Export] public int Seed { get; set; } = 1337;

	[Signal] public delegate void FragmentsChangedEventHandler(int count);

	// _types is the *cover* at each cell: Soil, Stone, or Empty (cleared).
	// _hasFragment is an overlay: a fragment hides under the cover.
	// A fragment is "exposed" when its own cover is Empty.
	private TileType[,] _types = new TileType[0, 0];
	private int[,] _hp = new int[0, 0];
	private bool[,] _hasFragment = new bool[0, 0];

	public int FragmentsCollected { get; private set; }

	public override void _Ready()
	{
		Generate();
	}

	public void Generate()
	{
		_types = new TileType[Width, Height];
		_hp = new int[Width, Height];
		_hasFragment = new bool[Width, Height];

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

				if (rng.NextDouble() < 0.06)
				{
					_hasFragment[x, y] = true;
				}
			}
		}

		QueueRedraw();
	}

	public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

	public TileType GetTile(int x, int y) => _types[x, y];

	public bool HasExposedFragment(int x, int y) =>
		InBounds(x, y) && _hasFragment[x, y] && _types[x, y] == TileType.Empty;

	public Vector2I WorldToCell(Vector2 worldPosition)
	{
		var local = ToLocal(worldPosition);
		return new Vector2I(
			(int)Math.Floor(local.X / TileSize),
			(int)Math.Floor(local.Y / TileSize));
	}

	// Single entry point for a click on a cell.
	// Collects an exposed fragment if one is there; otherwise digs the cover.
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
		if (!HasExposedFragment(cell.X, cell.Y)) return false;

		_hasFragment[cell.X, cell.Y] = false;
		FragmentsCollected++;
		EmitSignal(SignalName.FragmentsChanged, FragmentsCollected);
		QueueRedraw();
		return true;
	}

	// True if any 4-neighbor's cover has been cleared.
	// Triggers the hint color on a buried fragment tile.
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
					TileSize - 1,
					TileSize - 1);
				DrawRect(rect, ColorFor(x, y));
			}
		}
	}

	private Color ColorFor(int x, int y)
	{
		bool hasFrag = _hasFragment[x, y];

		switch (_types[x, y])
		{
			case TileType.Empty:
				return hasFrag
					// bright gold — cover cleared, fragment exposed, ready to collect
					? new Color(1.00f, 0.82f, 0.32f)
					// near-black brown — empty hole, nothing here
					: new Color(0.10f, 0.08f, 0.07f);

			case TileType.Soil:
				return hasFrag && HasClearedNeighbor(x, y)
					// muted ochre — a neighbor was cleared; a fragment hides under this soil
					? new Color(0.60f, 0.50f, 0.28f)
					// warm earthy brown — plain soil (also camouflages buried fragments)
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
