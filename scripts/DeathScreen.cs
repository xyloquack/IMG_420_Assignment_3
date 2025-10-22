using Godot;
using System;

public partial class DeathScreen : CanvasLayer
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
	
	public void Dead()
	{
		GetTree().Paused = true;
		Show();
	}
	
	private void OnRestart()
	{
		GetTree().Paused = false;
		Hide();
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids = [];
		GetTree().ChangeSceneToFile("uid://cmv6c30k5fpj0");
	}
	
	private void OnMainMenu()
	{
		GetTree().Paused = false;
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids = [];
		GetTree().ChangeSceneToFile("uid://mn86qcay6nmo");
	}
}
