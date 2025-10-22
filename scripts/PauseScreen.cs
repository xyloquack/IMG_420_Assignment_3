using Godot;
using System;

public partial class PauseScreen : CanvasLayer
{
	private Sprite2D _cursor;
	private Timer _unpauseTimer;
	private Timer _pauseTimer;
	
	override public void _Ready()
	{
		_cursor = GetNode<Sprite2D>("Cursor");
		_unpauseTimer = GetNode<Timer>("UnpauseTimer");
		_pauseTimer = GetNode<Timer>("PauseTimer");
	}
	
	override public void _Process(double delta)
	{
		_cursor.GlobalPosition = _cursor.GetGlobalMousePosition();
		if (Input.IsActionJustPressed("pause") && _unpauseTimer.IsStopped() && GetTree().Paused == true)
		{
			OnResume();
		}
	}
	
	public void Pause()
	{
		if (_pauseTimer.IsStopped())
		{
			GetTree().Paused = true;
			_unpauseTimer.Start();
			Show();
		}
	}
	
	private void OnResume()
	{
		Hide();
		GetTree().Paused = false;
		_pauseTimer.Start();
	}
	
	private void OnMainMenu()
	{
		GetTree().Paused = false;
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids = [];
		GetTree().ChangeSceneToFile("uid://mn86qcay6nmo");
	}
}
