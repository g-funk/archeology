using Godot;
using System;
using System.Collections.Generic;

namespace Arkeology.Production.Client;

// Subtle feedback for failed interactions. Currently: when the player's dig is
// rejected by the step constraint, briefly flash the *preventing* neighbors
// (the tiles that are too shallow) in red. Lives as a child of `Grid` so its
// _Draw is composited on top of floors, walls, and pings.
public partial class HintsSystem : Node2D
{
	[Export] public NodePath GridPath { get; set; } = new("..");

	[Export] public float FlashPeakBrightness { get; set; } = 0.6f;
	[Export] public float FadeMs { get; set; } = 400f;
	[Export] public Color FlashColor { get; set; } = new Color(1f, 0.2f, 0.2f);

	private Grid? _grid;
	private readonly List<Flash> _flashes = new();

	private struct Flash
	{
		public int X;
		public int Y;
		public float ElapsedMs;
	}

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"HintsSystem: could not resolve Grid at '{GridPath}'.");
			return;
		}
		_grid.DigBlocked += OnDigBlocked;
	}

	private void OnDigBlocked(int x, int y)
	{
		if (_grid == null) return;

		int d = _grid.GetDepth(x, y);

		if (d >= _grid.LayerCount || _grid.HasFragmentAt(x, y, d))
		{
			_flashes.Add(new Flash { X = x, Y = y, ElapsedMs = 0f });
			QueueRedraw();
			return;
		}

		// Step constraint: flash every hex neighbor that is shallower than this tile.
		bool anyAdded = false;
		Span<Vector2I> neighbors = stackalloc Vector2I[6];
		HexMetrics.GetNeighbors(x, y, neighbors);
		foreach (var n in neighbors)
		{
			if (!_grid.InBounds(n.X, n.Y)) continue;
			if (_grid.GetDepth(n.X, n.Y) >= d) continue;
			_flashes.Add(new Flash { X = n.X, Y = n.Y, ElapsedMs = 0f });
			anyAdded = true;
		}
		if (!anyAdded)
			_flashes.Add(new Flash { X = x, Y = y, ElapsedMs = 0f });
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_flashes.Count == 0) return;
		float dt = (float)(delta * 1000.0);
		for (int i = _flashes.Count - 1; i >= 0; i--)
		{
			var f = _flashes[i];
			f.ElapsedMs += dt;
			if (f.ElapsedMs >= FadeMs)
				_flashes.RemoveAt(i);
			else
				_flashes[i] = f;
		}
		QueueRedraw();
	}

	private readonly Vector2[] _hexVerts = new Vector2[6];

	public override void _Draw()
	{
		if (_grid == null) return;
		foreach (var f in _flashes)
		{
			float t = f.ElapsedMs / FadeMs;
			float alpha = FlashPeakBrightness * Math.Max(0f, 1f - t);
			var color = new Color(FlashColor.R, FlashColor.G, FlashColor.B, alpha);
			var center = HexMetrics.CellCenter(f.X, f.Y, _grid.TileSize);
			HexMetrics.HexVerticesAt(center, _grid.TileSize, _hexVerts);
			DrawColoredPolygon(_hexVerts, color);
		}
	}
}
