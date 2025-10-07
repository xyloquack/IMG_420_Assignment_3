using Godot;
using System;

public partial class HitBox : Area2D
{
	[Export]
	public string EnemyGroup;
	
	public float DamageAmount;
	
	override public void _PhysicsProcess(double delta)
	{
		if (GetParent<Boid>().Active)
		{
			foreach (Node2D area in GetOverlappingAreas()) 
			{
				if (area.IsInGroup(EnemyGroup))
				{
					area.EmitSignal("Damage", DamageAmount);
				}
			}
		}
	}
}
