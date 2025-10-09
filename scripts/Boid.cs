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
	
	override public void _Ready() 
	{
		GetNode<BoidHitBox>("HitBox").DamageAmount = DamageAmount;
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids.Add(this);
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
		CpuParticles2D deathParticles = DeathParticles.Instantiate<CpuParticles2D>();
		deathParticles.Emitting = true;
		deathParticles.Restart();
		deathParticles.GlobalPosition = GlobalPosition;
		deathParticles.ZIndex = ZIndex;
		GetTree().Root.AddChild(deathParticles);
		GetTree().Root.GetNode<BoidManager>("BoidManager").Boids.Remove(this);
		AudioStreamPlayer2D deathAudio = GetNode<AudioStreamPlayer2D>("DeathAudio");
		deathAudio.PitchScale = 0.90f + (GD.Randf() * 0.2f);
		deathAudio.Play();
		Hide();
		Active = false;
	}
	
	public void PlayAudioAfterDelay(double delay)
	{
		Timer audioTimer = GetNode<Timer>("AudioTimer");
		audioTimer.WaitTime = (float)delay;
		audioTimer.Start();
	}
	
	private void OnAudioDelayTimeout()
	{
		AudioStreamPlayer2D whooshAudio = GetNode<AudioStreamPlayer2D>("WhooshAudio");
		whooshAudio.PitchScale = 0.90f + (GD.Randf() * 0.2f);
		whooshAudio.Play();
		GD.Print("Playing Audio!");
	}
	
	private void OnAudioComplete()
	{
		QueueFree();
	}
}
