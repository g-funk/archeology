using Godot;

namespace Arkeology.Simple.Prototype;

// Tracks the character's stamina. Drains on every `Grid.Dug` emit (each HP hit
// counts as one tile). Provides a slowdown contribution when stamina drops
// below `StaminaSlowdownLimit` — `PlayerCharacter` adds this to its dig
// interval, so digging gets slower and slower as stamina runs out.
public partial class StaminaSystem : Node
{
	[Export] public NodePath GridPath { get; set; } = new("../Grid");

	[Export] public int StaminaFull { get; set; } = 100;
	[Export] public int StaminaSpend { get; set; } = 1;
	[Export] public int StaminaSlowdownLimit { get; set; } = 10;
	[Export] public float SlowdownTimeMs { get; set; } = 200f;

	[Signal] public delegate void StaminaChangedEventHandler(int current, int max);

	public int Current { get; private set; }
	public int Max => StaminaFull;

	private Grid? _grid;

	public override void _Ready()
	{
		Current = StaminaFull;
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid != null) _grid.Dug += OnDug;
	}

	private void OnDug(int x, int y, int depth)
	{
		Current = Mathf.Max(0, Current - StaminaSpend);
		EmitSignal(SignalName.StaminaChanged, Current, Max);
	}

	// Additional dig delay (ms) due to low stamina.
	// t = (limit - stamina) × slowdownTime when stamina < limit, else 0.
	public float CurrentSlowdownMs()
	{
		if (Current >= StaminaSlowdownLimit) return 0f;
		return (StaminaSlowdownLimit - Current) * SlowdownTimeMs;
	}

	// Public refill (used by the Relaxation Room button).
	public void Refill()
	{
		Current = StaminaFull;
		EmitSignal(SignalName.StaminaChanged, Current, Max);
	}
}
