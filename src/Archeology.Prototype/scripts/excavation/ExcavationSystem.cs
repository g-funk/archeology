using Godot;

namespace Archeology.Prototype;

public partial class ExcavationSystem : Node
{
	[Export] public NodePath GridPath { get; set; } = new("../Grid");

	private Grid? _grid;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"ExcavationSystem: could not resolve Grid at '{GridPath}'.");
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_grid == null) return;
		if (@event is not InputEventMouseButton btn || !btn.Pressed) return;
		if (btn.ButtonIndex != MouseButton.Left) return;

		var world = _grid.GetViewport().GetCamera2D() != null
			? _grid.GetGlobalMousePosition()
			: btn.Position;

		var cell = _grid.WorldToCell(world);
		if (!_grid.InBounds(cell.X, cell.Y)) return;

		_grid.HandleClick(cell);
	}
}
