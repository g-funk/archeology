using Godot;
using System;
using System.Collections.Generic;

namespace Archeology.Prototype;

// Surfaces hints about nearby fragments after each dig. Listens to Grid.Dug;
// when fired, finds the closest fragment cell within radius (3D distance — depth
// counts the same as horizontal distance) and flashes the floor at that cell.
// Occasionally fires a fake ping with no fragment beneath it.
//
// Lives as a child of `Grid` so its _Draw is composited on top of Grid's floors
// and walls, and local coordinates already match grid space.
public partial class PingSystem : Node2D
{
	[Export] public NodePath GridPath { get; set; } = new("..");

	// Maximum 3D distance (in tiles) at which a fragment can trigger a ping.
	[Export] public float PingRadius { get; set; } = 8f;
	// Peak alpha of the white overlay at distance 0; scales linearly down to 0 at the radius.
	[Export] public float PingPeakBrightness { get; set; } = 0.5f;
	// Time from peak brightness to fully faded.
	[Export] public float FadeMs { get; set; } = 600f;
	// Probability per dig of producing a fake ping instead of a real one.
	[Export] public float FakePingChance { get; set; } = 0.05f;

	private Grid? _grid;
	private readonly List<Ping> _pings = new();
	private readonly Random _rng = new();
	// For each fragment that has been pinged at least once, the specific cell
	// that was flashed. After the first ping, only that same cell is eligible
	// to ping again — "any other part cannot be pinged". Reference-keyed so
	// `Grid.Generate` naturally invalidates old entries.
	private readonly Dictionary<Fragment, Vector2I> _pingedCellByFragment = new();

	private struct Ping
	{
		public int X;
		public int Y;
		public float PeakBrightness;
		public float ElapsedMs;
	}

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"PingSystem: could not resolve Grid at '{GridPath}'.");
			return;
		}
		_grid.Dug += OnDug;
	}

	private void OnDug(int digX, int digY, int digDepth)
	{
		if (_grid == null) return;

		if (_rng.NextDouble() < FakePingChance)
		{
			FireFakePing(digX, digY);
			return;
		}

		if (TryFindClosestEligibleCell(digX, digY, digDepth, out Fragment? frag, out int fx, out int fy, out float distance))
		{
			float falloff = 1f - (distance / PingRadius);
			float brightness = PingPeakBrightness * falloff;
			if (brightness > 0f)
			{
				_pings.Add(new Ping { X = fx, Y = fy, PeakBrightness = brightness, ElapsedMs = 0f });
				// Lock this fragment to this cell on the *first* ping only;
				// subsequent searches restrict it to the same cell.
				if (!_pingedCellByFragment.ContainsKey(frag!))
				{
					_pingedCellByFragment[frag!] = new Vector2I(fx, fy);
				}
				QueueRedraw();
			}
		}
	}

	// A fragment is eligible to ping at all only if none of its cells is
	// already exposed. The "pinged once" rule constrains *which* cells can be
	// considered, not whether the fragment itself participates.
	private bool IsFragmentEligibleForPing(Fragment frag)
	{
		foreach (var c in frag.Cells)
		{
			if (_grid!.GetDepth(c.X, c.Y) == frag.Depth) return false; // any cell exposed
		}
		return true;
	}

	private bool TryFindClosestEligibleCell(int digX, int digY, int digDepth, out Fragment? closest, out int fx, out int fy, out float bestDistance)
	{
		closest = null;
		fx = fy = 0;
		bestDistance = float.MaxValue;
		if (_grid == null) return false;

		foreach (var frag in _grid.Fragments)
		{
			if (!IsFragmentEligibleForPing(frag)) continue;

			// If this fragment was pinged before, only its locked cell is in play.
			bool hasLocked = _pingedCellByFragment.TryGetValue(frag, out var lockedCell);
			foreach (var c in frag.Cells)
			{
				if (hasLocked && (c.X != lockedCell.X || c.Y != lockedCell.Y)) continue;

				float dx = c.X - digX;
				float dy = c.Y - digY;
				float dz = frag.Depth - digDepth;
				float d = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
				if (d < bestDistance)
				{
					bestDistance = d;
					fx = c.X;
					fy = c.Y;
					closest = frag;
				}
			}
		}

		return closest != null && bestDistance < PingRadius;
	}

	private void FireFakePing(int digX, int digY)
	{
		if (_grid == null) return;
		int r = Math.Max(1, (int)PingRadius);
		int x = digX + _rng.Next(-r, r + 1);
		int y = digY + _rng.Next(-r, r + 1);
		if (!_grid.InBounds(x, y)) return;

		float brightness = (float)_rng.NextDouble() * PingPeakBrightness;
		_pings.Add(new Ping { X = x, Y = y, PeakBrightness = brightness, ElapsedMs = 0f });
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_pings.Count == 0) return;
		float dt = (float)(delta * 1000.0);
		for (int i = _pings.Count - 1; i >= 0; i--)
		{
			var p = _pings[i];
			p.ElapsedMs += dt;
			if (p.ElapsedMs >= FadeMs)
				_pings.RemoveAt(i);
			else
				_pings[i] = p;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_grid == null) return;
		int tile = _grid.TileSize;
		foreach (var p in _pings)
		{
			float t = p.ElapsedMs / FadeMs;
			float alpha = p.PeakBrightness * Math.Max(0f, 1f - t);
			var rect = new Rect2(p.X * tile, p.Y * tile, tile, tile);
			DrawRect(rect, new Color(1f, 1f, 0.95f, alpha));
		}
	}
}
