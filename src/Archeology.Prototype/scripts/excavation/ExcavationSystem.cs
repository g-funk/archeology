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

	private Grid? _grid;
	private PlayerCharacter? _character;

	private bool _mouseDown;
	private ulong _mouseDownTimeMs;
	private Vector2I _mouseDownCell;
	private bool _longPressTriggered;

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

		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.S)
		{
			_character?.RequestScanHere();
		}
	}

	public override void _Process(double delta)
	{
		if (!_mouseDown || _longPressTriggered || _character == null || _grid == null) return;

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
}
