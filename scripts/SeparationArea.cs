using Godot;
using System;

public partial class SeparationArea : Area2D
{
	public void OnBodyEntered(Node2D body)
	{
		if (body is Boid boid)
		{
			GetParent<Boid>().EmitSignal("SeparationAdd", boid);
		}
	}
	public void OnBodyExited(Node2D body)
	{
		if (body is Boid boid)
		{
			GetParent<Boid>().EmitSignal("SeparationRemove", boid);
		}
	}
}
