using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class BoidManager : Node
{
	public const int MAX_BOIDS = 10000;
	public const uint MAX_BOID_BYTES = (uint)(MAX_BOIDS * 8);
	public const uint MAX_GRID_CELLS = 5000;
	
	public List<Boid> Boids = [];
	public List<List<Boid>> SeparatingGrid = new List<List<Boid>>();
	public List<List<Boid>> LocalGrid = new List<List<Boid>>();
	List<Boid> BoidsToCheck = new List<Boid>();
	
	public float SeparatingDistance = 20;
	public float LocalDistance = 50;
	
	private RenderingDevice _rd;
	private Rid shader;
	private Rid pipeline;
	private Rid _turningVectorsBuffer;
	private Rid _boidPositionsBuffer;
	private Rid _boidVelocitiesBuffer;
	private Rid _boidGoalsBuffer;
	private Rid _numBoidBuffer;
	private Rid _boidGridIndicesBuffer;
	private Rid _separatingGridBuffer;
	private Rid _separatingGridInfoBuffer;
	private Rid _localGridBuffer;
	private Rid _localGridInfoBuffer;
	private Rid _turningWeightsBuffer;
	
	private Rid _boidInfoUniformSet;
	private Rid _separatingGridUniformSet;
	private Rid _localGridUniformSet;
	private Rid _turningWeightsUniformSet;
	
	override public void _Ready()
	{
		SetupComputeShader();
	}
	
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
		uint separatingGridWidth = (uint)Math.Ceiling((MaxX - MinX) / 20) + 1;
		uint separatingGridHeight = (uint)Math.Ceiling((MaxY - MinY) / 20) + 1;
		uint separatingGridSize = separatingGridWidth * separatingGridHeight;
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
		uint localGridWidth = (uint)Math.Ceiling((MaxX - MinX) / 50) + 1;
		uint localGridHeight = (uint)Math.Ceiling((MaxY - MinY) / 50) + 1;
		uint localGridSize = localGridWidth * localGridHeight;
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
			uint sep_cell_x = (uint)((boid.GlobalPosition.X - MinX) / 20.0f);
			uint sep_cell_y = (uint)((boid.GlobalPosition.Y - MinY) / 20.0f);
			int separatingIndex = (int)(sep_cell_y * separatingGridWidth + sep_cell_x);
			
			uint local_cell_x = (uint)((boid.GlobalPosition.X - MinX) / 50.0f);
			uint local_cell_y = (uint)((boid.GlobalPosition.Y - MinY) / 50.0f);
			int localIndex = (int)(local_cell_y * localGridWidth + local_cell_x);
			SeparatingGrid[separatingIndex].Add(boid);
			LocalGrid[localIndex].Add(boid);
		}
		
		//foreach (Boid boid in Boids)
		//{
			//int sep_cell_x = (int)((boid.GlobalPosition.X - MinX) / 20.0f);
			//int sep_cell_y = (int)((boid.GlobalPosition.Y - MinY) / 20.0f);
			//int separatingIndex = sep_cell_y * separatingGridWidth + sep_cell_x;
			//
			//int local_cell_x = (int)((boid.GlobalPosition.X - MinX) / 50.0f);
			//int local_cell_y = (int)((boid.GlobalPosition.Y - MinY) / 50.0f);
			//int localIndex = local_cell_y * localGridWidth + local_cell_x;
			//Vector2 steeringVector = Vector2.Zero;
			//steeringVector += SeparationGridParsing(SeparatingGrid, separatingGridWidth, separatingGridHeight, boid, separatingIndex);
			//steeringVector += LocalGridParsing(LocalGrid, localGridWidth, localGridHeight, boid, localIndex);
			//steeringVector += GoalSeeking(boid);
			//if (steeringVector.LengthSquared() > boid.MaxSteeringForce * boid.MaxSteeringForce)
			//{
				//steeringVector = steeringVector.Normalized() * boid.MaxSteeringForce;
			//}
			//Vector2 currentHeading = Vector2.FromAngle(boid.Rotation);
			//float angleToSteering = currentHeading.AngleTo(steeringVector);
			//boid.Rotation = (float)Mathf.Wrap(boid.Rotation + angleToSteering * boid.RotationStrength * delta, -Mathf.Pi, Mathf.Pi);
			//Vector2 newVelocity = Vector2.FromAngle(boid.Rotation);
			//
			//newVelocity *= boid.Speed;
			//boid.Velocity = newVelocity;
			//boid.Position += boid.Velocity * (float)delta;
		//}
		Vector2 adjustmentVector = new Vector2(MinX, MinY);
		
		byte[] turningVectors = new byte[8 * Boids.Count];
		
		_rd.BufferUpdate(_turningVectorsBuffer, 0, (uint)turningVectors.Length, turningVectors);
		
		
		byte[] boidPositions = new byte[8 * Boids.Count];
		for (int i = 0; i < Boids.Count; i++)
		{
			int offset = i * 8;
			Buffer.BlockCopy(BitConverter.GetBytes((float)(Boids[i].GlobalPosition.X - adjustmentVector.X)), 0, boidPositions, offset, 4);
			Buffer.BlockCopy(BitConverter.GetBytes((float)(Boids[i].GlobalPosition.Y - adjustmentVector.Y)), 0, boidPositions, offset + 4, 4);
		}
		
		_rd.BufferUpdate(_boidPositionsBuffer, 0, (uint)boidPositions.Length, boidPositions);
		
		byte[] boidVelocities = new byte[8 * Boids.Count];
		for (int i = 0; i < Boids.Count; i++)
		{
			int offset = i * 8;
			Buffer.BlockCopy(BitConverter.GetBytes((float)Boids[i].Velocity.X), 0, boidVelocities, offset, 4);
			Buffer.BlockCopy(BitConverter.GetBytes((float)Boids[i].Velocity.Y), 0, boidVelocities, offset + 4, 4);
		}
		
		byte[] boidGoals = new byte[8 * Boids.Count];
		for (int i = 0; i < Boids.Count; i++)
		{
			int offset = i * 8;
			Buffer.BlockCopy(BitConverter.GetBytes((float)(Boids[i].Goal.X - adjustmentVector.X)), 0, boidGoals, offset, 4);
			Buffer.BlockCopy(BitConverter.GetBytes((float)(Boids[i].Goal.Y - adjustmentVector.Y)), 0, boidGoals, offset + 4, 4);
		}
		
		_rd.BufferUpdate(_boidGoalsBuffer, 0, (uint)boidGoals.Length, boidGoals);
		
		byte[] numBoidArray = new byte[sizeof(uint)];
		Buffer.BlockCopy(BitConverter.GetBytes((uint)(Boids.Count)), 0, numBoidArray, 0, 4);
		
		_rd.BufferUpdate(_numBoidBuffer, 0, (uint)numBoidArray.Length, numBoidArray);
		
		byte[] boidGridIndices = new byte[8 * Boids.Count];
		for (int i = 0; i < Boids.Count; i++)
		{
			Boid boid = Boids[i];
			uint sep_cell_x = (uint)((boid.GlobalPosition.X - MinX) / 20.0f);
			uint sep_cell_y = (uint)((boid.GlobalPosition.Y - MinY) / 20.0f);
			uint separatingIndex = (sep_cell_y * separatingGridWidth + sep_cell_x);

			uint local_cell_x = (uint)((boid.GlobalPosition.X - MinX) / 50.0f);
			uint local_cell_y = (uint)((boid.GlobalPosition.Y - MinY) / 50.0f);
			uint localIndex = (local_cell_y * localGridWidth + local_cell_x);
			
			int offset = i * 8;
			Buffer.BlockCopy(BitConverter.GetBytes(separatingIndex), 0, boidGridIndices, offset, 4);
			Buffer.BlockCopy(BitConverter.GetBytes(localIndex), 0, boidGridIndices, offset + 4, 4);
		}
		_rd.BufferUpdate(_boidGridIndicesBuffer, 0, (uint)boidGridIndices.Length, boidGridIndices);
		
		List<Vector2I> separatingGridData = new List<Vector2I>();
		uint currentOffset = 0;
		
		byte[] packedSeparatingGrid = new byte[8 * Boids.Count];
		for (int i = 0; i < SeparatingGrid.Count; i++)
		{
			separatingGridData.Add(new Vector2I((int)currentOffset, (int)SeparatingGrid[i].Count));
			foreach (Boid boid in SeparatingGrid[i])
			{
				int offset = (int)currentOffset * 8;
				Buffer.BlockCopy(BitConverter.GetBytes((float)(boid.GlobalPosition.X - adjustmentVector.X)), 0, packedSeparatingGrid, offset, 4);
				Buffer.BlockCopy(BitConverter.GetBytes((float)(boid.GlobalPosition.Y - adjustmentVector.Y)), 0, packedSeparatingGrid, offset + 4, 4);
				currentOffset++;
			}
		}
		
		_rd.BufferUpdate(_separatingGridBuffer, 0, (uint)packedSeparatingGrid.Length, packedSeparatingGrid);
		
		byte[] packedSeparatingGridInfo = new byte[16 + separatingGridData.Count * 8];
		Buffer.BlockCopy(BitConverter.GetBytes(separatingGridWidth), 0, packedSeparatingGridInfo, 0, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(separatingGridHeight), 0, packedSeparatingGridInfo, 4, 4);
		Buffer.BlockCopy(BitConverter.GetBytes((float)(SeparatingDistance * SeparatingDistance)), 0, packedSeparatingGridInfo, 8, 4);
		for (int i = 0; i < separatingGridData.Count; i++)
		{
			int offset = i * 8 + 16;
			Buffer.BlockCopy(BitConverter.GetBytes((uint)(separatingGridData[i][0])), 0, packedSeparatingGridInfo, offset, 4);
			Buffer.BlockCopy(BitConverter.GetBytes((uint)(separatingGridData[i][1])), 0, packedSeparatingGridInfo, offset + 4, 4);
		}
		
		_rd.BufferUpdate(_separatingGridInfoBuffer, 0, (uint)packedSeparatingGridInfo.Length, packedSeparatingGridInfo);
		
		List<Vector2I> localGridData = new List<Vector2I>();
		currentOffset = 0;
		
		byte[] packedLocalGrid = new byte[16 * Boids.Count];
		for (int i = 0; i < LocalGrid.Count; i++)
		{
			localGridData.Add(new Vector2I((int)currentOffset, (int)LocalGrid[i].Count));
			foreach (Boid boid in LocalGrid[i])
			{
				int offset = (int)currentOffset * 16;
				Buffer.BlockCopy(BitConverter.GetBytes((float)(boid.GlobalPosition.X - adjustmentVector.X)), 0, packedLocalGrid, offset, 4);
				Buffer.BlockCopy(BitConverter.GetBytes((float)(boid.GlobalPosition.Y - adjustmentVector.Y)), 0, packedLocalGrid, offset + 4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes((float)boid.Velocity.X), 0, packedLocalGrid, offset + 8, 4);
				Buffer.BlockCopy(BitConverter.GetBytes((float)boid.Velocity.Y), 0, packedLocalGrid, offset + 12, 4);
				currentOffset++;
			}
		}
		
		_rd.BufferUpdate(_localGridBuffer, 0, (uint)packedLocalGrid.Length, packedLocalGrid);
		
		byte[] packedLocalGridInfo = new byte[16 + localGridData.Count * 8];
		Buffer.BlockCopy(BitConverter.GetBytes(localGridWidth), 0, packedLocalGridInfo, 0, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(localGridHeight), 0, packedLocalGridInfo, 4, 4);
		Buffer.BlockCopy(BitConverter.GetBytes((float)(LocalDistance * LocalDistance)), 0, packedLocalGridInfo, 8, 4);
		for (int i = 0; i < localGridData.Count; i++)
		{
			int offset = i * 8 + 16;
			Buffer.BlockCopy(BitConverter.GetBytes((uint)(localGridData[i][0])), 0, packedLocalGridInfo, offset, 4);
			Buffer.BlockCopy(BitConverter.GetBytes((uint)(localGridData[i][1])), 0, packedLocalGridInfo, offset + 4, 4);
		}
		
		_rd.BufferUpdate(_localGridInfoBuffer, 0, (uint)packedLocalGridInfo.Length, packedLocalGridInfo);
		
		byte[] packedTurningWeights = new byte[sizeof(float) * (4 + Boids.Count)];
		Buffer.BlockCopy(BitConverter.GetBytes((float)Boids[0].SeparationTurnAmount), 0, packedTurningWeights, 0, 4);
		Buffer.BlockCopy(BitConverter.GetBytes((float)Boids[0].AlignmentTurnAmount), 0, packedTurningWeights, 4, 4);
		Buffer.BlockCopy(BitConverter.GetBytes((float)Boids[0].CohesionTurnAmount), 0, packedTurningWeights, 8, 4);
		for (int i = 0; i < Boids.Count; i++)
		{
			int offset = i * 4 + 16;
			Buffer.BlockCopy(BitConverter.GetBytes((float)Boids[i].GoalSeekingTurnAmount), 0, packedTurningWeights, offset, 4);
		}
		
		_rd.BufferUpdate(_turningWeightsBuffer, 0, (uint)packedTurningWeights.Length, packedTurningWeights);
		
		uint localSizeX = 64;
		uint numWorkgroupsX = (uint)Math.Ceiling((double)Boids.Count / localSizeX);
		
		long computeList = _rd.ComputeListBegin();
		_rd.ComputeListBindComputePipeline(computeList, pipeline);
		_rd.ComputeListBindUniformSet(computeList, _boidInfoUniformSet, 0);
		_rd.ComputeListBindUniformSet(computeList, _separatingGridUniformSet, 1);
		_rd.ComputeListBindUniformSet(computeList, _localGridUniformSet, 2);
		_rd.ComputeListBindUniformSet(computeList, _turningWeightsUniformSet, 3);
		_rd.ComputeListDispatch(computeList, numWorkgroupsX, 1, 1);
		_rd.ComputeListEnd();

		_rd.Submit();
		_rd.Sync();

		byte[] packedTurningVectorOutput = _rd.BufferGetData(_turningVectorsBuffer);
		List<Vector2> turningVectorOutput = [];
		for (int i = 0; i < Boids.Count; i++)
		{
			int offset = i * 8;
			float xPos = BitConverter.ToSingle(packedTurningVectorOutput, offset);
			float yPos = BitConverter.ToSingle(packedTurningVectorOutput, offset + 4);
			turningVectorOutput.Add(new Vector2(xPos, yPos));
		}
		for (int i = 0; i < turningVectorOutput.Count; i++)
		{
			if (turningVectorOutput[i].LengthSquared() > Boids[i].MaxSteeringForce * Boids[i].MaxSteeringForce)
			{
				turningVectorOutput[i] = turningVectorOutput[i].Normalized() * Boids[i].MaxSteeringForce;
			}
			Vector2 currentHeading = Vector2.FromAngle(Boids[i].Rotation);
			float angleToSteering = currentHeading.AngleTo(turningVectorOutput[i]);
			Boids[i].Rotation = (float)Mathf.Wrap(Boids[i].Rotation + angleToSteering * Boids[i].RotationStrength * delta, -Mathf.Pi, Mathf.Pi);
			Vector2 newVelocity = Vector2.FromAngle(Boids[i].Rotation);
			
			newVelocity *= Boids[i].Speed;
			Boids[i].Velocity = newVelocity;
			Boids[i].Position += Boids[i].Velocity * (float)delta;
		}
	}
	
	//private Vector2 SeparationGridParsing(List<List<Boid>> SeparatingGrid, int separatingGridWidth, int separatingGridHeight, Boid boid, int index)
	//{
		//BoidsToCheck.Clear();
		//bool left = index % separatingGridWidth == 0;
		//bool right = index % separatingGridWidth == separatingGridWidth - 1;
		//bool top = index - separatingGridWidth < 0;
		//bool bottom = index + separatingGridWidth >= separatingGridWidth * separatingGridHeight;
		//BoidsToCheck.AddRange(SeparatingGrid[index]);
		//if (!top)
		//{
			//BoidsToCheck.AddRange(SeparatingGrid[index - separatingGridWidth]);
			//if (!left)
			//{
				//BoidsToCheck.AddRange(SeparatingGrid[index - separatingGridWidth - 1]);
			//}
			//if (!right)
			//{
				//BoidsToCheck.AddRange(SeparatingGrid[index - separatingGridWidth + 1]);
			//}
		//}
		//if (!left)
		//{
			//BoidsToCheck.AddRange(SeparatingGrid[index - 1]);
		//}
		//if (!right)
		//{
			//BoidsToCheck.AddRange(SeparatingGrid[index + 1]);
		//}
		//if (!bottom)
		//{
			//BoidsToCheck.AddRange(SeparatingGrid[index + separatingGridWidth]);
			//if (!left)
			//{
				//BoidsToCheck.AddRange(SeparatingGrid[index + separatingGridWidth - 1]);
			//}
			//if (!right)
			//{
				//BoidsToCheck.AddRange(SeparatingGrid[index + separatingGridWidth + 1]);
			//}
		//}
		//return Separation(BoidsToCheck, boid) * boid.SeparationTurnAmount;
	//}
	//
	//private Vector2 LocalGridParsing(List<List<Boid>> LocalGrid, int localGridWidth, int localGridHeight, Boid boid, int index)
	//{
		//BoidsToCheck.Clear();
		//bool left = index % localGridWidth == 0;
		//bool right = index % localGridWidth == localGridWidth - 1;
		//bool top = index - localGridWidth < 0;
		//bool bottom = index + localGridWidth >= localGridWidth * localGridHeight;
		//BoidsToCheck.AddRange(LocalGrid[index]);
		//if (!top)
		//{
			//BoidsToCheck.AddRange(LocalGrid[index - localGridWidth]);
			//if (!left)
			//{
				//BoidsToCheck.AddRange(LocalGrid[index - localGridWidth - 1]);
			//}
			//if (!right)
			//{
				//BoidsToCheck.AddRange(LocalGrid[index - localGridWidth + 1]);
			//}
		//}
		//if (!left)
		//{
			//BoidsToCheck.AddRange(LocalGrid[index - 1]);
		//}
		//if (!right)
		//{
			//BoidsToCheck.AddRange(LocalGrid[index + 1]);
		//}
		//if (!bottom)
		//{
			//BoidsToCheck.AddRange(LocalGrid[index + localGridWidth]);
			//if (!left)
			//{
				//BoidsToCheck.AddRange(LocalGrid[index + localGridWidth - 1]);
			//}
			//if (!right)
			//{
				//BoidsToCheck.AddRange(LocalGrid[index + localGridWidth + 1]);
			//}
		//}
		//return Alignment(BoidsToCheck, boid).Normalized() * boid.AlignmentTurnAmount + Cohesion(BoidsToCheck, boid).Normalized() * boid.CohesionTurnAmount;
	//}
	//
	//private Vector2 Separation(List<Boid> gridCell, Boid boid) 
	//{
		//if (gridCell.Count == 0)
		//{
			//return Vector2.Zero;
		//}
		//Vector2 repulseVector = Vector2.Zero;
		//foreach (Boid currentBoid in gridCell)
		//{
			//if (boid.GlobalPosition.DistanceSquaredTo(currentBoid.GlobalPosition) < 400 && boid != currentBoid)
			//{
				//repulseVector += (boid.GlobalPosition - currentBoid.GlobalPosition) * (float)(1.0 / ((currentBoid.GlobalPosition - boid.GlobalPosition).LengthSquared() + 0.0001));
			//}
		//}
		//return repulseVector;
	//}
	//
	//private Vector2 Alignment(List<Boid> gridCell, Boid boid) 
	//{
		//if (gridCell.Count == 0)
		//{
			//return Vector2.Zero;
		//}
		//Vector2 averageVelocity = Vector2.Zero;
		//foreach (Boid currentBoid in gridCell) 
		//{
			//if (boid.GlobalPosition.DistanceSquaredTo(currentBoid.GlobalPosition) < 2500 && boid != currentBoid)
			//{
				//averageVelocity += currentBoid.Velocity;
			//}
		//}
		//return averageVelocity;
	//}
	//
	//private Vector2 Cohesion(List<Boid> gridCell, Boid boid) 
	//{
		//if (gridCell.Count == 0)
		//{
			//return Vector2.Zero;
		//}
		//Vector2 averagePosition = Vector2.Zero;
		//foreach (Boid currentBoid in gridCell) 
		//{
			//if (boid.GlobalPosition.DistanceSquaredTo(currentBoid.GlobalPosition) < 2500 && boid != currentBoid)
			//{
				//averagePosition += currentBoid.GlobalPosition;
			//}
		//}
		//Vector2 neededDirection = (averagePosition - boid.GlobalPosition);
		//return neededDirection;
	//}
	//
	//private Vector2 GoalSeeking(Boid boid) 
	//{
		//Vector2 neededDirection = (boid.Goal - boid.GlobalPosition).Normalized();
		//return boid.GoalSeekingTurnAmount * neededDirection;
	//}
//}

	private void SetupComputeShader()
	{
		_rd = RenderingServer.CreateLocalRenderingDevice();

		// 1. Load the GLSL shader
		RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://scripts/boids.glsl");
		RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
		shader = _rd.ShaderCreateFromSpirV(shaderBytecode);
		pipeline = _rd.ComputePipelineCreate(shader);

		_turningVectorsBuffer = _rd.StorageBufferCreate((uint)(MAX_BOID_BYTES));
		_boidPositionsBuffer = _rd.StorageBufferCreate((uint)(MAX_BOID_BYTES));
		_boidVelocitiesBuffer = _rd.StorageBufferCreate((uint)(MAX_BOID_BYTES));
		_boidGoalsBuffer = _rd.StorageBufferCreate((uint)(MAX_BOID_BYTES));
		_numBoidBuffer = _rd.StorageBufferCreate((uint)sizeof(uint));
		_boidGridIndicesBuffer = _rd.StorageBufferCreate((uint)(MAX_BOIDS * 8));
		_separatingGridBuffer = _rd.StorageBufferCreate((uint)(MAX_BOID_BYTES * 2));
		_separatingGridInfoBuffer = _rd.StorageBufferCreate((uint)(sizeof(uint) * 2 + sizeof(float) + MAX_GRID_CELLS * 8));
		_localGridBuffer = _rd.StorageBufferCreate((uint)(MAX_BOID_BYTES * 2));
		_localGridInfoBuffer = _rd.StorageBufferCreate((uint)(sizeof(uint) * 2 + sizeof(float) + MAX_GRID_CELLS * 8));
		_turningWeightsBuffer = _rd.StorageBufferCreate((uint)(sizeof(float) * 4 + MAX_BOID_BYTES));

		RDUniform turningVectorsUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0
		};
		turningVectorsUniform.AddId(_turningVectorsBuffer);
		
		RDUniform boidPositionsUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1
		};
		boidPositionsUniform.AddId(_boidPositionsBuffer);
		
		RDUniform boidVelocitiesUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 2
		};
		boidVelocitiesUniform.AddId(_boidVelocitiesBuffer);
		
		RDUniform boidGoalsUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 3
		};
		boidGoalsUniform.AddId(_boidGoalsBuffer);
		
		RDUniform numBoidUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 4
		};
		numBoidUniform.AddId(_numBoidBuffer);
		
		RDUniform boidGridIndicesUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 5
		};
		boidGridIndicesUniform.AddId(_boidGridIndicesBuffer);
		
		_boidInfoUniformSet = _rd.UniformSetCreate(new Godot.Collections.Array<RDUniform> { turningVectorsUniform, boidPositionsUniform, boidVelocitiesUniform, boidGoalsUniform, numBoidUniform, boidGridIndicesUniform }, shader, 0);
		
		RDUniform separatingGridUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0
		};
		separatingGridUniform.AddId(_separatingGridBuffer);
		
		RDUniform separatingGridInfoUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1
		};
		separatingGridInfoUniform.AddId(_separatingGridInfoBuffer);
		
		_separatingGridUniformSet = _rd.UniformSetCreate(new Godot.Collections.Array<RDUniform> { separatingGridUniform, separatingGridInfoUniform }, shader, 1);
		
		RDUniform localGridUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0
		};
		localGridUniform.AddId(_localGridBuffer);
		
		RDUniform localGridInfoUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1
		};
		localGridInfoUniform.AddId(_localGridInfoBuffer);
		
		_localGridUniformSet = _rd.UniformSetCreate(new Godot.Collections.Array<RDUniform> { localGridUniform, localGridInfoUniform }, shader, 2);
		
		RDUniform turningWeightsUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0
		};
		turningWeightsUniform.AddId(_turningWeightsBuffer);
		
		_turningWeightsUniformSet = _rd.UniformSetCreate(new Godot.Collections.Array<RDUniform> { turningWeightsUniform }, shader, 3);
	}
	
	override public void _ExitTree()
	{
		if (_rd != null)
		{
			if (_turningVectorsBuffer.IsValid)
			{
				_rd.FreeRid(_turningVectorsBuffer);
			}
			if (_boidPositionsBuffer.IsValid)
			{
				_rd.FreeRid(_boidPositionsBuffer);
			}
			if (_boidVelocitiesBuffer.IsValid)
			{
				_rd.FreeRid(_boidVelocitiesBuffer);
			}
			if (_boidGoalsBuffer.IsValid)
			{
				_rd.FreeRid(_boidGoalsBuffer);
			}
			if (_numBoidBuffer.IsValid)
			{
				_rd.FreeRid(_numBoidBuffer);
			}
			if (_separatingGridBuffer.IsValid)
			{
				_rd.FreeRid(_separatingGridBuffer);
			}
			if (_separatingGridInfoBuffer.IsValid)
			{
				_rd.FreeRid(_separatingGridInfoBuffer);
			}
			if (_localGridBuffer.IsValid)
			{
				_rd.FreeRid(_localGridBuffer);
			}
			if (_localGridInfoBuffer.IsValid)
			{
				_rd.FreeRid(_localGridInfoBuffer);
			}
			if (_turningWeightsBuffer.IsValid)
			{
				_rd.FreeRid(_turningWeightsBuffer);
			}
			if (_boidInfoUniformSet.IsValid)
			{
				_rd.FreeRid(_boidInfoUniformSet);
			}
			if (_separatingGridUniformSet.IsValid)
			{
				_rd.FreeRid(_separatingGridUniformSet);
			}
			if (_localGridUniformSet.IsValid)
			{
				_rd.FreeRid(_localGridUniformSet);
			}
			if (_turningWeightsUniformSet.IsValid)
			{
				_rd.FreeRid(_turningWeightsUniformSet);
			}
		}
	}
}
