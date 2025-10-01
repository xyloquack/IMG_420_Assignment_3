using Godot;
using System;

public partial class BoidTest : Node2D
{
	[Export]
	public int numBoids;
	[Export]
	public PackedScene BoidScene { get; set; }
	
	public float time_passed;
	public Godot.Collections.Array boids;
	
	override public void _Ready()
	{
		time_passed = 0;
		boids = [];
		for (int i = 0; i < numBoids; i++)
		{
			CharacterBody2D newBoid = BoidScene.Instantiate<CharacterBody2D>();
			Vector2 newPosition;
			newPosition.X = (float)GD.Randf() * 20;
			newPosition.Y = (float)GD.Randf() * 20;
			newBoid.Position = newPosition;
			newBoid.Rotation = (float)(GD.Randf() * 2 * Math.PI);
			boids.Add(newBoid);
			AddChild(newBoid);
		}
	}
	
	override public void _PhysicsProcess(double delta)
	{
		time_passed += (float)delta;
		if (time_passed >= 3)
		{
			time_passed -= 3;
			Vector2 newGoal;
			newGoal.X = (float)GD.Randf() * 200 - 100;
			newGoal.Y = (float)GD.Randf() * 500 - 250;
			GetNode<CollisionShape2D>("GoalLocation").Position = newGoal;
			foreach (Boid boid in boids)
			{
				boid.Goal = newGoal;
			}
		}
	}
}
