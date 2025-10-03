using Godot;
using System;
using System.Collections.Generic;

public partial class BoidManager : Node
{
	public List<Boid> Boids = [];
	public List<List<Boid>> SeparatingGrid = new List<List<Boid>>();
	public List<List<Boid>> LocalGrid = new List<List<Boid>>();
	
	override public void _PhysicsProcess(double delta) 
	{
		if (Boids.Count == 0)
		{
			return;
		}
		float MinX = Boids[0].GlobalPosition.X;
		float MinY = Boids[0].GlobalPosition.Y;
		float MaxX = Boids[0].GlobalPosition.X;
		float MaxY = Boids[0].GlobalPosition.Y;
		foreach (Boid boid in Boids)
		{
			MinX = Math.Min(boid.GlobalPosition.X, MinX);
			MinY = Math.Min(boid.GlobalPosition.Y, MinY);
			MaxX = Math.Max(boid.GlobalPosition.X, MaxX);
			MaxY = Math.Max(boid.GlobalPosition.Y, MaxY);
		}
		int separatingGridWidth = (int)Math.Ceiling((MaxX - MinX) / 20 + 1);
		int separatingGridHeight = (int)Math.Ceiling((MaxY - MinY) / 20 + 1);
		int separatingGridSize = separatingGridWidth * separatingGridHeight;
		if (SeparatingGrid.Count != separatingGridSize)
		{
			SeparatingGrid.Clear();
			for (int i = 0; i < separatingGridSize; i++)
			{
				SeparatingGrid.Add(new List<Boid>());
			}
		}
		else
		{
			for (int i = 0; i < separatingGridSize; i++)
			{
				SeparatingGrid[i].Clear(); 
			}
		}
		int localGridWidth = (int)Math.Ceiling((MaxX - MinX) / 50 + 1);
		int localGridHeight = (int)Math.Ceiling((MaxY - MinY) / 50 + 1);
		int localGridSize = localGridWidth * localGridHeight;
		if (LocalGrid.Count != localGridSize)
		{
			LocalGrid.Clear();
			for (int i = 0; i < localGridSize; i++)
			{
				LocalGrid.Add(new List<Boid>());
			}
		}
		else
		{
			for (int i = 0; i < localGridSize; i++)
			{
				LocalGrid[i].Clear(); 
			}
		}
		foreach (Boid boid in Boids)
		{
			int sep_cell_x = (int)Math.Floor((boid.GlobalPosition.X - MinX) / 20.0f);
			int sep_cell_y = (int)Math.Floor((boid.GlobalPosition.Y - MinY) / 20.0f);
			int separatingIndex = sep_cell_y * separatingGridWidth + sep_cell_x;
			
			int local_cell_x = (int)Math.Floor((boid.GlobalPosition.X - MinX) / 50.0f);
			int local_cell_y = (int)Math.Floor((boid.GlobalPosition.Y - MinY) / 50.0f);
			int localIndex = local_cell_y * localGridWidth + local_cell_x;
			SeparatingGrid[separatingIndex].Add(boid);
			LocalGrid[localIndex].Add(boid);
		}
		
		foreach (Boid boid in Boids)
		{
			int sep_cell_x = (int)Math.Floor((boid.GlobalPosition.X - MinX) / 20.0f);
			int sep_cell_y = (int)Math.Floor((boid.GlobalPosition.Y - MinY) / 20.0f);
			sep_cell_x = Mathf.Clamp(sep_cell_x, 0, separatingGridWidth - 1);
			sep_cell_y = Mathf.Clamp(sep_cell_y, 0, separatingGridHeight - 1);
			int separatingIndex = sep_cell_y * separatingGridWidth + sep_cell_x;
			
			int local_cell_x = (int)Math.Floor((boid.GlobalPosition.X - MinX) / 50.0f);
			int local_cell_y = (int)Math.Floor((boid.GlobalPosition.Y - MinY) / 50.0f);
			local_cell_x = Mathf.Clamp(local_cell_x, 0, localGridWidth - 1);
			local_cell_y = Mathf.Clamp(local_cell_y, 0, localGridHeight - 1);
			int localIndex = local_cell_y * localGridWidth + local_cell_x;
			Vector2 steeringVector = Vector2.Zero;
			steeringVector += SeparationGridParsing(SeparatingGrid, separatingGridWidth, separatingGridHeight, boid, separatingIndex);
			steeringVector += LocalGridParsing(LocalGrid, localGridWidth, localGridHeight, boid, localIndex);
			steeringVector += GoalSeeking(boid);
			if (steeringVector.Length() > boid.MaxSteeringForce)
			{
				steeringVector = steeringVector.Normalized() * boid.MaxSteeringForce;
			}
			Vector2 currentHeading = Vector2.FromAngle(boid.Rotation);
			float angleToSteering = currentHeading.AngleTo(steeringVector);
			boid.Rotation = (float)Mathf.Wrap(boid.Rotation + angleToSteering * boid.RotationStrength * delta, -Mathf.Pi, Mathf.Pi);
			Vector2 newVelocity = Vector2.FromAngle(boid.Rotation);
			
			newVelocity *= boid.Speed;
			boid.Velocity = newVelocity;
			boid.Position += boid.Velocity * (float)delta;
		}
	}
	
	private Vector2 SeparationGridParsing(List<List<Boid>> SeparatingGrid, int separatingGridWidth, int separatingGridHeight, Boid boid, int index)
	{
		Vector2 steeringVector = Vector2.Zero;
		bool left = index % separatingGridWidth == 0;
		bool right = index % separatingGridWidth == separatingGridWidth - 1;
		bool top = index - separatingGridWidth < 0;
		bool bottom = index + separatingGridWidth >= separatingGridWidth * separatingGridHeight;
		steeringVector += Separation(SeparatingGrid[index], boid);
		if (!top)
		{
			steeringVector += Separation(SeparatingGrid[index - separatingGridWidth], boid);
			if (!left)
			{
				steeringVector += Separation(SeparatingGrid[index - separatingGridWidth - 1], boid);
			}
			if (!right)
			{
				steeringVector += Separation(SeparatingGrid[index - separatingGridWidth + 1], boid);
			}
		}
		if (!left)
		{
			steeringVector += Separation(SeparatingGrid[index - 1], boid);
		}
		if (!right)
		{
			steeringVector += Separation(SeparatingGrid[index + 1], boid);
		}
		if (!bottom)
		{
			steeringVector += Separation(SeparatingGrid[index + separatingGridWidth], boid);
			if (!left)
			{
				steeringVector += Separation(SeparatingGrid[index + separatingGridWidth - 1], boid);
			}
			if (!right)
			{
				steeringVector += Separation(SeparatingGrid[index + separatingGridWidth + 1], boid);
			}
		}
		return steeringVector * boid.SeparationTurnAmount;
	}
	
	private Vector2 LocalGridParsing(List<List<Boid>> LocalGrid, int localGridWidth, int localGridHeight, Boid boid, int index)
	{
		Vector2 alignmentSteeringVector = Vector2.Zero;
		Vector2 cohesionSteeringVector = Vector2.Zero;
		bool left = index % localGridWidth == 0;
		bool right = index % localGridWidth == localGridWidth - 1;
		bool top = index - localGridWidth < 0;
		bool bottom = index + localGridWidth >= localGridWidth * localGridHeight;
		alignmentSteeringVector += Alignment(LocalGrid[index], boid);
		cohesionSteeringVector += Cohesion(LocalGrid[index], boid);
		if (!top)
		{
			alignmentSteeringVector += Alignment(LocalGrid[index - localGridWidth], boid);
			cohesionSteeringVector += Cohesion(LocalGrid[index - localGridWidth], boid);
			if (!left)
			{
				alignmentSteeringVector += Alignment(LocalGrid[index - localGridWidth - 1], boid);
				cohesionSteeringVector += Cohesion(LocalGrid[index - localGridWidth - 1], boid);
			}
			if (!right)
			{
				alignmentSteeringVector += Alignment(LocalGrid[index - localGridWidth + 1], boid);
				cohesionSteeringVector += Cohesion(LocalGrid[index - localGridWidth + 1], boid);
			}
		}
		if (!left)
		{
			alignmentSteeringVector += Alignment(LocalGrid[index - 1], boid);
			cohesionSteeringVector += Cohesion(LocalGrid[index - 1], boid);
		}
		if (!right)
		{
			alignmentSteeringVector += Alignment(LocalGrid[index + 1], boid);
			cohesionSteeringVector += Cohesion(LocalGrid[index + 1], boid);
		}
		if (!bottom)
		{
			alignmentSteeringVector += Alignment(LocalGrid[index + localGridWidth], boid);
			cohesionSteeringVector += Cohesion(LocalGrid[index + localGridWidth], boid);
			if (!left)
			{
				alignmentSteeringVector += Alignment(LocalGrid[index + localGridWidth - 1], boid);
				cohesionSteeringVector += Cohesion(LocalGrid[index + localGridWidth - 1], boid);
			}
			if (!right)
			{
				alignmentSteeringVector += Alignment(LocalGrid[index + localGridWidth + 1], boid);
				cohesionSteeringVector += Cohesion(LocalGrid[index + localGridWidth + 1], boid);
			}
		}
		return alignmentSteeringVector.Normalized() * boid.AlignmentTurnAmount + cohesionSteeringVector.Normalized() * boid.CohesionTurnAmount;
	}
	
	private Vector2 Separation(List<Boid> gridCell, Boid boid) 
	{
		if (gridCell.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 repulseVector = Vector2.Zero;
		foreach (Boid currentBoid in gridCell)
		{
			if (boid.GlobalPosition.DistanceTo(currentBoid.GlobalPosition) < 20 && boid != currentBoid)
			{
				repulseVector += (boid.GlobalPosition - currentBoid.GlobalPosition) * (float)(1.0 / ((currentBoid.GlobalPosition - boid.GlobalPosition).LengthSquared() + 0.0001));
			}
		}
		return repulseVector;
	}
	
	private Vector2 Alignment(List<Boid> gridCell, Boid boid) 
	{
		if (gridCell.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 averageVelocity = Vector2.Zero;
		foreach (Boid currentBoid in gridCell) 
		{
			if (boid.GlobalPosition.DistanceTo(currentBoid.GlobalPosition) < 50 && boid != currentBoid)
			{
				averageVelocity += currentBoid.Velocity.Normalized();
			}
		}
		return averageVelocity;
	}
	
	private Vector2 Cohesion(List<Boid> gridCell, Boid boid) 
	{
		if (gridCell.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 averagePosition = Vector2.Zero;
		foreach (Boid currentBoid in gridCell) 
		{
			if (boid.GlobalPosition.DistanceTo(currentBoid.GlobalPosition) < 50 && boid != currentBoid)
			{
				averagePosition += currentBoid.GlobalPosition;
			}
		}
		Vector2 neededDirection = (averagePosition - boid.GlobalPosition).Normalized();
		return neededDirection;
	}
	
	private Vector2 GoalSeeking(Boid boid) 
	{
		Vector2 neededDirection = (boid.Goal - boid.GlobalPosition).Normalized();
		return boid.GoalSeekingTurnAmount * neededDirection;
	}
}
