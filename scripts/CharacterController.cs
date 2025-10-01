using Godot;
using System;

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
	
	public double TimePassed;
	public bool SlowFalling;
	public bool Jumping;
	
	public override void _Ready() 
	{
		TimePassed = 0.0;
		SlowFalling = false;
	}
	
	public override void _PhysicsProcess(double delta) 
	{
		TimePassed += delta;
		UpdateVelocity(delta);
		UpdateSprite();
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
		}
		if (Velocity.X > 0) 
		{
			playerSprite.FlipH = false;
		}
	}
}
