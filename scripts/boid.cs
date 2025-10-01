using Godot;
using System;

public partial class Boid : CharacterBody2D
{
	[Export]
	public float separationRadius;
	[Export]
	public float visualRadius;
	[Export]
	public float separationTurnAmount;
	[Export]
	public float alignmentTurnAmount;
	[Export]
	public float cohesionTurnAmount;
	
	public Godot.Collections.Array boids;
	
	override public void _Ready() {
		boids = [];
	}
	
	override public void _PhysicsProcess(double delta) {
		Godot.Collections.Array separatingBoids = [];
		Godot.Collections.Array localBoids = [];
		CategorizeLocalBoids(separatingBoids, localBoids);
		Separation(separatingBoids, delta);
		Alignment(localBoids, delta);
		Cohesion(localBoids, delta);
		Rotation = (float) (Rotation % (2 * Math.PI));
	}
	
	private void CategorizeLocalBoids(Godot.Collections.Array separatingBoids, Godot.Collections.Array localBoids) {
		foreach (CharacterBody2D boid in boids) {
			if (boid.Position.DistanceTo(Position) <= separationRadius) {
				separatingBoids.Add(boid);
			}
			if (boid.Position.DistanceTo(Position) <= visualRadius) {
				localBoids.Add(boid);
			}
		}
	}
	
	private void Separation(Godot.Collections.Array separatingBoids, double delta) {
		Vector2 averagePosition = new Vector2((float)0.0, (float)0.0);
		float numBoidsScalar = (float)(1.0 / separatingBoids.Count);
		foreach (CharacterBody2D boid in separatingBoids) {
			averagePosition += boid.Position * numBoidsScalar;
		}
		int direction = 0;
		if (Position.Cross(averagePosition) > 0) {
			direction = -1;
		}
		else {
			direction = 1;
		}
		Rotation += (float)(direction * separationTurnAmount * delta);
	}
	private void Alignment(Godot.Collections.Array localBoids, double delta) {
		float averageRotation = 0;
		float numBoidsScalar = (float)(1.0 / localBoids.Count);
		foreach (CharacterBody2D boid in localBoids) {
			averageRotation += boid.Rotation * numBoidsScalar;
		}
		int direction = 0;
		if (Math.Abs(averageRotation - Rotation) > Math.PI) {
			direction = -1;
		}
		else {
			direction = 1;
		}
		Rotation += (float)(direction * cohesionTurnAmount * delta);
	}
	private void Cohesion(Godot.Collections.Array localBoids, double delta) {
		Vector2 averagePosition = new Vector2((float)0.0, (float)0.0);
		float numBoidsScalar = (float)(1.0 / localBoids.Count);
		foreach (CharacterBody2D boid in localBoids) {
			averagePosition += boid.Position * numBoidsScalar;
		}
		int direction = 0;
		if (Position.Cross(averagePosition) > 0) {
			direction = 1;
		}
		else {
			direction = -1;
		}
		Rotation += (float)(direction * cohesionTurnAmount * delta);
	}
}
