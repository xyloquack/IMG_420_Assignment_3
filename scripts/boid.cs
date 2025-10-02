using Godot;
using System;
using System.Collections.Generic;

public partial class Boid : CharacterBody2D
{
	[Export]
	public float Speed;
	[Export]
	public float RotationStrength;
	[Export]
	public float MaxSteeringForce;
	[Export]
	public float IdealDistance;
	[Export]
	public float SeparationTurnAmount;
	[Export]
	public float AlignmentTurnAmount;
	[Export]
	public float CohesionTurnAmount;
	[Export]
	public float GoalSeekingTurnAmount;
	
	public List<Boid> Boids;
	public Vector2 Goal;
	Godot.Collections.Array SeparatingBoids = [];
	Godot.Collections.Array LocalBoids = [];
	
	override public void _Ready() 
	{
		Goal = new Vector2((float)0.0, (float)0.0);
	}
	
	override public void _PhysicsProcess(double delta) 
	{
		Vector2 steeringVector = Vector2.Zero;
		steeringVector += Separation();
		steeringVector += Alignment();
		steeringVector += Cohesion();
		steeringVector += GoalSeeking();
		if (steeringVector.Length() > MaxSteeringForce)
		{
			steeringVector = steeringVector.Normalized() * MaxSteeringForce;
		}
		Vector2 currentHeading = Vector2.FromAngle(Rotation);
		float angleToSteering = currentHeading.AngleTo(steeringVector);
		Rotation = (float)Mathf.Wrap(Rotation + angleToSteering * RotationStrength * delta, -Mathf.Pi, Mathf.Pi);
		Vector2 newVelocity = Vector2.FromAngle(Rotation);
		
		newVelocity *= Speed;
		Velocity = newVelocity;
		MoveAndSlide();
	}
	
	private Vector2 Separation() 
	{
		if (SeparatingBoids.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 repulseVector = new Vector2((float)0.0, (float)0.0);
		foreach (Boid currentBoid in SeparatingBoids) 
		{
			repulseVector += (Position - currentBoid.Position) * (float)(1.0 / ((currentBoid.Position - Position).LengthSquared() + 0.0001));
		}
		return SeparationTurnAmount * repulseVector;
	}
	
	private Vector2 Alignment() 
	{
		if (LocalBoids.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 averageVelocity = Vector2.Zero;
		foreach (Boid currentBoid in LocalBoids) 
		{
			averageVelocity += currentBoid.Velocity.Normalized();
		}
		return AlignmentTurnAmount * averageVelocity.Normalized();
	}
	
	private Vector2 Cohesion() 
	{
		if (LocalBoids.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 averagePosition = new Vector2((float)0.0, (float)0.0);
		foreach (Boid currentBoid in LocalBoids) 
		{
			averagePosition += currentBoid.Position;
		}
		Vector2 neededDirection = (averagePosition - Position).Normalized();
		return CohesionTurnAmount * neededDirection;
	}
	
	private Vector2 GoalSeeking() 
	{
		Vector2 neededDirection = (Goal - Position).Normalized();
		return GoalSeekingTurnAmount * neededDirection;
	}
	
	public void OnSeparationAdd(Node2D body)
	{
		if (body is Boid boid)
		{
			SeparatingBoids.Add(boid);
		}
	}
	
	public void OnSeparationRemove(Node2D body)
	{
		if (body is Boid boid)
		{
			SeparatingBoids.Remove(boid);
		}
	}
	
	public void OnLocalAdd(Node2D body)
	{
		if (body is Boid boid)
		{
			LocalBoids.Add(boid);
		}
		
	}
	
	public void OnLocalRemove(Node2D body)
	{
		if (body is Boid boid)
		{
			LocalBoids.Remove(boid);
		}
	}
}
