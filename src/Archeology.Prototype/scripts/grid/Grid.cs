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

	private TileType[,] _types = new TileType[0, 0];
	private int[,] _hp = new int[0, 0];

	public int FragmentsCollected { get; private set; }

	public override void _Ready()
	{
		Generate();
	}

	public void Generate()
	{
		_types = new TileType[Width, Height];
		_hp = new int[Width, Height];

		var rng = new Random(Seed);
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				double r = rng.NextDouble();
				if (r < 0.06)
				{
					_types[x, y] = TileType.Fragment;
					_hp[x, y] = 0;
				}
				else if (r < 0.32)
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

		QueueRedraw();
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

	// Damage a single tile. Soil clears in 1 hit, stone in 2, fragments are blockers.
	public void Dig(Vector2I cell)
	{
		if (!InBounds(cell.X, cell.Y)) return;

		var t = _types[cell.X, cell.Y];
		if (t == TileType.Empty || t == TileType.Fragment) return;

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
		if (_types[cell.X, cell.Y] != TileType.Fragment) return false;
		if (!IsExposed(cell.X, cell.Y)) return false;

		_types[cell.X, cell.Y] = TileType.Empty;
		FragmentsCollected++;
		EmitSignal(SignalName.FragmentsChanged, FragmentsCollected);
		QueueRedraw();
		return true;
	}

	private bool IsExposed(int x, int y)
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
		switch (_types[x, y])
		{
			case TileType.Empty:
				// near-black brown — a dug-out hole
				return new Color(0.10f, 0.08f, 0.07f);
			case TileType.Soil:
				// warm earthy brown — easy-to-dig dirt
				return new Color(0.42f, 0.30f, 0.18f);
			case TileType.Stone:
				return _hp[x, y] >= 2
					// dark slate gray — undamaged stone
					? new Color(0.42f, 0.42f, 0.48f)
					// light slate gray — cracked/damaged stone (1 hp left)
					: new Color(0.58f, 0.58f, 0.64f);
			case TileType.Fragment:
				return IsExposed(x, y)
					// bright gold — fragment with a cleared neighbor, ready to collect
					? new Color(1.00f, 0.82f, 0.32f)
					// matches soil — buried fragments are hidden until a neighbor is dug
					: new Color(0.42f, 0.30f, 0.18f);
			default:
				// magenta — error/unhandled case sentinel
				return Colors.Magenta;
		}
	}
}
