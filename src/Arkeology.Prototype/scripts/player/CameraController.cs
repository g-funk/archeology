using Godot;

namespace Arkeology.Prototype;

public partial class CameraController : Camera2D
{
	[Export] public NodePath GridPath { get; set; } = new("../Grid");

	// Region-of-interest carve-outs. The grid is fit into the viewport area
	// that isn't covered by the HUD bar (top) or the collection panel (right).
	[Export] public float HudTopHeight { get; set; } = 80f;
	[Export] public float SidePanelWidth { get; set; } = 220f;
	[Export] public float Margin { get; set; } = 20f;

	[Export] public float MinZoom { get; set; } = 0.02f;
	[Export] public float MaxZoom { get; set; } = 4f;

	private Grid? _grid;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"CameraController: could not resolve Grid at '{GridPath}'.");
			return;
		}
		FitGrid();
		GetViewport().SizeChanged += FitGrid;
	}

	// Picks a zoom level and camera position so the entire grid fits inside the
	// viewport region not covered by the HUD bar (top) or collection panel (right).
	public void FitGrid()
	{
		if (_grid == null) return;
		var viewport = GetViewportRect().Size;

		float left = Margin;
		float top = HudTopHeight;
		float right = viewport.X - SidePanelWidth;
		float bottom = viewport.Y - Margin;
		float availW = right - left;
		float availH = bottom - top;

		float gridW = _grid.Width * _grid.TileSize;
		float gridH = _grid.Height * _grid.TileSize;
		if (gridW <= 0 || gridH <= 0 || availW <= 0 || availH <= 0) return;

		float zoom = Mathf.Min(availW / gridW, availH / gridH);
		zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
		Zoom = new Vector2(zoom, zoom);

		// Offset the camera so the *available* region (not the full viewport)
		// is what's centered on the grid.
		var gridCenter = _grid.Position + new Vector2(gridW / 2f, gridH / 2f);
		var availableCenter = new Vector2((left + right) / 2f, (top + bottom) / 2f);
		var viewportCenter = viewport / 2f;
		Position = gridCenter + (viewportCenter - availableCenter) / zoom;
	}
}
