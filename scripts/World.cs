using Godot;
using System;

public partial class World : Node2D
{
	public float TimePassed;
	
	override public void _Ready()
	{
		TimePassed = 0.0f;
	}
	
	override public void _PhysicsProcess(double delta)
	{
		TimePassed += (float)delta;
		if (TimePassed > 0.5f)
		{
			TimePassed -= 0.5f;
			GD.Print(Engine.GetFramesPerSecond());
		}
	}
}
