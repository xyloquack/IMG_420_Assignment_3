using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterController : CharacterBody2D
{
	[Export]
	public int MaxHealth;
	[Export]
	public float Speed;
	[Export]
	public float DashSpeed;
	[Export]
	public float Acceleration;
	[Export]
	public float AirAccelerationMult;
	[Export]
	public float Friction;
	[Export]
	public float AirFrictionMult;
	[Export]
	public float JumpSpeed;
	[Export]
	public float BurstJumpSpeed;
	[Export]
	public float Gravity;
	[Export]
	public float SlowFallMult;
	[Export]
	public PackedScene BoidScene { get; set; }
	[Export]
	public int MaxBoids;
	
	public double TimePassed;
	public double TimeSinceLastAttack;
	public List<Boid> Boids = [];
	public int NumBoids;
	public int Health;
	private bool _slowFalling;
	private bool _jumping;
	private bool _dashing;
	private AnimatedSprite2D _playerSprite;
	private Timer _invulnerabilityTimer;
	private Timer _dashTimer;
	private Timer _dashCooldown;
	private Timer _jumpTimer;
	private Timer _attackCooldown;
	
	public override void _Ready() 
	{
		TimePassed = 0.0;
		TimeSinceLastAttack = 0.0;
		NumBoids = 0;
		_slowFalling = false;
		_dashing = false; 
		_playerSprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		_invulnerabilityTimer = GetNode<Timer>("InvulnerabilityTimer");
		_dashTimer = GetNode<Timer>("DashTimer");
		_dashCooldown = GetNode<Timer>("DashCooldown");
		_jumpTimer = GetNode<Timer>("JumpTimer");
		_attackCooldown = GetNode<Timer>("AttackCooldown");
		Health = MaxHealth;
	}
	
	public override void _PhysicsProcess(double delta) 
	{
		TimePassed += delta;
		TimeSinceLastAttack += delta;
		UpdateVelocity(delta);
		UpdateSprite();
		AttemptBoidSpawn();
		UpdateIdleBoidGoal();
		if (Input.IsActionPressed("attack"))
		{
			Attack();
		}
		MoveAndSlide();
	}
	
	private void UpdateVelocity(double delta) 
	{
		if (Input.IsActionPressed("dash") && _dashTimer.IsStopped() && _dashCooldown.IsStopped())
		{
			_dashing = true;
			int direction = 0;
			if (Input.IsActionPressed("left")) 
			{
				direction -= 1;
			}
			if (Input.IsActionPressed("right")) 
			{
				direction += 1;
			}
			if (direction == 0)
			{
				if (_playerSprite.FlipH)
				{
					direction = -1;
				}
				else
				{
					direction = 1;
				}
			}
			Vector2 newVelocity = new Vector2(DashSpeed * direction, 0);
			Velocity = newVelocity;
			_dashTimer.Start();
		}
		if (!_dashing)
		{
			Velocity = WalkingVelocity(delta);
		}
	}
	
	private Vector2 WalkingVelocity(double delta)
	{
		Vector2 newVelocity = Velocity;
		int direction = 0;
		if (Input.IsActionPressed("left")) 
		{
			direction -= 1;
		}
		if (Input.IsActionPressed("right")) 
		{
			direction += 1;
		}
		float currentFriction = Friction * Math.Abs(newVelocity.X) / Speed;
		float currentAcceleration = Acceleration;
		float currentGravity = Gravity;
		
		if (Math.Abs(newVelocity.X) > Speed) 
		{
			currentAcceleration = 0;
		}
		
		if (!IsOnFloor()) 
		{
			currentAcceleration *= AirAccelerationMult;
			currentFriction *= AirFrictionMult;
		}
		newVelocity.X = Mathf.Lerp(newVelocity.X, newVelocity.X + (direction * currentAcceleration - currentFriction * Math.Sign(Velocity.X)) * (float)delta, 0.55f);
		int currentMovingDirection = Math.Sign(newVelocity.X);
		newVelocity.X = Mathf.Lerp(newVelocity.X, newVelocity.X - currentMovingDirection * currentFriction * (float)delta, 0.55f);
		int newMovingDirection = Math.Sign(newVelocity.X);
		if (currentMovingDirection != newMovingDirection) 
		{
			newVelocity.X = 0;
		}
		if (IsOnFloor() && Input.IsActionPressed("jump")) 
		{
			_slowFalling = true;
			_jumping = true;
			_jumpTimer.Start();
			newVelocity.Y = -BurstJumpSpeed;
		}
		if (_slowFalling && !(Input.IsActionPressed("jump")) || IsOnFloor()) 
		{
			_slowFalling = false;
		}
		if (_slowFalling) 
		{
			currentGravity *= SlowFallMult;
		}
		if (_jumping) 
		{
			if (!(Input.IsActionPressed("jump")) || _jumpTimer.IsStopped()) 
			{
				_jumping = false;
				if (!(_jumpTimer.IsStopped())) 
				{
					newVelocity.Y /= 4;
				}
				_jumpTimer.Stop();
			}
			else 
			{
				newVelocity.Y = Mathf.Lerp(newVelocity.Y, -JumpSpeed, (float)0.25);
			}
		}
		else if (!IsOnFloor())
		{
			newVelocity.Y += currentGravity * (float)delta;
		}
		else 
		{
			newVelocity.Y = 0;
		}
		return newVelocity;
	}
	
	private void OnDashTimeout()
	{
		_dashing = false;
		_dashCooldown.Start();
	}
	
	private void UpdateSprite() 
	{
		Vector2 newOffset;
		newOffset.X = 0;
		newOffset.Y = (float)(4 * Math.Sin(TimePassed * 2.5) - 4);
		_playerSprite.Offset = newOffset;
		
		if (_dashing)
		{
			_playerSprite.SetFrame(_playerSprite.GetSpriteFrames().GetFrameCount("walk") - 1);
		}
		else
		{
			_playerSprite.Stop();
			int numFrames = _playerSprite.GetSpriteFrames().GetFrameCount("walk") - 1;
			int newFrame = (int)Math.Floor(numFrames * (Math.Abs(Velocity.X) / Speed));
			if (newFrame >= numFrames) 
			{
				newFrame = numFrames - 1;
			}
			_playerSprite.SetFrame(newFrame);
		}
		if (Velocity.X < 0) 
		{
			_playerSprite.FlipH = true;
		}
		if (Velocity.X > 0) 
		{
			_playerSprite.FlipH = false;
		}
		
	}
	
	private void AttemptBoidSpawn() 
	{
		for (int i = 0; i < Math.Floor(Mathf.Clamp(Math.Pow(3, TimeSinceLastAttack) - 1, 0, MaxBoids) - NumBoids); i++)
		{
			Boid newBoid = BoidScene.Instantiate<Boid>();
			Vector2 newPosition;
			newPosition.X = 0f;
			newPosition.Y = (float)(GD.Randf() * 10 - 15);
			newBoid.GlobalPosition = newPosition;
			newBoid.Speed = 200;
			newBoid.GoalSeekingTurnAmount = 0.5f;
			Boids.Add(newBoid);
			AddChild(newBoid);
			NumBoids++;
		}
	}
	
	private void UpdateIdleBoidGoal()
	{
		foreach (Boid boid in Boids)
		{
			Vector2 goalPosition = GlobalPosition + new Vector2(0, -25);
			if (_playerSprite.FlipH)
			{
				goalPosition.X += 40;
			}
			else
			{
				goalPosition.X -= 40;
			}
			boid.Goal = goalPosition;
		}
	}
	
	private void Attack() 
	{
		if (_attackCooldown.IsStopped() && NumBoids > 0)
		{
			TimeSinceLastAttack = 0;
			Vector2 mousePosition = GetGlobalMousePosition();
			foreach (Boid boid in Boids)
			{
				boid.GetNode<Timer>("Lifetime").WaitTime += GD.Randf() * 0.2;
				boid.GetNode<Timer>("Lifetime").Start();
				boid.Goal = mousePosition;
				boid.Speed = 1000;
				boid.Rotation = (float)(Vector2.Right.AngleTo(mousePosition) + GD.Randf() * 3 - 1.5);
				boid.GoalSeekingTurnAmount = 3f;
				boid.Active = true;
			}
			Vector2 velocityChange = Vector2.Zero;
			velocityChange -= (mousePosition - GlobalPosition).Normalized();
			velocityChange.X *= 1100 * (NumBoids / MaxBoids);
			velocityChange.Y *= 600 * (NumBoids / MaxBoids);
			Vector2 newVelocity = Velocity + velocityChange;
			Velocity = newVelocity;
			NumBoids = 0;
			Boids = [];
			_attackCooldown.Start();
		}
	}
	
	private void OnDamage(float damage)
	{
		if (_invulnerabilityTimer.IsStopped())
		{
			Health -= (int)damage;
			GD.Print(Health);
			_invulnerabilityTimer.Start();
		}
	}
}
