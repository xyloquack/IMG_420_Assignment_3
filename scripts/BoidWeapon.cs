using Godot;
using System;
using System.Collections.Generic;

public partial class BoidWeapon : Weapon
{
	[Export]
	public PackedScene BoidScene { get; set; }
	[Export]
	public int MaxBoids = 10;
	
	public int NumBoids;
	public List<Boid> Boids = [];

	private List<Boid> _boidsToLaunch;
	private const int BOIDS_PER_FRAME = 100;
	private Timer _attackCooldown;
	private bool _isAttacking = false;
	private CharacterController _player;

	override public void _Ready()
	{
		_attackCooldown = GetNode<Timer>("AttackCooldown");
		_player = GetParent<CharacterController>();
		for (int i = 0; i < NumBoids; i++)
		{
			BoidSpawn();
		}
	}
	
	override public void _PhysicsProcess(double delta)
	{
		AttemptBoidSpawn();
		UpdateIdleBoidGoal();
		if (_isAttacking)
		{
			int launchCount = Math.Min(BOIDS_PER_FRAME, _boidsToLaunch.Count);
			for (int i = 0; i < launchCount; i++)
			{
				Boid boid = _boidsToLaunch[0];
				Vector2 mousePosition = GetGlobalMousePosition();
				boid.CallDeferred("Launch", mousePosition, 1000f, (float)(Vector2.Right.AngleTo(mousePosition) + GD.Randf() * 3 - 1.5));
				_boidsToLaunch.RemoveAt(0);
			}
			if (_boidsToLaunch.Count == 0)
			{
				_isAttacking = false;
			}
		}
	}

	override public Vector2 Attack()
	{
		Vector2 velocityChange = Vector2.Zero;
		if (_attackCooldown.IsStopped() && NumBoids > 0 && !_isAttacking)
		{
			_player.TimeSinceLastAttack = 0;
			
			Vector2 mousePosition = GetGlobalMousePosition();
			velocityChange = -(mousePosition - GlobalPosition).Normalized();
			velocityChange.X *= 1100 * ((float)NumBoids / (float)MaxBoids);
			velocityChange.Y *= 600 * ((float)NumBoids / (float)MaxBoids);
			
			_boidsToLaunch = new List<Boid>(Boids);
			_isAttacking = true;

			NumBoids = 0;
			Boids.Clear();
			_attackCooldown.Start();
		}
		return velocityChange;
	}

	private void AttemptBoidSpawn() 
	{
		for (int i = 0; i < Math.Floor(Mathf.Clamp(Math.Pow(3, _player.TimeSinceLastAttack) - 1, 0, MaxBoids) - NumBoids); i++)
		{
			BoidSpawn();
			NumBoids++;
		}
	}

	private void BoidSpawn()
	{
		Boid newBoid = BoidScene.Instantiate<Boid>();
		Vector2 newPosition = _player.GlobalPosition;
		newPosition.Y += (float)(GD.Randf() * 10f - 15f);
		newBoid.GlobalPosition = newPosition;
		newBoid.Speed = 400;
		newBoid.GoalSeekingTurnAmount = 0.5f;
		Boids.Add(newBoid);
		_player.GetParent().CallDeferred("add_child", newBoid);
	}

	private void UpdateIdleBoidGoal()
	{
		List<Boid> BoidsToRemove = [];
		foreach (Boid boid in Boids)
		{
			Vector2 goalPosition = _player.GlobalPosition + new Vector2(0, -25);
			if (_player.PlayerSprite.FlipH)
			{
				goalPosition.X += 40;
			}
			else
			{
				goalPosition.X -= 40;
			}
			boid.Goal = goalPosition;
			float distance = (boid.GlobalPosition - _player.GlobalPosition).Length();
			if (distance > 400)
			{
				BoidsToRemove.Add(boid);
			}
			else
			{
				boid.Speed = 200 + (Mathf.Clamp((distance - 50), 0, 1000)) * 4;
			}
		}
		foreach (Boid boid in BoidsToRemove)
		{
			Boids.Remove(boid);
			boid.CallDeferred("emit_signal", "Kill");
			NumBoids--;
			AttemptBoidSpawn();
		}
	}

	override public int GetAmmo()
	{
		return NumBoids;
	}

	override public void SetAmmo(int num)
	{
		NumBoids = num;
	}

	override public int GetMaxAmmo()
	{
		return MaxBoids;
	}
}
