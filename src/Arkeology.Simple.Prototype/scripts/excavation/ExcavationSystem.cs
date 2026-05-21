using Godot;

namespace Arkeology.Simple.Prototype;

// Touch-first input dispatch for the simple version:
//   • Short tap on a fully-exposed fragment → collect (anywhere on the grid).
//   • Short tap on the character's current tile → autodig (3×3 sweep).
//   • Short tap on any other tile → walk the character there.
//   • Long-press on the character's current tile → scan.
//   • Long-press on any other tile → walk (same as short tap).
//
// Mouse and emulated-from-touch events go through the same code path, so the
// prototype works the same on desktop and on a touch device.
public partial class ExcavationSystem : Node
{
	[Export] public NodePath GridPath { get; set; } = new("../Grid");
	[Export] public NodePath PlayerCharacterPath { get; set; } = new("../Grid/PlayerCharacter");
	// Hold duration (in seconds) that flips a tap into a long-press.
	[Export] public float LongPressSeconds { get; set; } = 0.4f;

	private Grid? _grid;
	private PlayerCharacter? _character;

	private bool _down;
	private ulong _downMs;
	private Vector2I _downCell;
	private bool _longResolved;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"ExcavationSystem: could not resolve Grid at '{GridPath}'.");
		}
		_character = GetNodeOrNull<PlayerCharacter>(PlayerCharacterPath);
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
				_down = true;
				_downMs = Time.GetTicksMsec();
				_downCell = cell;
				_longResolved = false;
			}
			else if (_down)
			{
				_down = false;
				if (!_longResolved && _grid.InBounds(_downCell.X, _downCell.Y))
				{
					HandleShortTap(_downCell);
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!_down || _longResolved || _grid == null) return;
		ulong elapsedMs = Time.GetTicksMsec() - _downMs;
		if (elapsedMs >= (ulong)(LongPressSeconds * 1000f))
		{
			_longResolved = true;
			if (_grid.InBounds(_downCell.X, _downCell.Y))
			{
				HandleLongPress(_downCell);
			}
		}
	}

	private void HandleShortTap(Vector2I cell)
	{
		if (_grid == null || _character == null) return;
		// 1. Fully-exposed fragment? Collect (works from anywhere).
		if (_grid.TryCollectFragment(cell)) return;
		// 2. Tap on the character's tile? Autodig.
		if (cell == _character.CurrentTile())
		{
			_character.RequestDigAround();
			return;
		}
		// 3. Otherwise walk.
		_character.MoveTo(cell);
	}

	private void HandleLongPress(Vector2I cell)
	{
		if (_grid == null || _character == null) return;
		if (cell == _character.CurrentTile())
		{
			_character.RequestScanHere();
			return;
		}
		_character.MoveTo(cell);
	}
}
