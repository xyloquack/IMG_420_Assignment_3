using Godot;
using System;
using System.Collections.Generic;

public partial class SmallBird : Enemy
{
	public enum BirdState
	{
		Idle,
		Follow,
		Telegraphing,
		Dive
	}
	
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
	[Export]
	public float DiveDistance;
	[Export]
	public float TelegraphingTime;
	[Export]
	public float DiveWaitTime;
	[Export]
	public float DiveDuration;
	[Export]
	public float DiveSpeed;
	[Export]
	public PackedScene TelegraphParticles { get; set; }
	
	public Vector2 SuggestedVelocity = Vector2.Zero;
	public BirdState State = BirdState.Idle;
	
	private Timer _flapTimer;
	private NavigationAgent2D _navigation;
	private Vector2 _homePosition;
	private float _health;
	private float _remainingTelegraphingTime;
	private float _remainingDiveWaitTime;
	private float _remainingDiveDuration;
	private bool _diving = false;
	private AudioStreamPlayer2D _telegraphSound;
	
	override public void _Ready()
	{
		base._Ready();
		_flapTimer = GetNode<Timer>("FlapTimer");
		_navigation = GetNode<NavigationAgent2D>("Navigation");
		_homePosition = GlobalPosition;
		GetNode<HitBox>("HitBox").DamageAmount = Damage;
		_remainingDiveWaitTime = DiveWaitTime;
		_telegraphSound = GetNode<AudioStreamPlayer2D>("TelegraphSound");
	}
	
	override public void _PhysicsProcess(double delta)
	{
		switch (State)
		{
			case BirdState.Idle:
				_navigation.TargetPosition = _homePosition + new Vector2(GD.Randf() * WanderRange, GD.Randf() * WanderRange);
				break;
			case BirdState.Follow:
				if (Player != null)
				{
					if (Player.LastFloorHeight != 0f)
					{
						_navigation.TargetPosition = new Vector2(Player.GlobalPosition.X, Player.LastFloorHeight - 50);
					}
					else
					{
						_navigation.TargetPosition = new Vector2(Player.GlobalPosition.X, -50);
					}
					if ((_navigation.TargetPosition - GlobalPosition).Length() < DiveDistance)
					{
						PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
						PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(_navigation.TargetPosition, GlobalPosition);
						var result = spaceState.IntersectRay(query);
						if (result.Count == 0)
						{
							_remainingDiveWaitTime -= (float)delta;
						}
						if (_remainingDiveWaitTime <= 0)
						{
							State = BirdState.Telegraphing;
							_remainingTelegraphingTime = TelegraphingTime;
						}
					}
				}
				break;
			case BirdState.Dive:
				
				if (_remainingDiveDuration == DiveDuration)
				{
					_navigation.TargetPosition = Player.GlobalPosition + Player.Velocity * 0.1f;
					PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
					PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(_navigation.TargetPosition, GlobalPosition);
					var result = spaceState.IntersectRay(query);
					if (result.Count == 0)
					{
						_remainingDiveDuration -= (float)delta;
						_diving = true;
					}
				}
				if (_diving)
				{
					_remainingDiveDuration -= (float)delta;
				}
				if (_remainingDiveDuration <= 0 || (_navigation.TargetPosition - GlobalPosition).Length() < 10)
				{
					State = BirdState.Follow;
					_remainingDiveWaitTime = DiveWaitTime;
					_diving = false;
				}
				break;
			default:
				break;
		}
		SuggestedVelocity = (_navigation.GetNextPathPosition() - GlobalPosition).Normalized();
		Vector2 newVelocity;
		switch (State)
		{
			case BirdState.Idle:
			case BirdState.Follow:
				newVelocity = Velocity;
				if (SuggestedVelocity.Y < 0 && _flapTimer.IsStopped()) 
				{
					newVelocity.Y = Flap();
					Sprite.Play();
				}
				newVelocity.X = Mathf.Lerp(newVelocity.X, SuggestedVelocity.X * HorizontalSpeed, 0.3f);
				newVelocity.Y += Gravity * (float)delta;
				Velocity = newVelocity;
				break;
				
			case BirdState.Telegraphing:
				if (_remainingTelegraphingTime == TelegraphingTime)
				{
					GpuParticles2D particles = TelegraphParticles.Instantiate<GpuParticles2D>();
					particles.Emitting = true;
					_telegraphSound.PitchScale = (float)(0.95 + GD.Randf() * 0.1);
					_telegraphSound.Play();
					AddChild(particles);
				}
				newVelocity = Velocity;
				newVelocity.X = Mathf.Lerp(newVelocity.X, 0, 0.2f);
				newVelocity.Y = Mathf.Lerp(newVelocity.Y, 0, 0.2f);
				Velocity = newVelocity;
				_remainingTelegraphingTime -= (float)delta;
				if (_remainingTelegraphingTime <= 0)
				{
					State = BirdState.Dive;
					_remainingDiveDuration = DiveDuration;
				}
				break;
				
			case BirdState.Dive:
				newVelocity = Velocity;
				newVelocity.X = Mathf.Lerp(Velocity.X, SuggestedVelocity.X * DiveSpeed, 0.3f);
				newVelocity.Y = Mathf.Lerp(Velocity.Y, SuggestedVelocity.Y * DiveSpeed, 0.3f);
				Velocity = newVelocity;
				break;
		}
		MoveAndSlide();
		ShaderMat.SetShaderParameter("opacity", FlashTimer.TimeLeft / FlashTimer.WaitTime);
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
	
	override public void OnDetectionEntered(Node2D node)
	{
		base.OnDetectionEntered(node);
		if (Player != null)
		{
			State = BirdState.Follow;
		}
	}
}
