using Godot;
using System;
using System.Collections.Generic;

public partial class Boid : CharacterBody2D
{
	[Signal]
	private delegate void KillEventHandler();
	
	[Export]
	public float Speed;
	[Export]
	public float RotationStrength;
	[Export]
	public float MaxSteeringForce;
	[Export]
	public float IdealDistance;
	[Export]
	public float SeparationTurnAmount;
	[Export]
	public float AlignmentTurnAmount;
	[Export]
	public float CohesionTurnAmount;
	[Export]
	public float GoalSeekingTurnAmount;
	[Export]
	public float DamageAmount;
	[Export]
	public PackedScene DeathParticles;
	
	public Vector2 Goal;
	public bool Active = false;
	
	private CollisionShape2D _collision;
	private Timer _lifetimeTimer;
	private Timer _deathTime;
	private AudioController _audioController;
	private float _waitTime = 0f;
	private bool _dying = false;
	
	override public void _Ready() 
	{
		GetNode<BoidHitBox>("HitBox").DamageAmount = DamageAmount;
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids.Add(this);
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
		_lifetimeTimer = GetNode<Timer>("Lifetime");
		_deathTime = GetNode<Timer>("DeathTime");
		_audioController = GetNode<AudioController>("/root/AudioController");
	}
	
	public void Launch(Vector2 goal, float speed, float rotation)
	{
		_lifetimeTimer.WaitTime += GD.Randf() * 0.2;
		_lifetimeTimer.Start();
		Goal = goal;
		Speed = speed;
		Rotation = rotation;
		GoalSeekingTurnAmount = 3f;
		RotationStrength = 25f;
		MaxSteeringForce = 20f;
		Active = true;
		Node newParent = GetParent().GetParent();
		Reparent(newParent);
		ResetPhysicsInterpolation();
		PlayAudioAfterDelay(GD.Randf() * 0.1);
	}
	
	public void OnTimeout()
	{
		if (Active)
		{
			EmitSignal("Kill");
		}
	}
	
	public void OnKill()
	{
		if (!_dying)
		{
			_dying = true;
			BoidDeathParticles deathParticles = DeathParticles.Instantiate<BoidDeathParticles>();
			deathParticles.Emitting = true;
			deathParticles.Restart();
			deathParticles.GlobalPosition = GlobalPosition;
			deathParticles.ZIndex = ZIndex;
			GetTree().Root.AddChild(deathParticles);
			GetTree().Root.GetNode<BoidManager>("BoidManager").Boids.Remove(this);
			_waitTime = Mathf.Max(_audioController.RequestAudio("BoidDeathAudio", 0, this) + 0.1f, _waitTime);
			Hide();
			Active = false;
			_deathTime.WaitTime = _waitTime;
			_deathTime.Start();
		}
	}
	
	public void PlayAudioAfterDelay(double delay)
	{
		Timer audioTimer = GetNode<Timer>("AudioTimer");
		audioTimer.WaitTime = (float)delay;
		audioTimer.Start();
	}
	
	private void OnAudioDelayTimeout()
	{
		_waitTime = Mathf.Max(_audioController.RequestAudio("BoidWhooshAudio", 0, this) + 0.1f, _waitTime);
		if (!_deathTime.IsStopped())
		{
			if (_deathTime.TimeLeft < _deathTime.WaitTime)
			{
				_deathTime.WaitTime = _waitTime;
				_deathTime.Start();
			}
		}
	}
	
	private void OnAudioComplete()
	{
		QueueFree();
	}
}
