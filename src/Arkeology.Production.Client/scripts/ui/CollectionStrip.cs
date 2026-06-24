using Godot;

namespace Arkeology.Production.Client;

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

	private readonly Vector2[] _hexVerts = new Vector2[6];

	private void DrawSlot(Vector2 origin, Vector2 size, Fragment frag)
	{
		DrawRect(new Rect2(origin, size), SlotColor, filled: true);

		var template = frag.RelativeCells;
		if (template.Count == 0) return;

		// Preserve the row parity of the original placement so the shape stagger
		// matches how it appeared in the hex grid.
		int minOrigY = int.MaxValue;
		foreach (var c in frag.Cells)
			if (c.Y < minOrigY) minOrigY = c.Y;
		int yParity = minOrigY & 1;

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

		// Fit hex grid into slot: R such that GridPixelSize(wCells, hCells, R) <= avail.
		float rByW = availW / ((wCells + 0.5f) * 1.7320508f);
		float rByH = availH / (hCells * 1.5f + 0.5f);
		float cellR = Mathf.Min(CellSize, Mathf.Min(rByW, rByH));
		if (cellR < 1f) cellR = 1f;

		// Compute actual pixel bounds using parity-adjusted rows, then center.
		float hexHalfW = cellR * 0.8660254f;
		float minPX = float.MaxValue, maxPX = float.MinValue;
		float minPY = float.MaxValue, maxPY = float.MinValue;
		foreach (var c in template)
		{
			var pos = HexMetrics.CellCenter(c.X, c.Y + yParity, cellR);
			if (pos.X - hexHalfW < minPX) minPX = pos.X - hexHalfW;
			if (pos.X + hexHalfW > maxPX) maxPX = pos.X + hexHalfW;
			if (pos.Y - cellR   < minPY) minPY = pos.Y - cellR;
			if (pos.Y + cellR   > maxPY) maxPY = pos.Y + cellR;
		}

		var shapeOrigin = origin + new Vector2(
			slotInset + (availW - (maxPX - minPX)) * 0.5f - minPX,
			slotInset + (availH - (maxPY - minPY)) * 0.5f - minPY);

		foreach (var c in template)
		{
			var center = HexMetrics.CellCenter(c.X, c.Y + yParity, cellR) + shapeOrigin;
			HexMetrics.HexVerticesAt(center, cellR, _hexVerts);
			DrawColoredPolygon(_hexVerts, GoldColor);
		}
	}
}
