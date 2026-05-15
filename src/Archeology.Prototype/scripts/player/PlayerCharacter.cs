using Godot;
using System.Collections.Generic;

namespace Archeology.Prototype;

// The archaeologist — a simple stick-figure that walks toward whatever tile
// the player clicks. Animates walking (vertical bob + alternating leg lift)
// and digging (brief crouch + arms swing down) — see _Draw.
//
// Child of `Grid` so its `Position` is in grid-local coordinates: drawing at
// local (0, 0) puts the figure exactly at its current `Position`.
public partial class PlayerCharacter : Node2D
{
	[Export] public NodePath GridPath { get; set; } = new("..");

	// Movement speed in tiles per second. Quick but visibly non-instant.
	[Export] public float SpeedTilesPerSecond { get; set; } = 10f;
	// Body / outline colour. Light cream so the figure reads against earthy floors.
	[Export] public Color BodyColor { get; set; } = new Color(0.95f, 0.92f, 0.85f);
	// Duration of one dig pose, in milliseconds. The pose ramps in and back out.
	[Export] public float DigAnimationMs { get; set; } = 300f;

	private Grid? _grid;
	private Vector2 _targetPosition;
	// Set by RequestScanAt; flips false once a scan fires on arrival.
	private bool _scanPendingOnArrival;
	// Advances by π per tile travelled — so one half-cycle (one leg lift) per tile.
	private float _walkPhase;
	// Elapsed time inside the current dig animation; <0 means idle.
	private float _digElapsedMs = -1f;
	// Queue of tiles to dig from `RequestDigAround`; one tile is consumed
	// every `DigAnimationMs` so each tile gets its own animation.
	private readonly List<Vector2I> _digQueue = new();
	private float _digQueueTimer;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"PlayerCharacter: could not resolve Grid at '{GridPath}'.");
			return;
		}

		// Start on the middle tile of the grid.
		var startCell = new Vector2I(_grid.Width / 2, _grid.Height / 2);
		Position = CellCenter(startCell);
		_targetPosition = Position;

		_grid.Clicked += OnClicked;
		_grid.Dug += OnDug;
	}

	// True while an autodig sweep is in progress. Movement inputs are ignored
	// in that window — the player can't walk while digging.
	public bool IsAutoDigging => _digQueue.Count > 0;

	private void OnClicked(int x, int y)
	{
		if (IsAutoDigging) return;
		// A normal click moves the character but does not queue a scan; the
		// short-click path runs `Grid.HandleClick` for dig/collect side-effects.
		_targetPosition = CellCenter(new Vector2I(x, y));
	}

	private void OnDug(int x, int y, int depth)
	{
		// Every Dug emit re-arms (or extends) the dig pose.
		_digElapsedMs = 0f;
		QueueRedraw();
	}

	// Walk to this cell, then fire a scan from it once arrived. Used by the
	// long-press input gesture in ExcavationSystem.
	public void RequestScanAt(Vector2I cell)
	{
		if (IsAutoDigging) return;
		_targetPosition = CellCenter(cell);
		_scanPendingOnArrival = true;
	}

	// Fire a scan from the character's current tile immediately, no walking.
	// Used by the S-key trigger.
	public void RequestScanHere()
	{
		if (_grid == null) return;
		var cell = CurrentTile();
		_grid.TriggerScan(cell.X, cell.Y, _grid.GetDepth(cell.X, cell.Y));
	}

	// Try to collect a fragment at the character's current tile. No-op when
	// there's no fragment or it isn't fully exposed — `Grid.TryCollectFragment`
	// already validates both. Used by the C-key trigger.
	public void RequestCollect()
	{
		if (_grid == null) return;
		_grid.TryCollectFragment(CurrentTile());
	}

	// Step one tile in the given direction. Only retargets when the character
	// has finished its current step — otherwise mid-walk taps would double up
	// (the target advances as `CurrentTile()` flips at the half-way point).
	// Holding the key works fine: the next arrival re-fires from polling.
	public void RequestStep(int dx, int dy)
	{
		if (_grid == null) return;
		if (IsAutoDigging) return;
		if (Position != _targetPosition) return;
		var cur = CurrentTile();
		int nx = cur.X + dx;
		int ny = cur.Y + dy;
		if (!_grid.InBounds(nx, ny)) return;
		_targetPosition = CellCenter(new Vector2I(nx, ny));
	}

	// Queue the tile under the character and its 8 neighbors for sequential
	// digging — one tile per `DigAnimationMs` so the dig animation fires for
	// each. Order: center first, then the ring anti-clockwise from east
	// (E → NE → N → NW → W → SW → S → SE). Re-pressing D resets the queue
	// to the character's current tile.
	public void RequestDigAround()
	{
		if (_grid == null) return;
		_digQueue.Clear();
		var center = CurrentTile();
		if (_grid.InBounds(center.X, center.Y)) _digQueue.Add(center);

		// Anti-clockwise from east. In Godot screen coords +Y is down, so a
		// negative dy means "north" on the player's view.
		var ring = new Vector2I[]
		{
			new(1, 0),   // East
			new(1, -1),  // North-East
			new(0, -1),  // North
			new(-1, -1), // North-West
			new(-1, 0),  // West
			new(-1, 1),  // South-West
			new(0, 1),   // South
			new(1, 1),   // South-East
		};
		foreach (var dir in ring)
		{
			int nx = center.X + dir.X;
			int ny = center.Y + dir.Y;
			if (_grid.InBounds(nx, ny)) _digQueue.Add(new Vector2I(nx, ny));
		}

		// Set the timer at full so the first tile fires on the very next _Process.
		_digQueueTimer = DigAnimationMs;
	}

	private Vector2I CurrentTile()
	{
		if (_grid == null) return Vector2I.Zero;
		return new Vector2I(
			(int)Mathf.Floor(Position.X / _grid.TileSize),
			(int)Mathf.Floor(Position.Y / _grid.TileSize));
	}

	private Vector2 CellCenter(Vector2I cell)
	{
		if (_grid == null) return Vector2.Zero;
		return new Vector2((cell.X + 0.5f) * _grid.TileSize, (cell.Y + 0.5f) * _grid.TileSize);
	}

	public override void _Process(double delta)
	{
		if (_grid == null) return;

		bool wasMoving = Position != _targetPosition;
		if (wasMoving)
		{
			var oldPos = Position;
			float maxStep = SpeedTilesPerSecond * _grid.TileSize * (float)delta;
			Position = Position.MoveToward(_targetPosition, maxStep);
			float traveled = (Position - oldPos).Length();
			// π per tile travelled → one leg lift per tile, alternating legs.
			_walkPhase = Mathf.PosMod(_walkPhase + traveled / _grid.TileSize * Mathf.Pi, Mathf.Pi * 2f);
		}

		bool digging = _digElapsedMs >= 0f;
		if (digging)
		{
			_digElapsedMs += (float)(delta * 1000.0);
			if (_digElapsedMs >= DigAnimationMs) _digElapsedMs = -1f;
		}

		// Advance the dig-queue: one hit per DigAnimationMs interval. Stone
		// takes two hits before its depth advances, so a `Damaged` result keeps
		// the tile at the head of the queue. Every other result emits
		// `Grid.DigBlocked` — so the hint system flashes a red indicator on
		// the preventing neighbour (step constraint) or on the attempted tile
		// itself (bedrock, fragment at current depth). Random collapse is
		// suppressed for autodig digs (deterministic sweep).
		if (_digQueue.Count > 0)
		{
			_digQueueTimer += (float)(delta * 1000.0);
			if (_digQueueTimer >= DigAnimationMs)
			{
				_digQueueTimer = 0f;
				var result = _grid.Dig(_digQueue[0], allowCollapse: false);
				if (result != Grid.DigResult.Damaged)
				{
					_digQueue.RemoveAt(0);
				}
				// Damaged: stone HP--; retry next interval.
			}
		}

		// Animation requires a fresh draw while limbs are in motion or the dig
		// pose is finishing (so the final frame reverts to idle).
		if (wasMoving || digging) QueueRedraw();

		// On arrival (now or already), fire any pending scan.
		if (_scanPendingOnArrival && Position == _targetPosition)
		{
			_scanPendingOnArrival = false;
			var cell = CurrentTile();
			_grid.TriggerScan(cell.X, cell.Y, _grid.GetDepth(cell.X, cell.Y));
		}
	}

	public override void _Draw()
	{
		if (_grid == null) return;
		float t = _grid.TileSize;
		float headRadius = t * 0.16f;
		float bodyH = t * 0.40f;
		float armLen = t * 0.30f;
		float legLen = t * 0.30f;
		float lineWidth = Mathf.Max(2f, t * 0.08f);

		bool moving = Position != _targetPosition;
		bool digging = _digElapsedMs >= 0f;

		// Walk: gentle upward bob synced to step cadence, alternating leg lift.
		float walkBob = moving ? Mathf.Abs(Mathf.Sin(_walkPhase)) * t * 0.05f : 0f;
		float leftLift = moving ? Mathf.Max(0f, Mathf.Sin(_walkPhase)) : 0f;
		float rightLift = moving ? Mathf.Max(0f, -Mathf.Sin(_walkPhase)) : 0f;
		float leftLegLen = legLen * (1f - leftLift * 0.35f);
		float rightLegLen = legLen * (1f - rightLift * 0.35f);

		// Dig: a one-shot arch (0 → 1 → 0) over `DigAnimationMs`. Arms swing
		// from idle splay toward straight-down; body crouches slightly.
		float digProgress = digging ? _digElapsedMs / DigAnimationMs : 0f;
		float digSwing = digging ? Mathf.Sin(digProgress * Mathf.Pi) : 0f;
		float digCrouch = digSwing * t * 0.06f;

		// Walk lifts upward (-Y); dig pushes downward (+Y).
		float yOff = -walkBob + digCrouch;

		var headCenter = new Vector2(0, -bodyH * 0.5f - headRadius + yOff);
		var bodyTop = headCenter + new Vector2(0, headRadius);
		var bodyBottom = bodyTop + new Vector2(0, bodyH);
		var shoulders = bodyTop + new Vector2(0, bodyH * 0.15f);

		DrawCircle(headCenter, headRadius, BodyColor);
		DrawLine(bodyTop, bodyBottom, BodyColor, lineWidth);

		// Arms: idle splay, lerped toward a straight-down strike during dig.
		var leftArmIdle = new Vector2(-armLen, armLen * 0.5f);
		var rightArmIdle = new Vector2(armLen, armLen * 0.5f);
		var armStrike = new Vector2(0, armLen);
		var leftArmEnd = digging ? leftArmIdle.Lerp(armStrike, digSwing) : leftArmIdle;
		var rightArmEnd = digging ? rightArmIdle.Lerp(armStrike, digSwing) : rightArmIdle;
		DrawLine(shoulders, shoulders + leftArmEnd, BodyColor, lineWidth);
		DrawLine(shoulders, shoulders + rightArmEnd, BodyColor, lineWidth);

		// Legs: alternating shortened length reads as a step.
		DrawLine(bodyBottom, bodyBottom + new Vector2(-legLen * 0.4f, leftLegLen), BodyColor, lineWidth);
		DrawLine(bodyBottom, bodyBottom + new Vector2(legLen * 0.4f, rightLegLen), BodyColor, lineWidth);
	}
}
