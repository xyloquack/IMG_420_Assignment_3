using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node2D
{
	[Signal]
	public delegate void DeathEventHandler();
	
	public float TimePassed;
	private PauseScreen _pauseScreen;
	private DeathScreen _deathScreen;
	private VictoryScreen _victoryScreen;
	private List<Enemy> EnemyList = [];
	
	override public void _Ready()
	{
		TimePassed = 0.0f;
		_pauseScreen = GetNode<PauseScreen>("PauseScreen");
		_deathScreen = GetNode<DeathScreen>("DeathScreen");
		_victoryScreen = GetNode<VictoryScreen>("VictoryScreen");
		
		EnemyList.Add(GetNode<Enemy>("Enemy"));
	}
	
	override public void _PhysicsProcess(double delta)
	{
		TimePassed += (float)delta;
		if (TimePassed > 1f)
		{
			TimePassed -= 1f;
			GD.Print(Engine.GetFramesPerSecond());
		}
		
		if (Input.IsActionPressed("pause"))
		{
			_pauseScreen.Pause();
		}
		bool win = true;
		foreach (Enemy enemy in EnemyList)
		{
			if (enemy.IsDead == false)
			{
				win = false;
				break;
			}
		}
		if (win)
		{
			_victoryScreen.Win();
		}
	}
	
	private void OnDeath()
	{
		_deathScreen.Dead();
	}
}
