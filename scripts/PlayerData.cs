using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerData : Node
{
	public double TimePassed = 0.0;
	public double TimeSinceLastAttack = 0.0;
	public List<Boid> Boids = [];
	public int NumBoids = 0;
	public int Health = 10;
	
	public void SaveData(CharacterController player)
	{
		TimePassed = player.TimePassed;
		TimeSinceLastAttack = player.TimeSinceLastAttack;
		Boids = player.Boids;
		NumBoids = player.NumBoids;
		Health = player.Health;
	}
	
	public void LoadData(CharacterController player)
	{
		player.TimePassed = TimePassed;
		player.TimeSinceLastAttack = TimeSinceLastAttack;
		player.Boids = Boids;
		player.NumBoids = NumBoids;
		player.Health = Health;
	}
}
