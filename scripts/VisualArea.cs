using Godot;
using System;

public partial class VisualArea : Area2D
{
	public void OnBodyEntered(Node2D body)
	{
		if (body is Boid boid)
		{
			GetParent<Boid>().EmitSignal("LocalBoidAdd", boid);
		}
	}
	public void OnBodyExited(Node2D body)
	{
		if (body is Boid boid)
		{
			GetParent<Boid>().EmitSignal("LocalBoidRemove", boid);
		}
	}
}
