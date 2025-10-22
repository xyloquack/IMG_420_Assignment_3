using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float MaxHealth;
	[Export]
	public float Damage;
	
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
		FlashTimer = GetNode<Timer>("FlashTimer");
	}
	
	private void OnDamage(float damage)
	{
		Health -= damage;
		ShaderMat.SetShaderParameter("enable", true);
		FlashTimer.Start();
		CheckHealth();
	}
	
	private void OnDetectionEntered(Node2D node)
	{
		if (node.IsInGroup("player"))
		{
			Player = (CharacterController)node;
		}
	}
	
	private void OnFlashTimeout()
	{
		ShaderMat.SetShaderParameter("enable", false);
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
