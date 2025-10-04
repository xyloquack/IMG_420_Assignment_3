using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float Health;
	
	private void OnDamage(float damage)
	{
		Health -= damage;
		CheckHealth();
	}
	
	private void CheckHealth()
	{
		if (Health <= 0)
		{
			QueueFree();
		}
	}
}
