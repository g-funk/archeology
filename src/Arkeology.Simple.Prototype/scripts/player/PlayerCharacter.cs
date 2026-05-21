using Godot;
using System.Collections.Generic;

namespace Arkeology.Simple.Prototype;

// The archaeologist — stick-figure that walks to a target tile and runs the
// autodig sweep when triggered. Touch input is handled by `ExcavationSystem`;
// this class just exposes the action API (MoveTo / RequestDigAround /
// RequestScanHere) and animates.
//
// Child of `Grid`; `Position` is in grid-local coordinates.
public partial class PlayerCharacter : Node2D
{
	[Export] public NodePath GridPath { get; set; } = new("..");
	// Optional reference to `StaminaSystem`. When set, dig intervals scale up
	// as stamina drops below its slowdown threshold.
	[Export] public NodePath StaminaPath { get; set; } = new("../../StaminaSystem");

	// Movement speed in tiles per second.
	[Export] public float SpeedTilesPerSecond { get; set; } = 10f;
	// Body / outline colour.
	[Export] public Color BodyColor { get; set; } = new Color(0.95f, 0.92f, 0.85f);
	// Base duration of one dig pose, in milliseconds. Stamina slowdown is added on top.
	[Export] public float DigAnimationMs { get; set; } = 300f;

	private Grid? _grid;
	private StaminaSystem? _stamina;
	private Vector2 _targetPosition;
	private float _walkPhase;
	private float _digElapsedMs = -1f;
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
		_stamina = GetNodeOrNull<StaminaSystem>(StaminaPath);

		// Start on the middle tile of the grid.
		var startCell = new Vector2I(_grid.Width / 2, _grid.Height / 2);
		Position = CellCenter(startCell);
		_targetPosition = Position;

		_grid.Dug += OnDug;
	}

	// True while an autodig sweep is in progress. Movement inputs are ignored
	// in that window — the player can't walk while digging.
	public bool IsAutoDigging => _digQueue.Count > 0;

	private void OnDug(int x, int y, int depth)
	{
		// Every Dug emit re-arms the dig pose.
		_digElapsedMs = 0f;
		QueueRedraw();
	}

	// Walk to the given cell. No-op during autodig.
	public void MoveTo(Vector2I cell)
	{
		if (IsAutoDigging) return;
		if (_grid == null || !_grid.InBounds(cell.X, cell.Y)) return;
		_targetPosition = CellCenter(cell);
	}

	// Fire a scan from the character's current tile immediately.
	public void RequestScanHere()
	{
		if (_grid == null) return;
		var cell = CurrentTile();
		_grid.TriggerScan(cell.X, cell.Y, _grid.GetDepth(cell.X, cell.Y));
	}

	// Queue the tile under the character and its 8 neighbors for sequential
	// digging. Order: centre first, then the ring anti-clockwise from east.
	// Re-triggering resets the queue to the character's current tile.
	public void RequestDigAround()
	{
		if (_grid == null) return;
		_digQueue.Clear();
		var center = CurrentTile();
		if (_grid.InBounds(center.X, center.Y)) _digQueue.Add(center);

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

		// Fire the first tile on the very next _Process.
		_digQueueTimer = CurrentDigIntervalMs();
	}

	public Vector2I CurrentTile()
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

	private float CurrentDigIntervalMs()
	{
		return DigAnimationMs + (_stamina?.CurrentSlowdownMs() ?? 0f);
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
			_walkPhase = Mathf.PosMod(_walkPhase + traveled / _grid.TileSize * Mathf.Pi, Mathf.Pi * 2f);
		}

		bool digging = _digElapsedMs >= 0f;
		if (digging)
		{
			_digElapsedMs += (float)(delta * 1000.0);
			if (_digElapsedMs >= DigAnimationMs) _digElapsedMs = -1f;
		}

		// Autodig queue — one hit per (stamina-scaled) interval.
		if (_digQueue.Count > 0)
		{
			_digQueueTimer += (float)(delta * 1000.0);
			if (_digQueueTimer >= CurrentDigIntervalMs())
			{
				_digQueueTimer = 0f;
				var result = _grid.Dig(_digQueue[0], allowCollapse: false);
				if (result != Grid.DigResult.Damaged)
				{
					_digQueue.RemoveAt(0);
				}
			}
		}

		if (wasMoving || digging) QueueRedraw();
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

		float walkBob = moving ? Mathf.Abs(Mathf.Sin(_walkPhase)) * t * 0.05f : 0f;
		float leftLift = moving ? Mathf.Max(0f, Mathf.Sin(_walkPhase)) : 0f;
		float rightLift = moving ? Mathf.Max(0f, -Mathf.Sin(_walkPhase)) : 0f;
		float leftLegLen = legLen * (1f - leftLift * 0.35f);
		float rightLegLen = legLen * (1f - rightLift * 0.35f);

		float digProgress = digging ? _digElapsedMs / DigAnimationMs : 0f;
		float digSwing = digging ? Mathf.Sin(digProgress * Mathf.Pi) : 0f;
		float digCrouch = digSwing * t * 0.06f;

		float yOff = -walkBob + digCrouch;

		var headCenter = new Vector2(0, -bodyH * 0.5f - headRadius + yOff);
		var bodyTop = headCenter + new Vector2(0, headRadius);
		var bodyBottom = bodyTop + new Vector2(0, bodyH);
		var shoulders = bodyTop + new Vector2(0, bodyH * 0.15f);

		DrawCircle(headCenter, headRadius, BodyColor);
		DrawLine(bodyTop, bodyBottom, BodyColor, lineWidth);

		var leftArmIdle = new Vector2(-armLen, armLen * 0.5f);
		var rightArmIdle = new Vector2(armLen, armLen * 0.5f);
		var armStrike = new Vector2(0, armLen);
		var leftArmEnd = digging ? leftArmIdle.Lerp(armStrike, digSwing) : leftArmIdle;
		var rightArmEnd = digging ? rightArmIdle.Lerp(armStrike, digSwing) : rightArmIdle;
		DrawLine(shoulders, shoulders + leftArmEnd, BodyColor, lineWidth);
		DrawLine(shoulders, shoulders + rightArmEnd, BodyColor, lineWidth);

		DrawLine(bodyBottom, bodyBottom + new Vector2(-legLen * 0.4f, leftLegLen), BodyColor, lineWidth);
		DrawLine(bodyBottom, bodyBottom + new Vector2(legLen * 0.4f, rightLegLen), BodyColor, lineWidth);
	}
}
