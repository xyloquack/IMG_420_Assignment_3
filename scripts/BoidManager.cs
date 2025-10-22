using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class BoidManager : Node
{
	public const int MAX_BOIDS = 10000;
	public const uint MAX_BOID_BYTES = (uint)(MAX_BOIDS * 8);
	public const uint MAX_GRID_CELLS = 5000;
	
	public List<Boid> Boids = [];
	public List<List<Boid>> LocalGrid = new List<List<Boid>>();
	List<Boid> BoidsToCheck = new List<Boid>();
	
	public float SeparatingDistance = 20;
	public float LocalDistance = 50;
	public float SeparatingDistanceSquared;
	public float LocalDistanceSquared;
	
	private RenderingDevice _rd;
	private Rid shader;
	private Rid pipeline;
	private Rid _turningVectorsBuffer;
	private Rid _boidPositionsBuffer;
	private Rid _boidVelocitiesBuffer;
	private Rid _boidGoalsBuffer;
	private Rid _numBoidBuffer;
	private Rid _boidGridIndicesBuffer;
	private Rid _separatingGridInfoBuffer;
	private Rid _localGridBuffer;
	private Rid _localGridInfoBuffer;
	private Rid _turningWeightsBuffer;
	
	private Rid _boidInfoUniformSet;
	private Rid _separatingGridUniformSet;
	private Rid _localGridUniformSet;
	private Rid _turningWeightsUniformSet;
	
	private byte[] _turningVectors;
	private byte[] _boidPositions;
	private byte[] _boidVelocities;
	private byte[] _boidGoals;
	private byte[] _boidGridIndices;
	private byte[] _packedTurningWeights;
	private byte[] _numBoidArray;
	private byte[] _packedSeparatingGridInfo;
	private byte[] _packedLocalGrid;
	private byte[] _packedLocalGridInfo;
	private byte[] _packedTurningVectorOutput;
	
	private List<Vector2> _boidPositionsList = new();
	private List<Vector2> _boidVelocitiesList = new();
	private List<Vector2> _boidGoalsList = new();
	private List<uint> _boidIndicesList = new();
	private List<float> _turningWeightsList = new();
	private List<Vector2I> _localGridDataList = new();
	private List<Vector4> _localGridList = new();
	
	override public void _Ready()
	{
		SetupComputeShader();
		_turningVectors = new byte[MAX_BOID_BYTES];
		_boidPositions = new byte[MAX_BOID_BYTES];
		_boidVelocities = new byte[MAX_BOID_BYTES];
		_boidGoals = new byte[MAX_BOID_BYTES];
		_boidGridIndices = new byte[MAX_BOID_BYTES];
		_packedTurningWeights = new byte[sizeof(float) * (4 + MAX_BOIDS)];
		_numBoidArray = new byte[sizeof(uint)];
		_packedSeparatingGridInfo = new byte[4];
		_packedLocalGrid = new byte[16 * MAX_BOIDS];
		_packedLocalGridInfo = new byte[sizeof(uint) * 2 + sizeof(float) + MAX_GRID_CELLS * 8];
		_packedTurningVectorOutput = _rd.BufferGetData(_turningVectorsBuffer);
		SeparatingDistanceSquared = (float)Math.Pow(SeparatingDistance, 2);
		LocalDistanceSquared = (float)Math.Pow(LocalDistance, 2);
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
		uint localGridWidth = (uint)Math.Ceiling((MaxX - MinX) / LocalDistance) + 1;
		uint localGridHeight = (uint)Math.Ceiling((MaxY - MinY) / LocalDistance) + 1;
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
			uint local_cell_x = (uint)((boid.GlobalPosition.X - MinX) / LocalDistance);
			uint local_cell_y = (uint)((boid.GlobalPosition.Y - MinY) / LocalDistance);
			int localIndex = (int)(local_cell_y * localGridWidth + local_cell_x);
			LocalGrid[localIndex].Add(boid);
		}
		if (RenderingServer.GetVideoAdapterType() == RenderingDevice.DeviceType.DiscreteGpu)
		{
			Vector2 adjustmentVector = new Vector2(MinX, MinY);

			_boidPositionsList.Clear();
			_boidVelocitiesList.Clear();
			_boidGoalsList.Clear();
			_boidIndicesList.Clear();
			_turningWeightsList.Clear();

			_turningWeightsList.Add(Boids[0].SeparationTurnAmount);
			_turningWeightsList.Add(Boids[0].AlignmentTurnAmount);
			_turningWeightsList.Add(Boids[0].CohesionTurnAmount);
			_turningWeightsList.Add(0f);
			for (int i = 0; i < Boids.Count; i++)
			{
				int offset = i * 8;
				_boidPositionsList.Add(Boids[i].GlobalPosition - adjustmentVector);
				_boidVelocitiesList.Add(Boids[i].Velocity);
				_boidGoalsList.Add(Boids[i].Goal - adjustmentVector);
				
				Boid boid = Boids[i];
				uint local_cell_x = (uint)((boid.GlobalPosition.X - MinX) / 50.0f);
				uint local_cell_y = (uint)((boid.GlobalPosition.Y - MinY) / 50.0f);
				uint localIndex = (local_cell_y * localGridWidth + local_cell_x);
				_boidIndicesList.Add(localIndex);
				_turningWeightsList.Add(Boids[i].GoalSeekingTurnAmount);
			}
			
			var positionBytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_boidPositionsList));
			positionBytes.CopyTo(_boidPositions);
			var velocityBytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_boidVelocitiesList));
			velocityBytes.CopyTo(_boidVelocities);
			var goalBytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_boidGoalsList));
			goalBytes.CopyTo(_boidGoals);
			var indicesBytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_boidIndicesList));
			indicesBytes.CopyTo(_boidGridIndices);
			var turningWeightsBytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_turningWeightsList));
			turningWeightsBytes.CopyTo(_packedTurningWeights);

			Buffer.BlockCopy(BitConverter.GetBytes((uint)(Boids.Count)), 0, _numBoidArray, 0, 4);
			
			_rd.BufferUpdate(_boidPositionsBuffer, 0, (uint)_boidPositions.Length, _boidPositions);
			_rd.BufferUpdate(_boidVelocitiesBuffer, 0, (uint)_boidVelocities.Length, _boidVelocities);
			_rd.BufferUpdate(_boidGoalsBuffer, 0, (uint)_boidGoals.Length, _boidGoals);
			_rd.BufferUpdate(_numBoidBuffer, 0, (uint)_numBoidArray.Length, _numBoidArray);
			_rd.BufferUpdate(_boidGridIndicesBuffer, 0, (uint)_boidGridIndices.Length, _boidGridIndices);
			_rd.BufferUpdate(_turningWeightsBuffer, 0, (uint)_packedTurningWeights.Length, _packedTurningWeights);

			Buffer.BlockCopy(BitConverter.GetBytes((float)(SeparatingDistance * SeparatingDistance)), 0, _packedSeparatingGridInfo, 0, 4);
			_rd.BufferUpdate(_separatingGridInfoBuffer, 0, (uint)_packedSeparatingGridInfo.Length, _packedSeparatingGridInfo);
			
			_localGridList.Clear();
			_localGridDataList.Clear();
			
			int currentOffset = 0;
			for (int i = 0; i < LocalGrid.Count; i++)
			{
				_localGridDataList.Add(new Vector2I((int)currentOffset, (int)LocalGrid[i].Count));
				foreach (Boid boid in LocalGrid[i])
				{
					int offset = (int)currentOffset * 16;
					Vector2 adjustedPosition = boid.GlobalPosition - adjustmentVector;
					_localGridList.Add(new Vector4(adjustedPosition.X, adjustedPosition.Y, boid.Velocity.X, boid.Velocity.Y));
					currentOffset++;
				}
			}
			var localGridBytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_localGridList));
			localGridBytes.CopyTo(_packedLocalGrid);
			_rd.BufferUpdate(_localGridBuffer, 0, (uint)_packedLocalGrid.Length, _packedLocalGrid);

			Buffer.BlockCopy(BitConverter.GetBytes(localGridWidth), 0, _packedLocalGridInfo, 0, 4);
			Buffer.BlockCopy(BitConverter.GetBytes(localGridHeight), 0, _packedLocalGridInfo, 4, 4);
			Buffer.BlockCopy(BitConverter.GetBytes((float)(LocalDistanceSquared)), 0, _packedLocalGridInfo, 8, 4);
			for (int i = 0; i < _localGridDataList.Count; i++)
			{
				int offset = i * 8 + 16;
				Buffer.BlockCopy(BitConverter.GetBytes((uint)(_localGridDataList[i][0])), 0, _packedLocalGridInfo, offset, 4);
				Buffer.BlockCopy(BitConverter.GetBytes((uint)(_localGridDataList[i][1])), 0, _packedLocalGridInfo, offset + 4, 4);
			}
			_rd.BufferUpdate(_localGridInfoBuffer, 0, (uint)_packedLocalGridInfo.Length, _packedLocalGridInfo);
			
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

			_packedTurningVectorOutput = _rd.BufferGetData(_turningVectorsBuffer);
			var turningVectorOutputList = MemoryMarshal.Cast<byte, Vector2>(_packedTurningVectorOutput.AsSpan(0, Boids.Count * 8));
			for (int i = 0; i < Boids.Count; i++)
			{
				if (turningVectorOutputList[i].LengthSquared() > Boids[i].MaxSteeringForce * Boids[i].MaxSteeringForce)
				{
					turningVectorOutputList[i] = turningVectorOutputList[i].Normalized() * Boids[i].MaxSteeringForce;
				}
				Vector2 currentHeading = Vector2.FromAngle(Boids[i].Rotation);
				float angleToSteering = currentHeading.AngleTo(turningVectorOutputList[i]);
				Boids[i].Rotation = (float)Mathf.Wrap(Boids[i].Rotation + angleToSteering * Boids[i].RotationStrength * delta, -Mathf.Pi, Mathf.Pi);
				Vector2 newVelocity = Vector2.FromAngle(Boids[i].Rotation);
				
				newVelocity *= Boids[i].Speed;
				Boids[i].Velocity = newVelocity;
				Boids[i].MoveAndSlide();
			}
		}
		else
		{
			List<Vector2> turningVectors = [];
			for (int i = 0; i < Boids.Count; i++)
			{
				Boid boid = Boids[i];
				
				Vector2 separationVector = Vector2.Zero;
				Vector2 alignmentVector = Vector2.Zero;
				Vector2 cohesionVector = Vector2.Zero;
				
				uint localCount = 0;
				uint local_cell_x = (uint)((boid.GlobalPosition.X - MinX) / LocalDistance);
				uint local_cell_y = (uint)((boid.GlobalPosition.Y - MinY) / LocalDistance);
				for (int y = -1; y <= 1; y++) 
				{
					for (int x = -1; x <= 1; x++) 
					{
						int currentX = (int)local_cell_x + x;
						int currentY = (int)local_cell_y + y;
						if (currentY >= 0 && currentY < localGridHeight && currentX >= 0 && currentX < localGridWidth)
						{
							int localIndex = (int)((currentY) * localGridWidth + (currentX));
							for (int j = 0; j < LocalGrid[localIndex].Count; j++)
							{
								Boid otherBoid = LocalGrid[localIndex][j];
								Vector2 toOther = otherBoid.GlobalPosition - boid.GlobalPosition;
								float distSq = toOther.Dot(toOther);
								if (distSq > 0.0001f)
								{
									if (distSq < LocalDistanceSquared)
									{
										localCount++;
										if (distSq < SeparatingDistanceSquared)
										{
											separationVector -= toOther / distSq;
										}
										alignmentVector += otherBoid.Velocity;
										cohesionVector += otherBoid.GlobalPosition;
									}
								}
							}
						}
					}
				}
				Vector2 finalVector = separationVector * boid.SeparationTurnAmount;
				
				if (localCount > 0)
				{
					alignmentVector /= localCount;
					if (alignmentVector.Dot(alignmentVector) > 0.0) 
					{
						finalVector += alignmentVector.Normalized() * boid.AlignmentTurnAmount;
					}
					cohesionVector = cohesionVector / localCount - boid.GlobalPosition;
					if (cohesionVector.Dot(cohesionVector) > 0.0) 
					{
						finalVector += cohesionVector.Normalized() * boid.CohesionTurnAmount;
					}
				}
				Vector2 goalSeekingVector = (boid.Goal - boid.GlobalPosition).Normalized();
				finalVector += goalSeekingVector * boid.GoalSeekingTurnAmount;
				turningVectors.Add(finalVector);
			}
			for (int i = 0; i < Boids.Count; i++)
			{
				if (turningVectors[i].LengthSquared() > Boids[i].MaxSteeringForce * Boids[i].MaxSteeringForce)
				{
					turningVectors[i] = turningVectors[i].Normalized() * Boids[i].MaxSteeringForce;
				}
				Vector2 currentHeading = Vector2.FromAngle(Boids[i].Rotation);
				float angleToSteering = currentHeading.AngleTo(turningVectors[i]);
				Boids[i].Rotation = (float)Mathf.Wrap(Boids[i].Rotation + angleToSteering * Boids[i].RotationStrength * delta, -Mathf.Pi, Mathf.Pi);
				Vector2 newVelocity = Vector2.FromAngle(Boids[i].Rotation);
				
				newVelocity *= Boids[i].Speed;
				Boids[i].Velocity = newVelocity;
				Boids[i].MoveAndSlide();
			}
		}
	}

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
		_separatingGridInfoBuffer = _rd.StorageBufferCreate((uint)(sizeof(float)));
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
		
		RDUniform separatingGridInfoUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0
		};
		separatingGridInfoUniform.AddId(_separatingGridInfoBuffer);
		
		_separatingGridUniformSet = _rd.UniformSetCreate(new Godot.Collections.Array<RDUniform> { separatingGridInfoUniform }, shader, 1);
		
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
