using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node2D
{
	[Signal]
	public delegate void DeathEventHandler();
	[Signal]
	public delegate void RemoveEnemyEventHandler(Enemy givenEnemy);
	
	public float TimePassed;
	private PauseScreen _pauseScreen;
	private DeathScreen _deathScreen;
	private VictoryScreen _victoryScreen;
	private List<Enemy> RequiredEnemies = [];
	
	override public void _Ready()
	{
		TimePassed = 0.0f;
		_pauseScreen = GetNode<PauseScreen>("PauseScreen");
		_deathScreen = GetNode<DeathScreen>("DeathScreen");
		_victoryScreen = GetNode<VictoryScreen>("VictoryScreen");
		
		//RequiredEnemies.Add(GetNode<Enemy>("Enemy"));
		RequiredEnemies.Add(GetNode<Enemy>("SmallBird"));
	}
	
	override public void _PhysicsProcess(double delta)
	{
		TimePassed += (float)delta;
		if (TimePassed > 1f)
		{
			TimePassed -= 1f;
			GD.Print(Engine.GetFramesPerSecond());
		}
		
		if (Input.IsActionJustPressed("pause"))
		{
			_pauseScreen.Pause();
		}
		bool win = false;
		if (RequiredEnemies.Count == 0)
		{
			win = true;
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
	
	private void OnRemoveEnemy(Enemy givenEnemy)
	{
		if (RequiredEnemies.Contains(givenEnemy))
		{
			RequiredEnemies.Remove(givenEnemy);
		}
	}
}
