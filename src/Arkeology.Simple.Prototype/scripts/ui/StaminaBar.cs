using Godot;

namespace Arkeology.Simple.Prototype;

// Horizontal stamina bar pinned to the top of the screen. Subscribes to
// `StaminaSystem.StaminaChanged` and redraws on each emit.
public partial class StaminaBar : Control
{
	[Export] public NodePath StaminaPath { get; set; } = new("../../../StaminaSystem");

	[Export] public Color BackgroundColor { get; set; } = new Color(0.12f, 0.10f, 0.08f);
	[Export] public Color FillColor { get; set; } = new Color(0.42f, 0.74f, 0.46f);
	[Export] public Color LowColor { get; set; } = new Color(0.90f, 0.48f, 0.30f);
	[Export] public Color LabelColor { get; set; } = new Color(0.96f, 0.92f, 0.85f);
	[Export] public int FontSize { get; set; } = 16;
	[Export] public int Inset { get; set; } = 6;

	private StaminaSystem? _stamina;

	public override void _Ready()
	{
		_stamina = GetNodeOrNull<StaminaSystem>(StaminaPath);
		if (_stamina != null)
		{
			_stamina.StaminaChanged += (_, __) => QueueRedraw();
		}
		else
		{
			GD.PushWarning($"StaminaBar: could not resolve StaminaSystem at '{StaminaPath}'.");
		}
	}

	public override void _Draw()
	{
		var size = Size;
		DrawRect(new Rect2(Vector2.Zero, size), BackgroundColor, filled: true);

		if (_stamina == null) return;

		float ratio = _stamina.Max > 0 ? (float)_stamina.Current / _stamina.Max : 0f;
		var fillSize = new Vector2((size.X - 2 * Inset) * Mathf.Clamp(ratio, 0f, 1f), size.Y - 2 * Inset);
		var fillRect = new Rect2(new Vector2(Inset, Inset), fillSize);
		var color = _stamina.Current < _stamina.StaminaSlowdownLimit ? LowColor : FillColor;
		DrawRect(fillRect, color, filled: true);

		var font = GetThemeDefaultFont();
		var text = $"Stamina  {_stamina.Current} / {_stamina.Max}";
		DrawString(font, new Vector2(Inset + 6, size.Y / 2 + FontSize / 2 - 2), text,
			HorizontalAlignment.Left, -1, FontSize, LabelColor);
	}
}
