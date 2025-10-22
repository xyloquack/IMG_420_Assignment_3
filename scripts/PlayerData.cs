using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerData : Node
{
	public double TimePassed = 0.0;
	public double TimeSinceLastAttack = 0.0;
	public int numAmmo = 0;
	public int Health = 10;
	
	public void SaveData(CharacterController player)
	{
		TimePassed = player.TimePassed;
		TimeSinceLastAttack = player.TimeSinceLastAttack;
		numAmmo = player.GetAmmo();
		Health = player.Health;
		GD.Print("TimePassed: ", TimePassed);
		GD.Print("TimeSinceLastAttack: ", TimeSinceLastAttack);
		GD.Print("numAmmo: ", numAmmo);
		GD.Print("Health: ", Health);
	}
	
	public void LoadData(CharacterController player)
	{
		player.TimePassed = TimePassed;
		player.TimeSinceLastAttack = TimeSinceLastAttack;
		player.SetAmmo(numAmmo);
		player.Health = Health;
		GD.Print("TimePassed: ", TimePassed);
		GD.Print("TimeSinceLastAttack: ", TimeSinceLastAttack);
		GD.Print("numAmmo: ", numAmmo);
		GD.Print("Health: ", Health);
	}
}
