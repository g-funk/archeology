using Godot;

namespace Arkeology.Simple.Prototype;

// Switches between the three top-level screens: Grid, Museum, Relaxation.
// `World` is the Node2D that holds the grid + character — hidden when a
// non-grid tab is active so the museum / relaxation panels read as full screens.
public partial class ScreenManager : Node
{
	public enum Screen { Grid, Museum, Relaxation }

	[Export] public NodePath GridScreenPath { get; set; } = new();
	[Export] public NodePath MuseumScreenPath { get; set; } = new();
	[Export] public NodePath RelaxationScreenPath { get; set; } = new();
	[Export] public NodePath WorldPath { get; set; } = new();

	[Signal] public delegate void ScreenChangedEventHandler(int screen);

	public Screen Active { get; private set; } = Screen.Grid;

	private CanvasItem? _gridScreen;
	private CanvasItem? _museumScreen;
	private CanvasItem? _relaxationScreen;
	private CanvasItem? _world;

	public override void _Ready()
	{
		_gridScreen = GetNodeOrNull<CanvasItem>(GridScreenPath);
		_museumScreen = GetNodeOrNull<CanvasItem>(MuseumScreenPath);
		_relaxationScreen = GetNodeOrNull<CanvasItem>(RelaxationScreenPath);
		_world = GetNodeOrNull<CanvasItem>(WorldPath);
		Show(Screen.Grid);
	}

	public void Show(Screen screen)
	{
		Active = screen;
		if (_gridScreen != null) _gridScreen.Visible = screen == Screen.Grid;
		if (_museumScreen != null) _museumScreen.Visible = screen == Screen.Museum;
		if (_relaxationScreen != null) _relaxationScreen.Visible = screen == Screen.Relaxation;
		if (_world != null) _world.Visible = screen == Screen.Grid;
		EmitSignal(SignalName.ScreenChanged, (int)screen);
	}
}
