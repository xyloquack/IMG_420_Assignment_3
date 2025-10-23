using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float MaxHealth;
	[Export]
	public float Damage;
	[Export]
	public float Knockback;
	
	public bool IsDead = false;
	
	public float Health;
	public AnimatedSprite2D Sprite;
	public Timer FlashTimer;
	public ShaderMaterial ShaderMat;
	public CharacterController Player = null;
	
	override public void _Ready()
	{
		Health = MaxHealth;
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		ShaderMat = (ShaderMaterial)Sprite.Material;
		ShaderMat = (ShaderMaterial)ShaderMat.Duplicate();
		Sprite.Material = ShaderMat;
		ShaderMat.SetShaderParameter("flash_color", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
		FlashTimer = GetNode<Timer>("FlashTimer");
	}
	
	private void OnDamage(float damage, Vector2 knockback)
	{
		Health -= damage;
		GD.Print("Flash!");
		FlashTimer.Start();
		CheckHealth();
	}
	
	virtual public void OnDetectionEntered(Node2D node)
	{
		if (node.IsInGroup("player"))
		{
			PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
			PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(node.GlobalPosition, GlobalPosition);
			var result = spaceState.IntersectRay(query);
			if (result.Count == 0)
			{
				Player = (CharacterController)node;
			}
		}
	}
	
	private void CheckHealth()
	{
		if (Health <= 0)
		{
			GetParent().EmitSignal("RemoveEnemy", this);
			QueueFree();
		}
	}
}
