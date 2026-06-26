using Godot;

namespace Arkeology.Production.Client;

public partial class CameraController : Camera2D
{
	[Export] public NodePath GridPath { get; set; } = new("../Grid");

	// When set, the camera fits the grid into this Control's actual screen rect
	// instead of using the manual HudTopHeight / HudBottomHeight values.
	// Point this at the GridScreenOverlay node so the grid always fills exactly
	// the area between the collection strip and the tab bar, regardless of their sizes.
	[Export] public NodePath? GridOverlayPath { get; set; }

	// Fallback carve-outs used when GridOverlayPath is not set.
	[Export] public float HudTopHeight { get; set; } = 130f;
	[Export] public float HudBottomHeight { get; set; } = 110f;
	[Export] public float SidePanelWidth { get; set; } = 0f;
	[Export] public float Margin { get; set; } = 10f;

	[Export] public float MinZoom { get; set; } = 0.02f;
	[Export] public float MaxZoom { get; set; } = 4f;

	private Grid? _grid;
	private Control? _gridOverlay;

	public override void _Ready()
	{
		_grid = GetNodeOrNull<Grid>(GridPath);
		if (_grid == null)
		{
			GD.PushError($"CameraController: could not resolve Grid at '{GridPath}'.");
			return;
		}

		if (GridOverlayPath != null && !GridOverlayPath.IsEmpty)
			_gridOverlay = GetNodeOrNull<Control>(GridOverlayPath);

		if (_gridOverlay != null)
			_gridOverlay.Resized += FitGrid;

		GetViewport().SizeChanged += FitGrid;
		_grid.MapAdvanced += FitGrid;

		// Defer the initial fit so the HUD layout pass has completed first.
		CallDeferred(MethodName.FitGrid);
	}

	// Picks a zoom level and camera position so the entire grid fits inside
	// the available screen region — either the GridOverlay's actual rect or
	// the manually specified HUD margins.
	public void FitGrid()
	{
		if (_grid == null) return;

		float left, top, right, bottom;

		if (_gridOverlay != null)
		{
			var rect = _gridOverlay.GetGlobalRect();
			left   = rect.Position.X + Margin;
			top    = rect.Position.Y + Margin;
			right  = rect.End.X - SidePanelWidth - Margin;
			bottom = rect.End.Y - Margin;
		}
		else
		{
			var viewport = GetViewportRect().Size;
			left   = Margin;
			top    = HudTopHeight + Margin;
			right  = viewport.X - SidePanelWidth - Margin;
			bottom = viewport.Y - HudBottomHeight - Margin;
		}

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
		var viewportCenter = GetViewportRect().Size / 2f;
		Position = gridCenter + (viewportCenter - availableCenter) / zoom;
	}
}
