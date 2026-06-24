using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Arkeology.Production.Client;

public partial class Grid : Node2D
{
	[Export] public int Width { get; set; } = 28;
	[Export] public int Height { get; set; } = 16;
	// Hex circumradius (center-to-vertex) in pixels.
	[Export] public int TileSize { get; set; } = 22;
	[Export] public int Seed { get; set; } = 1337;
	[Export] public int MapIndex { get; set; } = 0;
	[Export] public string PredefinedTokensPath { get; set; } = "res://data/json/predefined_tokens.json";
	[Export] public string MapsConfigPath { get; set; } = "res://data/bin/maps.bin";
	[Export] public string ItemsConfigPath { get; set; } = "res://data/bin/items.bin";
	[Export] public int LayerCount { get; set; } = 4;
	[Export] public int MaxCollapse { get; set; } = 2;
	[Export] public float CollapseChance { get; set; } = 0.15f;
	[Export] public int MinScrapTiles { get; set; } = 4;
	[Export] public int MaxScrapTiles { get; set; } = 12;

	[Signal] public delegate void FragmentsChangedEventHandler(int count);
	[Signal] public delegate void DugEventHandler(int x, int y, int depth);
	[Signal] public delegate void DigBlockedEventHandler(int x, int y);
	[Signal] public delegate void ClickedEventHandler(int x, int y);
	[Signal] public delegate void ScanTriggeredEventHandler(int x, int y, int depth);
	[Signal] public delegate void MapAdvancedEventHandler();

	private TileType[,,] _layerTypes = new TileType[0, 0, 0];
	private int[,,] _layerHp = new int[0, 0, 0];
	private int[,] _depth = new int[0, 0];
	private Fragment?[,,] _fragmentAt = new Fragment?[0, 0, 0];

	private List<Fragment> _fragments = new();
	private List<Fragment> _collectedFragments = new();
	private Random _rng = new();
	private IReadOnlyList<MapConfig>? _maps;

	// Reused per _Draw call to avoid per-tile allocation.
	private readonly Vector2[] _hexVerts = new Vector2[6];
	private readonly Vector2[] _wallQuad = new Vector2[4];

	public int FragmentsCollected { get; private set; }
	public IReadOnlyList<Fragment> CollectedFragments => _collectedFragments;
	public IReadOnlyList<Fragment> Fragments => _fragments;

	public override void _Ready()
	{
		Generate();
	}

	public void Generate()
	{
		StringTable.Configure(ProjectSettings.GlobalizePath(PredefinedTokensPath));
		var map = LoadMapConfig();

		if (map != null)
		{
			Width = map.Width;
			Height = map.Height;
			LayerCount = map.Layers.Count;
		}

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
					if (map != null && d < map.Layers.Count && !map.Layers[d].IsRandom && map.Layers[d].Tiles != null)
					{
						var tileType = map.Layers[d].Tiles![y * Width + x];
						_layerTypes[x, y, d] = tileType;
						_layerHp[x, y, d] = HpForType(tileType);
					}
					else
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
		}

		if (map != null)
		{
			PlaceConfigShapes(map);
			SpawnScraps(map.ScrapCount);
		}

		EmitSignal(SignalName.FragmentsChanged, FragmentsCollected);
		QueueRedraw();
	}

	private MapConfig? LoadMapConfig()
	{
		if (_maps == null)
		{
			using var mapsFile = Godot.FileAccess.Open(MapsConfigPath, Godot.FileAccess.ModeFlags.Read);
			if (mapsFile == null)
			{
				GD.PrintErr($"[Grid] maps config not found: {MapsConfigPath}");
				return null;
			}
			var mapsBytes = mapsFile.GetBuffer((long)mapsFile.GetLength());
			mapsFile.Close();

			using var mapsStream = new MemoryStream(mapsBytes);
			_maps = new MapsConfigReader().Read(mapsStream);
		}

		if (_maps.Count == 0)
		{
			GD.PrintErr("[Grid] maps config is empty");
			return null;
		}

		int idx = Math.Clamp(MapIndex, 0, _maps.Count - 1);
		return _maps[idx];
	}

	private void AdvanceToNextMap()
	{
		if (_maps == null || MapIndex + 1 >= _maps.Count) return;
		MapIndex++;
		Generate();
		EmitSignal(SignalName.MapAdvanced);
	}

	private IReadOnlyList<ItemConfig>? LoadItems()
	{
		using var itemsFile = Godot.FileAccess.Open(ItemsConfigPath, Godot.FileAccess.ModeFlags.Read);
		if (itemsFile == null)
		{
			GD.PrintErr($"[Grid] items config not found: {ItemsConfigPath}");
			return null;
		}
		var bytes = itemsFile.GetBuffer((long)itemsFile.GetLength());
		itemsFile.Close();

		using var stream = new MemoryStream(bytes);
		return new ItemsConfigReader().Read(stream);
	}

	private void PlaceConfigShapes(MapConfig map)
	{
		var items = LoadItems();
		if (items == null) return;

		var itemById = new Dictionary<int, ItemConfig>(items.Count);
		foreach (var item in items)
			itemById[item.Id] = item;

		foreach (var shape in map.Shapes)
		{
			if (!itemById.TryGetValue(shape.ItemId, out var itemCfg)) continue;
			if (shape.Layer < 0 || shape.Layer >= LayerCount) continue;

			var cells = new List<Vector2I>();
			for (int sy = 0; sy < itemCfg.ShapeHeight; sy++)
			{
				for (int sx = 0; sx < itemCfg.ShapeWidth; sx++)
				{
					if (!itemCfg.IsShapeOccupied(sx, sy)) continue;
					int gx = shape.X + sx;
					int gy = shape.Y + sy;
					if (!InBounds(gx, gy)) continue;
					if (_fragmentAt[gx, gy, shape.Layer] != null) continue;
					cells.Add(new Vector2I(gx, gy));
				}
			}

			if (cells.Count == 0) continue;

			var frag = new Fragment(shape.ItemId, FragmentShape.SquareTwo, shape.Layer, cells);
			_fragments.Add(frag);
			foreach (var c in cells)
				_fragmentAt[c.X, c.Y, shape.Layer] = frag;
		}
	}

	private void SpawnScraps(int count)
	{
		if (count <= 0 || LayerCount <= 1) return;

		const int maxAttempts = 500;
		int attempts = 0;
		int spawned = 0;
		while (spawned < count && attempts < maxAttempts)
		{
			attempts++;
			int tileCount = MinScrapTiles + _rng.Next(Math.Max(1, MaxScrapTiles - MinScrapTiles + 1));
			var relCells = GenerateRandomShape(tileCount, _rng);

			int shapeW = 1, shapeH = 1;
			foreach (var c in relCells)
			{
				if (c.X + 1 > shapeW) shapeW = c.X + 1;
				if (c.Y + 1 > shapeH) shapeH = c.Y + 1;
			}
			if (shapeW > Width || shapeH > Height) continue;

			int ax = _rng.Next(Width - shapeW + 1);
			int ay = _rng.Next(Height - shapeH + 1);
			int depth = 1 + _rng.Next(LayerCount - 1);

			var cells = new Vector2I[relCells.Length];
			bool fits = true;
			for (int i = 0; i < relCells.Length; i++)
			{
				int cx = ax + relCells[i].X;
				int cy = ay + relCells[i].Y;
				if (!InBounds(cx, cy) || _fragmentAt[cx, cy, depth] != null) { fits = false; break; }
				cells[i] = new Vector2I(cx, cy);
			}
			if (!fits) continue;

			var frag = new Fragment(_fragments.Count, FragmentShape.SquareTwo, depth, cells);
			_fragments.Add(frag);
			foreach (var c in cells)
				_fragmentAt[c.X, c.Y, depth] = frag;
			spawned++;
		}
	}

	private static Vector2I[] GenerateRandomShape(int tileCount, Random rng)
	{
		var cells = new HashSet<Vector2I>();
		var perimeter = new List<Vector2I>();
		cells.Add(Vector2I.Zero);
		AddHexPerimeter(Vector2I.Zero, cells, perimeter);

		while (cells.Count < tileCount && perimeter.Count > 0)
		{
			int idx = rng.Next(perimeter.Count);
			var next = perimeter[idx];
			perimeter.RemoveAt(idx);
			if (!cells.Add(next)) continue;
			AddHexPerimeter(next, cells, perimeter);
		}

		int minX = int.MaxValue, minY = int.MaxValue;
		foreach (var c in cells) { if (c.X < minX) minX = c.X; if (c.Y < minY) minY = c.Y; }
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
			if (!cells.Contains(n)) perimeter.Add(n);
	}

	private static int HpForType(TileType type) => type switch
	{
		TileType.Stone => 2,
		TileType.Soil  => 1,
		_              => 1,
	};

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
		if (_fragments.Count == 0)
			CallDeferred(MethodName.AdvanceToNextMap);
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
		// Grass background behind the entire grid (fills the surroundings).
		DrawRect(new Rect2(-100000f, -100000f, 200000f, 200000f), GameColors.Sand);

		// Floors first; outlines and walls on top.
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				var center = HexMetrics.CellCenter(x, y, TileSize);
				HexMetrics.HexVerticesAt(center, TileSize, _hexVerts);
				DrawColoredPolygon(_hexVerts, FloorColorFor(x, y));
			}
		}
		DrawHexOutlines();
		DrawHexWalls();
	}

	private void DrawHexOutlines()
	{
		var brown = GameColors.HexOutline;
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
					// Skip edges that already have a shadow/highlight wall.
					if (InBounds(n.X, n.Y) && _depth[n.X, n.Y] < d) continue;
					// Deduplicate same-depth interior edges: draw only from the earlier tile.
					if (InBounds(n.X, n.Y) && _depth[n.X, n.Y] == d)
					{
						if (n.Y < y || (n.Y == y && n.X < x)) continue;
					}
					var (va, vb) = HexMetrics.EdgeForNeighbor(i);
					DrawLine(_hexVerts[va], _hexVerts[vb], brown, 1f, true);
				}
			}
		}
	}

	private void DrawHexWalls()
	{
		int unit = Math.Max(2, TileSize / 10);
		var shadow    = GameColors.WallShadow;
		var highlight = GameColors.WallHighlight;

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
					if (d <= nd) continue;

					float wallThick = unit * (d - nd);
					var (va, vb) = HexMetrics.EdgeForNeighbor(i);
					var color = HexMetrics.IsHighlightEdge(i) ? highlight : shadow;

					// Draw a filled quad rather than DrawLine to avoid rounded capsule
					// ends and AA artifacts. The quad runs along the hex edge and steps
					// inward (toward the tile center) by wallThick pixels.
					var edgeMid = (_hexVerts[va] + _hexVerts[vb]) * 0.5f;
					var inward = (center - edgeMid).Normalized() * wallThick;
					_wallQuad[0] = _hexVerts[va];
					_wallQuad[1] = _hexVerts[vb];
					_wallQuad[2] = _hexVerts[vb] + inward;
					_wallQuad[3] = _hexVerts[va] + inward;
					DrawColoredPolygon(_wallQuad, color);
				}
			}
		}
	}

	private Color FloorColorFor(int x, int y)
	{
		int d = _depth[x, y];

		if (d >= LayerCount) return GameColors.DepthFloor;

		var frag = _fragmentAt[x, y, d];
		if (frag != null)
		{
			return IsFragmentFullyExposed(frag)
				? Darken(GameColors.FragmentExposed, d)
				: Darken(GameColors.FragmentBuried, d);
		}

		for (int probe = d + 1; probe < LayerCount; probe++)
		{
			var deeperFrag = _fragmentAt[x, y, probe];
			if (deeperFrag == null) continue;
			if (AnyNeighborDeeperThan(x, y, probe))
				return Darken(GameColors.FragmentHint, d);
			break;
		}

		return Darken(MaterialColor(_layerTypes[x, y, d], _layerHp[x, y, d]), d);
	}

	private static Color MaterialColor(TileType type, int hp) => type switch
	{
		TileType.Soil   => GameColors.Grass,
		TileType.Stone  => hp >= 2 ? GameColors.StoneFull : GameColors.StoneDamaged,
		TileType.Empty  => GameColors.Void,
		_               => Godot.Colors.Magenta,
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
