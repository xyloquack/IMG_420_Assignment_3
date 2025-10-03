using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterController : CharacterBody2D
{
	[Export]
	public float Speed;
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
	public bool SlowFalling;
	public bool Jumping;
	public bool Flipped = false;
	
	public override void _Ready() 
	{
		TimePassed = 0.0;
		TimeSinceLastAttack = 0.0;
		NumBoids = 0;
		SlowFalling = false;
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
			currentFriction = (Math.Abs(newVelocity.X) / (Speed)) * (Acceleration - Friction) + Friction;
		}
		else 
		{
			currentFriction = Friction;
		}
		
		float frictionAdjustedAcceleration = currentAcceleration - currentFriction;
		currentAcceleration = (float)(frictionAdjustedAcceleration * (1 - (Math.Pow((Math.Abs(Velocity.X) / Speed), 3))) + currentFriction);
		
		if (!IsOnFloor()) 
		{
			currentAcceleration *= AirAccelerationMult;
			currentFriction *= AirFrictionMult;
		}
			
		newVelocity.X += direction * currentAcceleration * (float)delta;
		int currentMovingDirection;
		if (newVelocity.X > 0.0) 
		{
			currentMovingDirection = 1;
		}
		else 
		{
			currentMovingDirection = -1;
		}
		newVelocity.X -= currentMovingDirection * currentFriction * (float)delta;
		int newMovingDirection;
		if (newVelocity.X > 0) 
		{
			newMovingDirection = 1;
		}
		else 
		{
			newMovingDirection = -1;
		}
		if (currentMovingDirection != newMovingDirection) 
		{
			newVelocity.X = 0;
		}
		if (IsOnFloor() && Input.IsActionPressed("jump")) 
		{
			SlowFalling = true;
			Jumping = true;
			GetNode<Timer>("JumpTimer").Start();
			newVelocity.Y = -BurstJumpSpeed;
		}
		if (SlowFalling && !(Input.IsActionPressed("jump")) || IsOnFloor()) 
		{
			SlowFalling = false;
		}
		if (SlowFalling) 
		{
			currentGravity *= SlowFallMult;
		}
		if (Jumping) 
		{
			if (!(Input.IsActionPressed("jump")) || GetNode<Timer>("JumpTimer").IsStopped()) 
			{
				Jumping = false;
				if (!(GetNode<Timer>("JumpTimer").IsStopped())) 
				{
					newVelocity.Y /= 2;
				}
				GetNode<Timer>("JumpTimer").Stop();
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
		Velocity = newVelocity;
	}
	
	private void UpdateSprite() 
	{
		AnimatedSprite2D playerSprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		Vector2 newOffset;
		newOffset.X = 0;
		newOffset.Y = (float)(4 * Math.Sin(TimePassed * 2.5) - 4);
		playerSprite.Offset = newOffset;
		
		playerSprite.Stop();
		int numFrames = playerSprite.GetSpriteFrames().GetFrameCount("walk");
		int newFrame = (int)Math.Floor(numFrames * (Math.Abs(Velocity.X) / Speed));
		if (newFrame >= numFrames) 
		{
			newFrame = numFrames - 1;
		}
		playerSprite.SetFrame(newFrame);
		
		if (Velocity.X < 0) 
		{
			playerSprite.FlipH = true;
			Flipped = true;
		}
		if (Velocity.X > 0) 
		{
			playerSprite.FlipH = false;
			Flipped = false;
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
			if (Flipped)
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
		if (GetNode<Timer>("AttackCooldown").IsStopped())
		{
			NumBoids = 0;
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
			Boids = [];
			GetNode<Timer>("AttackCooldown").Start();
		}
	}
}
