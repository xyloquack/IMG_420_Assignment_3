using Godot;
using System;

public partial class SmallBird : CharacterBody2D
{
	[Export]
	public float VerticalSpeed;
	[Export]
	public float HorizontalSpeed;
	[Export]
	public float RandomVSpeedOffset;
	[Export]
	public float Gravity;
	[Export]
	public float WanderRange;
	[Export]
	public float MaxHealth;
	[Export]
	public float Damage;
	
	public Vector2 SuggestedVelocity = Vector2.Zero;
	public bool IsDead = false;
	
	private Timer _flapTimer;
	private Timer _flashTimer;
	private AnimatedSprite2D _sprite;
	private NavigationAgent2D _navigation;
	private Vector2 _homePosition;
	private float _health;
	private CharacterController _player = null;
	private ShaderMaterial _shaderMat;
	
	override public void _Ready()
	{
		_flapTimer = GetNode<Timer>("FlapTimer");
		_flashTimer = GetNode<Timer>("FlashTimer");
		_sprite = GetNode<AnimatedSprite2D>("SmallBirdSprite");
		_navigation = GetNode<NavigationAgent2D>("Navigation");
		_homePosition = GlobalPosition;
		_health = MaxHealth;
		_shaderMat = (ShaderMaterial)_sprite.Material;
		GetNode<HitBox>("HitBox").DamageAmount = Damage;
	}
	
	override public void _PhysicsProcess(double delta)
	{
		if (_player == null)
		{
			_navigation.TargetPosition = _homePosition + new Vector2(GD.Randf() * WanderRange, GD.Randf() * WanderRange);
		}
		else
		{
			_navigation.TargetPosition = new Vector2(_player.GlobalPosition.X, _player.LastFloorHeight - 50);
		}
		SuggestedVelocity = (_navigation.GetNextPathPosition() - GlobalPosition).Normalized();
		_shaderMat.SetShaderParameter("opacity", _flashTimer.TimeLeft / _flashTimer.WaitTime);
		Vector2 newVelocity = Velocity;
		if (SuggestedVelocity.Y < 0 && _flapTimer.IsStopped()) 
		{
			newVelocity.Y = Flap();
			_sprite.Play();
		}
		newVelocity.X = Mathf.Lerp(newVelocity.X, SuggestedVelocity.X * HorizontalSpeed, 0.3f);
		newVelocity.Y += Gravity * (float)delta;
		Velocity = newVelocity;
		MoveAndSlide();
		if (Velocity.X < 0)
		{
			_sprite.FlipH = false;
		}
		else if (Velocity.X > 0)
		{
			_sprite.FlipH = true;
		}
	}
	
	private float Flap()
	{
		GD.Print("Flap!");
		_flapTimer.Start();
		return -VerticalSpeed - GD.Randf() * RandomVSpeedOffset;
	}
	
	private void OnDamage(float damage)
	{
		_health -= damage;
		_shaderMat.SetShaderParameter("enable", true);
		_flashTimer.Start();
		CheckHealth();
	}
	
	private void OnDetectionEntered(Node2D node)
	{
		if (node.IsInGroup("player"))
		{
			_player = (CharacterController)node;
		}
	}
	
	private void OnFlashTimeout()
	{
		_shaderMat.SetShaderParameter("enable", false);
	}
	
	private void CheckHealth()
	{
		if (_health <= 0)
		{
			IsDead = true;
			Hide();
		}
	}
}
