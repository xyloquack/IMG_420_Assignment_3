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
		BoidDeathParticles deathParticles = DeathParticles.Instantiate<BoidDeathParticles>();
		deathParticles.Emitting = true;
		deathParticles.Restart();
		deathParticles.GlobalPosition = GlobalPosition;
		deathParticles.ZIndex = ZIndex;
		GetTree().Root.AddChild(deathParticles);
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids.Remove(this);
		float waitTime = _audioController.RequestAudio("BoidDeathAudio", 0, this) + 0.1f;
		Hide();
		Active = false;
		if (waitTime > 0f)
		{
			_deathTime.WaitTime = waitTime;
			_deathTime.Start();
		}
		else
		{
			QueueFree();
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
		_audioController.RequestAudio("BoidWhooshAudio", 0, this);
	}
	
	private void OnAudioComplete()
	{
		QueueFree();
	}
}
