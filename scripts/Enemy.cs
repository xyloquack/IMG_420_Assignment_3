using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	public enum STATE
	{
		IDLE,
		SWOOP,
		DASH,
		FIRING
	}
	
	[Export]
	public float Health;
	[Export]
	public float HorizontalSpeed;
	[Export]
	public float RandomHSpeedOffset;
	[Export]
	public float VerticalSpeed;
	[Export]
	public float RandomVSpeedOffset;
	[Export]
	public float TargetHeight;
	[Export]
	public float Gravity;
	public bool IsDead = false;
	public STATE State = STATE.IDLE;
	public Vector2 Home;
	public Vector2 Target;
	public AnimatedSprite2D Sprite;
	public Timer FlashTimer;
	public Timer FlapTimer;
	public ShaderMaterial ShaderMat;
	public CharacterBody2D Player = null;
	
	override public void _Ready()
	{
		ShaderMat = (ShaderMaterial)GetNode<AnimatedSprite2D>("EnemySprite").Material;
		FlashTimer = GetNode<Timer>("FlashTimer");
		FlapTimer = GetNode<Timer>("FlapTimer");
		Sprite = GetNode<AnimatedSprite2D>("EnemySprite");
		GetNode<HitBox>("HitBox").DamageAmount = 1;
		Home = GlobalPosition;
	}
	
	override public void _PhysicsProcess(double delta)
	{
		if (Player != null)
		{
			Target = Player.GlobalPosition;
		}
		else
		{
			Target = Home;
		}
		ShaderMat.SetShaderParameter("opacity", FlashTimer.TimeLeft / FlashTimer.WaitTime);
		if (State == STATE.IDLE)
		{
			Idle(delta);
		}
		MoveAndSlide();
	}
	
	private void Idle(double delta)
	{
		if (GlobalPosition.Y > TargetHeight && FlapTimer.IsStopped())
		{
			int direction = Math.Sign(Target.X - GlobalPosition.X);
			if (direction == 0)
			{
				direction = 1;
			}
			if (direction == 1)
			{
				Sprite.FlipH = true;
			}
			if (direction == -1)
			{
				Sprite.FlipH = false;
			}
			Flap(direction);
			FlapTimer.Start();
		}
		Vector2 newVelocity = Velocity;
		newVelocity.X = Mathf.Lerp(newVelocity.X, 0, 0.05f);
		newVelocity.Y += Gravity * (float)delta;
		Velocity = newVelocity;
	}
	
	private void Flap(int x_direction)
	{
		Vector2 newVelocity = Velocity;
		newVelocity.X = x_direction * HorizontalSpeed + GD.Randf() * RandomVSpeedOffset;
		newVelocity.Y = -VerticalSpeed - GD.Randf() * RandomVSpeedOffset;
		Velocity = newVelocity;
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
			Player = (CharacterBody2D)node;
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
			IsDead = true;
			Hide();
		}
	}
}
