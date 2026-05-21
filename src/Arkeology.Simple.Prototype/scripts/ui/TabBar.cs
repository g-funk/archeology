using Godot;

namespace Arkeology.Simple.Prototype;

// Three-tab strip pinned to the bottom of the screen. Each tab is a child
// `Button` named "Grid", "Museum", "Relaxation". Hooks them to ScreenManager.
public partial class TabBar : Control
{
	[Export] public NodePath ScreenManagerPath { get; set; } = new("../../ScreenManager");

	private ScreenManager? _manager;

	public override void _Ready()
	{
		_manager = GetNodeOrNull<ScreenManager>(ScreenManagerPath);
		if (_manager == null)
		{
			GD.PushWarning($"TabBar: could not resolve ScreenManager at '{ScreenManagerPath}'.");
			return;
		}

		var grid = GetNodeOrNull<Button>("Grid");
		var museum = GetNodeOrNull<Button>("Museum");
		var relax = GetNodeOrNull<Button>("Relaxation");
		if (grid != null) grid.Pressed += () => _manager.Show(ScreenManager.Screen.Grid);
		if (museum != null) museum.Pressed += () => _manager.Show(ScreenManager.Screen.Museum);
		if (relax != null) relax.Pressed += () => _manager.Show(ScreenManager.Screen.Relaxation);
	}
}
