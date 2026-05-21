using Godot;

namespace Arkeology.Simple.Prototype;

// Placeholder for the Relaxation Room. In the full game this is where the
// player watches an ad to refill stamina; here a button stands in.
public partial class RelaxationScreen : Control
{
	[Export] public NodePath StaminaPath { get; set; } = new("../../../StaminaSystem");

	private StaminaSystem? _stamina;

	public override void _Ready()
	{
		_stamina = GetNodeOrNull<StaminaSystem>(StaminaPath);
		var btn = GetNodeOrNull<Button>("RefillButton");
		if (btn != null) btn.Pressed += () => _stamina?.Refill();
	}
}
