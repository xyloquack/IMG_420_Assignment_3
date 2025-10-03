using Godot;
using System;

public partial class HitBox : Area2D
{
	[Export]
	public string EnemyGroup;
	
	public float DamageAmount = 0;
	
	override public void _PhysicsProcess(double delta)
	{
		if (GetParent<Boid>().Active)
		{
			foreach (Node2D body in GetOverlappingBodies()) 
			{
				if (body.IsInGroup(EnemyGroup))
				{
					body.EmitSignal("Damage", DamageAmount);
					GetParent<Boid>().EmitSignal("Kill");
				}
			}
		}
	}
}
