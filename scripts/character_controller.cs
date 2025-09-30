using Godot;
using System;

public partial class character_controller : CharacterBody2D
{
	[Export]
	public float speed;
	[Export]
	public float acceleration;
	[Export]
	public float air_acceleration;
	[Export]
	public float friction;
	[Export]
	public float air_friction;
	[Export]
	public float jump_speed;
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
		
		if (!IsOnFloor()) {
			current_acceleration = air_acceleration;
			current_friction = air_friction;
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
					newVelocity.Y = 0;
				}
				GetNode<Timer>("JumpTimer").Stop();
			}
			else {
				newVelocity.Y = jump_speed;
			}
		}
		else {
			newVelocity.Y += current_gravity * (float)delta;
		}
		Velocity = newVelocity;
		MoveAndSlide();
	}
}
