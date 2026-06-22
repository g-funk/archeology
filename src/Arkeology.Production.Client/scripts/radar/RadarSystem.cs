using Godot;
using System.Collections.Generic;

namespace Arkeology.Production.Client;

// Sonar-like directional scan. Triggered by `Grid.ScanTriggered` (which fires
// when the player presses S or when the character arrives at a long-pressed
// tile). An expanding ring grows out from the scan origin and fades by the
// time it reaches `ScanRadius`. For each eligible fragment within that
// radius, a brighter wedge highlights the 2D direction toward the fragment's
// closest cell.
//
// Child of `Grid` so local coords match grid space and the draw is composited
// on top of floors and walls.
public partial class RadarSystem : Node2D
{
	[Export] public NodePath GridPath { get; set; } = new("..");

	// Max scan range in tiles. Distance to fragments is 3D (depth counts the
	// same as horizontal distance) — matches ping.
	[Export] public float ScanRadius { get; set; } = 8f;
	// Time from pulse start to full fade. Radius reaches ScanRadius at the same
	// moment alpha reaches 0.
	[Export] public float FadeMs { get; set; } = 800f;
	// Wedge angular width as a fraction of the full circle. 0.125 = 1/8 = 45°.
	[Export] public float WedgeFraction { get; set; } = 0.125f;
	// Peak alpha for the base ring and the highlighted wedges.
	[Export] public float RingBrightness { get; set; } = 0.35f;
	[Export] public float WedgeBrightness { get; set; } = 0.7f;
	// Pixel thickness of the ring and wedge strokes.
	[Export] public float RingThickness { get; set; } = 2f;
	[Export] public float WedgeThickness { get; set; } = 5f;

	private Grid? _grid;
	private readonly List<Pulse> _pulses = new();

	private struct Pulse
	{
		public int CenterX;
		public int CenterY;
		public float[] Angles; // radians, one per detected fragment
		public float ElapsedMs;
	}

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"RadarSystem: could not resolve Grid at '{GridPath}'.");
			return;
		}
		_grid.ScanTriggered += OnScanTriggered;
	}

	private void OnScanTriggered(int digX, int digY, int digDepth)
	{
		if (_grid == null) return;

		var angles = new List<float>();
		foreach (var frag in _grid.Fragments)
		{
			if (!IsFragmentEligibleForRadar(frag)) continue;

			// Closest cell of this fragment to the dig, 3D distance.
			float bestDist = float.MaxValue;
			int closestX = 0, closestY = 0;
			foreach (var c in frag.Cells)
			{
				float dx = c.X - digX;
				float dy = c.Y - digY;
				float dz = frag.Depth - digDepth;
				float d = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
				if (d < bestDist)
				{
					bestDist = d;
					closestX = c.X;
					closestY = c.Y;
				}
			}
			if (bestDist >= ScanRadius) continue;

			// 2D direction to the closest cell. If it's directly under the dig
			// (same x and y), there's no horizontal direction — skip the wedge.
			float ax = closestX - digX;
			float ay = closestY - digY;
			if (ax == 0f && ay == 0f) continue;
			angles.Add(Mathf.Atan2(ay, ax));
		}

		_pulses.Add(new Pulse
		{
			CenterX = digX,
			CenterY = digY,
			Angles = angles.ToArray(),
			ElapsedMs = 0f,
		});
		QueueRedraw();
	}

	// Same eligibility as ping's "any cell exposed → ignore". The ping-only
	// "lock to first pinged cell" rule does NOT apply here — radar is its own beat.
	private bool IsFragmentEligibleForRadar(Fragment frag)
	{
		foreach (var c in frag.Cells)
		{
			if (_grid!.GetDepth(c.X, c.Y) == frag.Depth) return false;
		}
		return true;
	}

	public override void _Process(double delta)
	{
		if (_pulses.Count == 0) return;
		float dt = (float)(delta * 1000.0);
		for (int i = _pulses.Count - 1; i >= 0; i--)
		{
			var p = _pulses[i];
			p.ElapsedMs += dt;
			if (p.ElapsedMs >= FadeMs)
				_pulses.RemoveAt(i);
			else
				_pulses[i] = p;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_grid == null) return;
		int tile = _grid.TileSize;
		float wedgeWidth = Mathf.Pi * 2f * WedgeFraction;
		float wedgeHalf = wedgeWidth * 0.5f;

		foreach (var p in _pulses)
		{
			float t = p.ElapsedMs / FadeMs;
			float radiusPx = t * ScanRadius * tile;
			float alpha = Mathf.Max(0f, 1f - t);
			var center = HexMetrics.CellCenter(p.CenterX, p.CenterY, tile);

			// Base ring — subtle, dissipates as it expands.
			var ringColor = new Color(1f, 1f, 1f, alpha * RingBrightness);
			DrawArc(center, radiusPx, 0f, Mathf.Pi * 2f, 48, ringColor, RingThickness);

			// Directional wedges — one per detected fragment.
			var wedgeColor = new Color(1f, 0.9f, 0.5f, alpha * WedgeBrightness);
			foreach (var angle in p.Angles)
			{
				DrawArc(center, radiusPx, angle - wedgeHalf, angle + wedgeHalf, 12, wedgeColor, WedgeThickness);
			}
		}
	}
}
