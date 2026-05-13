using Godot;

namespace Archeology.Prototype;

// The archaeologist — a simple stick-figure that walks toward whatever tile
// the player clicks. Phase 1: cosmetic only; the click still drives the
// existing dig / collect behavior independently.
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

	private Grid? _grid;
	private Vector2 _targetPosition;
	// Set by RequestScanAt; flips false once a scan fires on arrival.
	private bool _scanPendingOnArrival;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"PlayerCharacter: could not resolve Grid at '{GridPath}'.");
			return;
		}

		// Start on the middle tile of the left edge.
		var startCell = new Vector2I(0, _grid.Height / 2);
		Position = CellCenter(startCell);
		_targetPosition = Position;

		_grid.Clicked += OnClicked;
	}

	private void OnClicked(int x, int y)
	{
		// A normal click moves the character but does not queue a scan; the
		// short-click path runs `Grid.HandleClick` for dig/collect side-effects.
		_targetPosition = CellCenter(new Vector2I(x, y));
	}

	// Walk to this cell, then fire a scan from it once arrived. Used by the
	// long-press input gesture in ExcavationSystem.
	public void RequestScanAt(Vector2I cell)
	{
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

		if (Position != _targetPosition)
		{
			float maxStep = SpeedTilesPerSecond * _grid.TileSize * (float)delta;
			Position = Position.MoveToward(_targetPosition, maxStep);
		}

		// On arrival (now or already), fire any pending scan.
		if (_scanPendingOnArrival && Position == _targetPosition)
		{
			_scanPendingOnArrival = false;
			var cell = CurrentTile();
			_grid.TriggerScan(cell.X, cell.Y, _grid.GetDepth(cell.X, cell.Y));
		}
		// No QueueRedraw needed: the figure is drawn at local (0,0); changing
		// Position re-transforms the cached draw automatically.
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

		// All offsets are relative to local (0,0) so the figure follows Position.
		var headCenter = new Vector2(0, -bodyH * 0.5f - headRadius);
		var bodyTop = headCenter + new Vector2(0, headRadius);
		var bodyBottom = bodyTop + new Vector2(0, bodyH);
		var shoulders = bodyTop + new Vector2(0, bodyH * 0.15f);

		DrawCircle(headCenter, headRadius, BodyColor);
		DrawLine(bodyTop, bodyBottom, BodyColor, lineWidth);
		DrawLine(shoulders, shoulders + new Vector2(-armLen, armLen * 0.5f), BodyColor, lineWidth);
		DrawLine(shoulders, shoulders + new Vector2(armLen, armLen * 0.5f), BodyColor, lineWidth);
		DrawLine(bodyBottom, bodyBottom + new Vector2(-legLen * 0.4f, legLen), BodyColor, lineWidth);
		DrawLine(bodyBottom, bodyBottom + new Vector2(legLen * 0.4f, legLen), BodyColor, lineWidth);
	}
}
