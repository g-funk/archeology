using Godot;

namespace Archeology.Prototype;

// Handles player input on the grid:
//   - short left-click → dig / collect (`Grid.HandleClick`)
//   - long left-click  → walk the character to that tile and scan on arrival
//   - 'S' key          → scan at the character's current tile immediately
public partial class ExcavationSystem : Node
{
	[Export] public NodePath GridPath { get; set; } = new("../Grid");
	[Export] public NodePath PlayerCharacterPath { get; set; } = new("../Grid/PlayerCharacter");
	// Mouse-button hold duration (in seconds) that flips a click into a long-press.
	[Export] public float LongPressSeconds { get; set; } = 0.4f;
	// Arrow-key hold duration (in ms) before continuous stepping kicks in.
	// The first step fires immediately on a press; further steps from the same
	// hold require this threshold so taps reliably move just one tile even when
	// the character finishes its walk while the key is still held.
	[Export] public float ContinuousStepHoldMs { get; set; } = 250f;

	private Grid? _grid;
	private PlayerCharacter? _character;

	private bool _mouseDown;
	private ulong _mouseDownTimeMs;
	private Vector2I _mouseDownCell;
	private bool _longPressTriggered;

	private (int X, int Y) _lastArrowDir;
	private ulong _arrowPressedAtMs;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"ExcavationSystem: could not resolve Grid at '{GridPath}'.");
		}
		_character = GetNodeOrNull<PlayerCharacter>(PlayerCharacterPath);
		// _character may be null if the node isn't in the scene — scan inputs
		// will then silently no-op.
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_grid == null) return;

		if (@event is InputEventMouseButton btn && btn.ButtonIndex == MouseButton.Left)
		{
			var world = _grid.GetViewport().GetCamera2D() != null
				? _grid.GetGlobalMousePosition()
				: btn.Position;
			var cell = _grid.WorldToCell(world);

			if (btn.Pressed)
			{
				_mouseDown = true;
				_mouseDownTimeMs = Time.GetTicksMsec();
				_mouseDownCell = cell;
				_longPressTriggered = false;
			}
			else if (_mouseDown)
			{
				_mouseDown = false;
				// Short click: resolve as dig/collect at the cell that was pressed.
				// Long-press was already resolved in _Process when the threshold hit.
				if (!_longPressTriggered && _grid.InBounds(_mouseDownCell.X, _mouseDownCell.Y))
				{
					_grid.HandleClick(_mouseDownCell);
				}
			}
			return;
		}

		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.S) _character?.RequestScanHere();
			else if (key.Keycode == Key.D) _character?.RequestDigAround();
			else if (key.Keycode == Key.C) _character?.RequestCollect();
		}
	}

	public override void _Process(double delta)
	{
		if (_grid == null) return;

		// Long-press latches mid-hold as soon as the threshold is crossed.
		if (_mouseDown && !_longPressTriggered && _character != null)
		{
			ulong elapsedMs = Time.GetTicksMsec() - _mouseDownTimeMs;
			if (elapsedMs >= (ulong)(LongPressSeconds * 1000f))
			{
				_longPressTriggered = true;
				if (_grid.InBounds(_mouseDownCell.X, _mouseDownCell.Y))
				{
					_character.RequestScanAt(_mouseDownCell);
				}
			}
		}

		// Arrow keys:
		//   • Press transition (or direction change) → one step immediately.
		//   • Same direction held ≥ ContinuousStepHoldMs → keep stepping
		//     (RequestStep's guard limits firing to tile arrivals).
		//   • Pure release → idle; next press is treated as a fresh press.
		//
		// Without the threshold, a tap longer than one tile's walk time (~100 ms
		// at default speed) would land an extra step on the arrival frame.
		if (_character != null)
		{
			int dx = 0, dy = 0;
			if (Input.IsKeyPressed(Key.Left)) dx -= 1;
			if (Input.IsKeyPressed(Key.Right)) dx += 1;
			if (Input.IsKeyPressed(Key.Up)) dy -= 1;
			if (Input.IsKeyPressed(Key.Down)) dy += 1;

			var dir = (dx, dy);
			bool isIdle = dx == 0 && dy == 0;
			bool wasIdle = _lastArrowDir.X == 0 && _lastArrowDir.Y == 0;
			bool isNewDirection = !isIdle && (wasIdle || dir != _lastArrowDir);

			if (isNewDirection)
			{
				_character.RequestStep(dx, dy);
				_arrowPressedAtMs = Time.GetTicksMsec();
			}
			else if (!isIdle)
			{
				ulong heldMs = Time.GetTicksMsec() - _arrowPressedAtMs;
				if (heldMs >= (ulong)ContinuousStepHoldMs)
				{
					_character.RequestStep(dx, dy);
				}
			}

			_lastArrowDir = dir;
		}
	}
}
