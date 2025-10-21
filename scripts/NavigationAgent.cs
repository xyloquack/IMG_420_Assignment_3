using Godot;
using System;

public partial class NavigationAgent : NavigationAgent2D
{
	private SmallBird _parent;
	
	override public void _Ready()
	{
		_parent = GetParent<SmallBird>();
	}
	
	override public void _PhysicsProcess(double delta)
	{
		GD.Print("Navigation Target Position: ", TargetPosition);
		_parent.SuggestedVelocity = GetNextPathPosition().Normalized();
	}
}
