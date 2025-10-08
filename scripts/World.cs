using Godot;
using System;

public partial class World : Node2D
{
	public float TimePassed;
	private PauseScreen _pauseScreen;
	
	
	override public void _Ready()
	{
		TimePassed = 0.0f;
		_pauseScreen = GetNode<PauseScreen>("PauseScreen");
	}
	
	override public void _PhysicsProcess(double delta)
	{
		TimePassed += (float)delta;
		if (TimePassed > 0.5f)
		{
			TimePassed -= 0.5f;
			GD.Print(Engine.GetFramesPerSecond());
		}
		
		if (Input.IsActionPressed("pause"))
		{
			_pauseScreen.Pause();
		}
	}
}
