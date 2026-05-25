using Godot;
using System;

public partial class TabButton : Button
{
	[Export] int TabIndex;
	[Export] TabContainer TabContainer = null!;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += () => TabContainer.CurrentTab = TabIndex;  
	}
}
