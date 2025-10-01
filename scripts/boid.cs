using Godot;
using System;

public partial class Boid : CharacterBody2D
{
	[Export]
	public float Speed;
	[Export]
	public float IdealDistance;
	[Export]
	public float SeparationRadius;
	[Export]
	public float VisualRadius;
	[Export]
	public float SeparationTurnAmount;
	[Export]
	public float AlignmentTurnAmount;
	[Export]
	public float CohesionTurnAmount;
	[Export]
	public float GoalSeekingTurnAmount;
	
	public Godot.Collections.Array Boids;
	public Vector2 Goal;
	
	override public void _Ready() 
	{
		Boids = [];
		Goal = new Vector2((float)0.0, (float)0.0);
	}
	
	override public void _PhysicsProcess(double delta) 
	{
		Godot.Collections.Array separatingBoids = [];
		Godot.Collections.Array localBoids = [];
		float rotationAdjustment = 0;
		CategorizeLocalBoids(separatingBoids, localBoids);
		rotationAdjustment += Separation(separatingBoids, delta);
		rotationAdjustment += Alignment(localBoids, delta);
		rotationAdjustment += Cohesion(localBoids, delta);
		rotationAdjustment += GoalSeeking(delta);
		Rotation = (float)Mathf.Lerp(Rotation, Rotation + rotationAdjustment, 0.2);
		Vector2 newVelocity = Vector2.FromAngle(Rotation);
		float speedMult = ((Goal - Position).Length() / IdealDistance);
		if (speedMult > 1)
		{
			speedMult = 1;
		}
		else if (speedMult < 0.7)
		{
			speedMult = (float)0.7;
		}
		newVelocity *= Speed * speedMult;
		Velocity = newVelocity;
		MoveAndSlide();
	}
	
	private void CategorizeLocalBoids(Godot.Collections.Array separatingBoids, Godot.Collections.Array localBoids) 
	{
		foreach (CharacterBody2D currentBoid in Boids) 
		{
			if (currentBoid != this) 
			{
				if (currentBoid.Position.DistanceTo(Position) <= SeparationRadius) 
				{
					separatingBoids.Add(currentBoid);
				}
				if (currentBoid.Position.DistanceTo(Position) <= VisualRadius) 
				{
					localBoids.Add(currentBoid);
				}
			}
		}
	}
	
	private float Separation(Godot.Collections.Array separatingBoids, double delta) 
	{
		Vector2 repulseVector = new Vector2((float)0.0, (float)0.0);
		float numBoidsScalar = (float)(1.0 / separatingBoids.Count);
		foreach (CharacterBody2D currentBoid in separatingBoids) 
		{
			repulseVector += (Position - currentBoid.Position) * (float)(1.0 / (Math.Pow((currentBoid.Position - Position).Length(), 2) + 0.0001));
		}
		int direction = 0;
		Vector2 headingDirection = Vector2.FromAngle(Rotation);
		if (headingDirection.Cross(repulseVector) > 0) 
		{
			direction = 1;
		}
		else 
		{
			direction = -1;
		}
		return (float)(direction * SeparationTurnAmount * Math.Abs(headingDirection.Cross(repulseVector)) * delta);
	}
	
	private float Alignment(Godot.Collections.Array localBoids, double delta) 
	{
		float averageRotation = 0;
		float numBoidsScalar = (float)(1.0 / localBoids.Count);
		foreach (CharacterBody2D currentBoid in localBoids) 
		{
			averageRotation += currentBoid.Rotation * numBoidsScalar;
		}
		int direction = (Mathf.Wrap(averageRotation - Rotation, -2 * Math.PI, 2 * Math.PI) > 0) ? 1 : -1;
		return (float)(direction * AlignmentTurnAmount * (Math.Abs(averageRotation - Rotation) / (2 * Math.PI)) * delta);
	}
	
	private float Cohesion(Godot.Collections.Array localBoids, double delta) 
	{
		Vector2 averagePosition = new Vector2((float)0.0, (float)0.0);
		float numBoidsScalar = (float)(1.0 / localBoids.Count);
		foreach (CharacterBody2D currentBoid in localBoids) 
		{
			averagePosition += currentBoid.Position * numBoidsScalar;
		}
		int direction = 0;
		Vector2 headingDirection = Vector2.FromAngle(Rotation);
		Vector2 neededDirection = averagePosition - Position;
		if (headingDirection.Cross(neededDirection) > 0) 
		{
			direction = 1;
		}
		else 
		{
			direction = -1;
		}
		return (float)(direction * CohesionTurnAmount * Math.Abs(headingDirection.Cross(neededDirection)) * delta);
	}
	
	private float GoalSeeking(double delta) 
	{
		int direction = 0;
		Vector2 headingDirection = Vector2.FromAngle(Rotation);
		Vector2 neededDirection = Goal - Position;
		if (headingDirection.Cross(neededDirection) > 0) 
		{
			direction = 1;
		}
		else 
		{
			direction = -1;
		}
		return (float)(direction * GoalSeekingTurnAmount * Math.Abs(headingDirection.Cross(neededDirection)) * delta);
	}
}
