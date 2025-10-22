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
		
		RequiredEnemies.Add(GetNode<Enemy>("SmallBird"));
		RequiredEnemies.Add(GetNode<Enemy>("SmallBird2"));
		RequiredEnemies.Add(GetNode<Enemy>("SmallBird3"));
		RequiredEnemies.Add(GetNode<Enemy>("SmallBird4"));
		GetNode<Sprite2D>("Guides/AttackGuide").Visible = false;
		GetNode<Sprite2D>("Guides/BoostDashGuide").Visible = false;
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
	
	private void OnGuideZoneEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			GetNode<Sprite2D>("Guides/AttackGuide").Visible = true;
			GetNode<Sprite2D>("Guides/BoostDashGuide").Visible = true;
		}
	}
}
