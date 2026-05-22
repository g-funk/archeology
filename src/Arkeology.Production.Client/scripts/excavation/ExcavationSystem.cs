using Godot;

namespace Arkeology.Production.Client;

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
	// Maximum gap between two move-type taps on the same cell that counts as a
	// double-tap → move+dig.
	[Export] public float DoubleTapMs { get; set; } = 300f;

	private Grid? _grid;
	private PlayerCharacter? _character;

	private bool _down;
	private ulong _downMs;
	private Vector2I _downCell;
	private bool _longResolved;

	// Tracks the previous move-style tap so the next tap can be promoted into a
	// double-tap (move+dig). Only "move" taps participate — collect / autodig /
	// scan reset the buffer.
	private bool _haveLastMoveTap;
	private ulong _lastMoveTapMs;
	private Vector2I _lastMoveTapCell;

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
		if (_grid.TryCollectFragment(cell))
		{
			_haveLastMoveTap = false;
			return;
		}
		// 2. Tap on the character's tile? Autodig.
		if (cell == _character.CurrentTile())
		{
			_character.RequestDigAround();
			_haveLastMoveTap = false;
			return;
		}
		// 3. Move-style tap. Check for double-tap on the same cell within the window.
		ulong now = Time.GetTicksMsec();
		bool doubleTap = _haveLastMoveTap
			&& cell == _lastMoveTapCell
			&& (now - _lastMoveTapMs) <= (ulong)DoubleTapMs;
		if (doubleTap)
		{
			_character.MoveAndDig(cell);
			_haveLastMoveTap = false;
		}
		else
		{
			_character.MoveTo(cell);
			_haveLastMoveTap = true;
			_lastMoveTapMs = now;
			_lastMoveTapCell = cell;
		}
	}

	private void HandleLongPress(Vector2I cell)
	{
		if (_grid == null || _character == null) return;
		if (cell == _character.CurrentTile())
		{
			_character.RequestScanHere();
			_haveLastMoveTap = false;
			return;
		}
		_character.MoveTo(cell);
		_haveLastMoveTap = false;
	}
}
