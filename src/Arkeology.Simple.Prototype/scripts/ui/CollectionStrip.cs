using Godot;

namespace Arkeology.Simple.Prototype;

// Horizontal strip under the stamina bar showing the fragments collected this
// session. Each fragment is rendered as a tiny gold-cell silhouette using its
// `RelativeCells` layout, same idea as the main prototype's CollectionPanel.
public partial class CollectionStrip : Control
{
	[Export] public NodePath GridPath { get; set; } = new("../../../Grid");

	[Export] public Color BackgroundColor { get; set; } = new Color(0.10f, 0.08f, 0.07f);
	[Export] public Color SlotColor { get; set; } = new Color(0.16f, 0.13f, 0.11f);
	[Export] public Color GoldColor { get; set; } = new Color(1.00f, 0.82f, 0.32f);
	[Export] public int Padding { get; set; } = 8;
	[Export] public int SlotSize { get; set; } = 56;
	[Export] public int SlotSpacing { get; set; } = 6;
	[Export] public int CellSize { get; set; } = 8;
	[Export] public int CellSpacing { get; set; } = 1;

	private Grid? _grid;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushWarning($"CollectionStrip: could not resolve Grid at '{GridPath}'.");
			return;
		}
		_grid.FragmentsChanged += _ => QueueRedraw();
	}

	public override void _Draw()
	{
		var size = Size;
		DrawRect(new Rect2(Vector2.Zero, size), BackgroundColor, filled: true);
		if (_grid == null) return;

		float x = Padding;
		float y = (size.Y - SlotSize) / 2f;
		foreach (var frag in _grid.CollectedFragments)
		{
			if (x + SlotSize > size.X - Padding) break;
			DrawSlot(new Vector2(x, y), new Vector2(SlotSize, SlotSize), frag);
			x += SlotSize + SlotSpacing;
		}
	}

	private void DrawSlot(Vector2 origin, Vector2 size, Fragment frag)
	{
		DrawRect(new Rect2(origin, size), SlotColor, filled: true);

		var template = frag.RelativeCells;
		int maxX = 0, maxY = 0;
		foreach (var c in template)
		{
			if (c.X > maxX) maxX = c.X;
			if (c.Y > maxY) maxY = c.Y;
		}
		int wCells = maxX + 1;
		int hCells = maxY + 1;

		const float slotInset = 3f;
		float availW = Mathf.Max(1f, size.X - 2 * slotInset);
		float availH = Mathf.Max(1f, size.Y - 2 * slotInset);
		float cellByW = (availW - (wCells - 1) * CellSpacing) / wCells;
		float cellByH = (availH - (hCells - 1) * CellSpacing) / hCells;
		float cellSize = Mathf.Min(CellSize, Mathf.Min(cellByW, cellByH));
		if (cellSize < 1f) cellSize = 1f;
		float spacing = cellSize > 3f ? CellSpacing : 0f;

		float pixW = wCells * cellSize + (wCells - 1) * spacing;
		float pixH = hCells * cellSize + (hCells - 1) * spacing;
		var shapeOrigin = origin + new Vector2((size.X - pixW) / 2f, (size.Y - pixH) / 2f);

		foreach (var c in template)
		{
			var rect = new Rect2(
				shapeOrigin.X + c.X * (cellSize + spacing),
				shapeOrigin.Y + c.Y * (cellSize + spacing),
				cellSize, cellSize);
			DrawRect(rect, GoldColor, filled: true);
		}
	}
}
