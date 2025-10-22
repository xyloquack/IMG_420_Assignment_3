using Godot;
using System;

public partial class SmallBird : Enemy
{
	[Export]
	public float VerticalSpeed;
	[Export]
	public float HorizontalSpeed;
	[Export]
	public float RandomVSpeedOffset;
	[Export]
	public float Gravity;
	[Export]
	public float WanderRange;
	
	public Vector2 SuggestedVelocity = Vector2.Zero;
	
	private Timer _flapTimer;
	private NavigationAgent2D _navigation;
	private Vector2 _homePosition;
	private float _health;
	
	override public void _Ready()
	{
		base._Ready();
		_flapTimer = GetNode<Timer>("FlapTimer");
		_navigation = GetNode<NavigationAgent2D>("Navigation");
		_homePosition = GlobalPosition;
		GetNode<HitBox>("HitBox").DamageAmount = Damage;
	}
	
	override public void _PhysicsProcess(double delta)
	{
		if (Player == null)
		{
			_navigation.TargetPosition = _homePosition + new Vector2(GD.Randf() * WanderRange, GD.Randf() * WanderRange);
		}
		else
		{
			if (Player.LastFloorHeight != 0f)
			{
				_navigation.TargetPosition = new Vector2(Player.GlobalPosition.X, Player.LastFloorHeight - 50);
			}
			else
			{
				_navigation.TargetPosition = new Vector2(Player.GlobalPosition.X, -50);
			}
		}
		SuggestedVelocity = (_navigation.GetNextPathPosition() - GlobalPosition).Normalized();
		ShaderMat.SetShaderParameter("opacity", FlashTimer.TimeLeft / FlashTimer.WaitTime);
		Vector2 newVelocity = Velocity;
		if (SuggestedVelocity.Y < 0 && _flapTimer.IsStopped()) 
		{
			newVelocity.Y = Flap();
			Sprite.Play();
		}
		newVelocity.X = Mathf.Lerp(newVelocity.X, SuggestedVelocity.X * HorizontalSpeed, 0.3f);
		newVelocity.Y += Gravity * (float)delta;
		Velocity = newVelocity;
		MoveAndSlide();
		if (Velocity.X < 0)
		{
			Sprite.FlipH = false;
		}
		else if (Velocity.X > 0)
		{
			Sprite.FlipH = true;
		}
	}
	
	private float Flap()
	{
		GD.Print("Flap!");
		_flapTimer.Start();
		return -VerticalSpeed - GD.Randf() * RandomVSpeedOffset;
	}
}
