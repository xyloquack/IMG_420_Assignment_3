using Godot;
using System;

public partial class PauseScreen : CanvasLayer
{
	private Sprite2D _cursor;
	
	override public void _Ready()
	{
		_cursor = GetNode<Sprite2D>("Cursor");
	}
	
	override public void _Process(double delta)
	{
		_cursor.GlobalPosition = _cursor.GetGlobalMousePosition();
	}
	
	public void Pause()
	{
		GetTree().Paused = true;
		Show();
	}
	
	private void OnResume()
	{
		GetTree().Paused = false;
		Hide();
	}
	
	private void OnMainMenu()
	{
		GetTree().Paused = false;
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids = [];
		GetTree().ChangeSceneToFile("uid://mn86qcay6nmo");
	}
}
