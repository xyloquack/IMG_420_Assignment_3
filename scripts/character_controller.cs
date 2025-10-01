using Godot;
using System;

public partial class character_controller : CharacterBody2D
{
	[Export]
	public float speed;
	[Export]
	public float acceleration;
	[Export]
	public float air_acceleration_mult;
	[Export]
	public float friction;
	[Export]
	public float air_friction_mult;
	[Export]
	public float jump_speed;
	[Export]
	public float burst_jump_speed;
	[Export]
	public float gravity;
	[Export]
	public float slow_fall_mult;
	
	public double time_passed;
	public bool slow_falling;
	public bool jumping;
	
	public override void _Ready() {
		time_passed = 0.0;
		slow_falling = false;
	}
	
	public override void _PhysicsProcess(double delta) {
		time_passed += delta;
		UpdateVelocity(delta);
		UpdateSprite();
		MoveAndSlide();
	}
	
	private void UpdateVelocity(double delta) {
		Vector2 newVelocity = Velocity;
		int direction = 0;
		if (Input.IsActionPressed("left")) {
			direction -= 1;
		}
		if (Input.IsActionPressed("right")) {
			direction += 1;
		}
		float current_friction = friction * Math.Abs(newVelocity.X) / speed;
		float current_acceleration = acceleration;
		float current_gravity = gravity;
		
		if (Math.Abs(newVelocity.X) > speed) {
			current_friction = (Math.Abs(newVelocity.X) / (speed)) * (acceleration - friction) + friction;
		}
		else {
			current_friction = friction;
		}
		
		float frictionAdjustedAcceleration = current_acceleration - current_friction;
		GD.Print("Before");
		GD.Print(current_acceleration);
		current_acceleration = (float)(frictionAdjustedAcceleration * (1 - (Math.Pow((Math.Abs(Velocity.X) / speed), 3))) + current_friction);
		GD.Print("After");
		GD.Print(current_acceleration);
		
		if (!IsOnFloor()) {
			current_acceleration *= air_acceleration_mult;
			current_friction *= air_friction_mult;
		}
			
		newVelocity.X += direction * current_acceleration * (float)delta;
		int current_moving_direction;
		if (newVelocity.X > 0.0) {
			current_moving_direction = 1;
		}
		else {
			current_moving_direction = -1;
		}
		newVelocity.X -= current_moving_direction * current_friction * (float)delta;
		int new_moving_direciton;
		if (newVelocity.X > 0) {
			new_moving_direciton = 1;
		}
		else {
			new_moving_direciton = -1;
		}
		if (current_moving_direction != new_moving_direciton) {
			newVelocity.X = 0;
		}
		if (IsOnFloor() && Input.IsActionPressed("jump")) {
			slow_falling = true;
			jumping = true;
			GetNode<Timer>("JumpTimer").Start();
			newVelocity.Y = -burst_jump_speed;
		}
		if (slow_falling && !(Input.IsActionPressed("jump")) || IsOnFloor()) {
			slow_falling = false;
		}
		if (slow_falling) {
			current_gravity *= slow_fall_mult;
		}
		if (jumping) {
			if (!(Input.IsActionPressed("jump")) || GetNode<Timer>("JumpTimer").IsStopped()) {
				jumping = false;
				if (!(GetNode<Timer>("JumpTimer").IsStopped())) {
					newVelocity.Y /= 2;
				}
				GetNode<Timer>("JumpTimer").Stop();
			}
			else {
				newVelocity.Y = Mathf.Lerp(newVelocity.Y, -jump_speed, (float)0.25);
			}
		}
		else if (!IsOnFloor()){
			newVelocity.Y += current_gravity * (float)delta;
		}
		else {
			newVelocity.Y = 0;
		}
		Velocity = newVelocity;
	}
	
	private void UpdateSprite() {
		AnimatedSprite2D playerSprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		Vector2 newOffset;
		newOffset.X = 0;
		newOffset.Y = (float)(4 * Math.Sin(time_passed * 2.5) - 4);
		playerSprite.Offset = newOffset;
		
		playerSprite.Stop();
		int numFrames = playerSprite.GetSpriteFrames().GetFrameCount("walk");
		int newFrame = (int)Math.Floor(numFrames * (Math.Abs(Velocity.X) / speed));
		if (newFrame >= numFrames) {
			newFrame = numFrames - 1;
		}
		playerSprite.SetFrame(newFrame);
		
		if (Velocity.X < 0) {
			playerSprite.FlipH = true;
		}
		if (Velocity.X > 0) {
			playerSprite.FlipH = false;
		}
	}
}
