using Godot;
using System;
using System.Collections.Generic;

namespace Arkeology.Simple.Prototype;

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

		// Bedrock or fragment-at-current-depth: no preventing neighbour exists,
		// so flash the attempted tile itself — that's "we tried this tile and
		// couldn't dig it".
		if (d >= _grid.LayerCount || _grid.HasFragmentAt(x, y, d))
		{
			_flashes.Add(new Flash { X = x, Y = y, ElapsedMs = 0f });
			QueueRedraw();
			return;
		}

		// Step constraint: flash every 4-neighbour that's shallower than this tile.
		bool anyAdded = false;
		ReadOnlySpan<(int dx, int dy)> n = stackalloc (int, int)[]
		{
			(1, 0), (-1, 0), (0, 1), (0, -1)
		};
		foreach (var (dx, dy) in n)
		{
			int nx = x + dx;
			int ny = y + dy;
			if (!_grid.InBounds(nx, ny)) continue;
			if (_grid.GetDepth(nx, ny) >= d) continue; // not a preventer
			_flashes.Add(new Flash { X = nx, Y = ny, ElapsedMs = 0f });
			anyAdded = true;
		}
		// Fallback — shouldn't happen for step-blocked digs, but if nothing
		// flashed, at least flash the attempted tile so the failure is visible.
		if (!anyAdded)
		{
			_flashes.Add(new Flash { X = x, Y = y, ElapsedMs = 0f });
		}
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

	public override void _Draw()
	{
		if (_grid == null) return;
		int tile = _grid.TileSize;
		foreach (var f in _flashes)
		{
			float t = f.ElapsedMs / FadeMs;
			float alpha = FlashPeakBrightness * Math.Max(0f, 1f - t);
			var color = new Color(FlashColor.R, FlashColor.G, FlashColor.B, alpha);
			var rect = new Rect2(f.X * tile, f.Y * tile, tile, tile);
			DrawRect(rect, color);
		}
	}
}
