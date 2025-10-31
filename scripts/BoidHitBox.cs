using Godot;
using System;

public partial class BoidHitBox : Area2D
{
	[Export]
	public string EnemyGroup;
	
	public float DamageAmount = 0;
	
	override public void _PhysicsProcess(double delta)
	{
		if (GetParent<Boid>().Active)
		{
			foreach (Node2D area in GetOverlappingAreas()) 
			{
				if (area.IsInGroup(EnemyGroup))
				{
					area.EmitSignal("Damage", DamageAmount, Vector2.Zero);
					GetParent<Boid>().EmitSignal("Kill");
				}
			}
		}
	}
}
