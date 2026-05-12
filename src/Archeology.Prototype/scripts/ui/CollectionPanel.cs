using Godot;

namespace Archeology.Prototype;

public partial class CollectionPanel : Control
{
	[Export] public NodePath GridPath { get; set; } = new("../../Grid");

	// Visual config — tweak these via the inspector to retune the panel look.
	[Export] public int Padding { get; set; } = 12;
	[Export] public int HeaderHeight { get; set; } = 36;
	[Export] public int SlotHeight { get; set; } = 60;
	[Export] public int SlotSpacing { get; set; } = 6;
	[Export] public int CellSize { get; set; } = 12;
	[Export] public int CellSpacing { get; set; } = 1;

	// Layout: panel sticks to the grid's top-right corner each frame.
	[Export] public float GapFromGrid { get; set; } = 16f;
	[Export] public float PanelWidth { get; set; } = 204f;

	private Grid? _grid;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"CollectionPanel: could not resolve Grid at '{GridPath}'.");
			return;
		}
		_grid.FragmentsChanged += _ => QueueRedraw();
	}

	public override void _Process(double delta)
	{
		FollowGrid();
	}

	// Project the grid's top-right and bottom-right corners through the active
	// canvas transform (which accounts for the Camera2D zoom/position) and place
	// the panel against the grid's right edge in viewport space.
	private void FollowGrid()
	{
		if (_grid == null) return;

		var origin = _grid.GlobalPosition;
		float worldW = _grid.Width * _grid.TileSize;
		float worldH = _grid.Height * _grid.TileSize;
		var topRightWorld = origin + new Vector2(worldW, 0);
		var bottomRightWorld = origin + new Vector2(worldW, worldH);

		var canvasXform = GetViewport().GetCanvasTransform();
		var topRightVp = canvasXform * topRightWorld;
		var bottomRightVp = canvasXform * bottomRightWorld;

		Position = new Vector2(topRightVp.X + GapFromGrid, topRightVp.Y);
		Size = new Vector2(PanelWidth, Mathf.Max(0f, bottomRightVp.Y - topRightVp.Y));
	}

	public override void _Draw()
	{
		if (_grid == null) return;

		var size = Size;

		// Panel background — slightly lighter than the page so the panel reads as a frame.
		DrawRect(new Rect2(Vector2.Zero, size), new Color(0.16f, 0.13f, 0.11f), filled: true);

		// Header: "Fragments: N"
		var font = GetThemeDefaultFont();
		int headerFontSize = 16;
		var headerColor = new Color(0.92f, 0.84f, 0.62f);
		var headerText = $"Fragments: {_grid.FragmentsCollected}";
		float headerBaseline = Padding + headerFontSize;
		DrawString(font, new Vector2(Padding, headerBaseline), headerText,
			HorizontalAlignment.Left, -1, headerFontSize, headerColor);

		// Vertical list of slots beneath the header.
		float slotsTop = HeaderHeight + Padding;
		float slotWidth = size.X - 2 * Padding;
		float y = slotsTop;
		foreach (var frag in _grid.CollectedFragments)
		{
			if (y + SlotHeight > size.Y - Padding) break; // overflow guard

			DrawSlot(frag, new Vector2(Padding, y), new Vector2(slotWidth, SlotHeight));
			y += SlotHeight + SlotSpacing;
		}
	}

	private void DrawSlot(Fragment frag, Vector2 origin, Vector2 size)
	{
		// Slot inset background — a darker well so the gold cells pop.
		DrawRect(new Rect2(origin, size), new Color(0.10f, 0.08f, 0.07f), filled: true);

		var template = frag.RelativeCells;
		int maxX = 0, maxY = 0;
		foreach (var c in template)
		{
			if (c.X > maxX) maxX = c.X;
			if (c.Y > maxY) maxY = c.Y;
		}
		int wCells = maxX + 1;
		int hCells = maxY + 1;

		// Auto-scale cells so any generated shape (up to ~16 tiles in odd layouts) fits.
		const float slotInset = 4f;
		float availW = Mathf.Max(1f, size.X - 2 * slotInset);
		float availH = Mathf.Max(1f, size.Y - 2 * slotInset);
		float cellByW = (availW - (wCells - 1) * CellSpacing) / wCells;
		float cellByH = (availH - (hCells - 1) * CellSpacing) / hCells;
		float cellSize = Mathf.Min(CellSize, Mathf.Min(cellByW, cellByH));
		if (cellSize < 1f) cellSize = 1f;
		float spacing = cellSize > 4f ? CellSpacing : 0f;

		float pixW = wCells * cellSize + (wCells - 1) * spacing;
		float pixH = hCells * cellSize + (hCells - 1) * spacing;
		var shapeOrigin = origin + new Vector2(
			(size.X - pixW) / 2f,
			(size.Y - pixH) / 2f);

		// Same gold as an exposed grid tile, so the panel and grid read as the same material.
		var gold = new Color(1.00f, 0.82f, 0.32f);
		foreach (var c in template)
		{
			var cellRect = new Rect2(
				shapeOrigin.X + c.X * (cellSize + spacing),
				shapeOrigin.Y + c.Y * (cellSize + spacing),
				cellSize, cellSize);
			DrawRect(cellRect, gold, filled: true);
		}
	}
}
