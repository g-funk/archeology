using Godot;

namespace Arkeology.Production.Client;

public partial class CameraController : Camera2D
{
	[Export] public NodePath GridPath { get; set; } = new("../Grid");

	// Region-of-interest carve-outs. The grid is fit into the viewport area
	// that isn't covered by the HUD bars at the top (stamina + collection
	// strip) or the bottom (tab bar).
	[Export] public float HudTopHeight { get; set; } = 130f;
	[Export] public float HudBottomHeight { get; set; } = 110f;
	[Export] public float SidePanelWidth { get; set; } = 0f;
	[Export] public float Margin { get; set; } = 10f;

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
		_grid.MapAdvanced += FitGrid;
	}

	// Picks a zoom level and camera position so the entire grid fits inside the
	// viewport region not covered by the HUD bar (top) or collection panel (right).
	public void FitGrid()
	{
		if (_grid == null) return;
		var viewport = GetViewportRect().Size;

		float left = Margin;
		float top = HudTopHeight + Margin;
		float right = viewport.X - SidePanelWidth - Margin;
		float bottom = viewport.Y - HudBottomHeight - Margin;
		float availW = right - left;
		float availH = bottom - top;

		var gridPixels = HexMetrics.GridPixelSize(_grid.Width, _grid.Height, _grid.TileSize);
		float gridW = gridPixels.X;
		float gridH = gridPixels.Y;
		if (gridW <= 0 || gridH <= 0 || availW <= 0 || availH <= 0) return;

		float zoom = Mathf.Min(availW / gridW, availH / gridH);
		zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
		Zoom = new Vector2(zoom, zoom);

		var gridCenter = _grid.Position + gridPixels / 2f;
		var availableCenter = new Vector2((left + right) / 2f, (top + bottom) / 2f);
		var viewportCenter = viewport / 2f;
		Position = gridCenter + (viewportCenter - availableCenter) / zoom;
	}
}
