using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterController : CharacterBody2D
{
	[Signal]
	public delegate void SetRespawnEventHandler(Vector2 position);
	[Signal]
	public delegate void RespawnEventHandler();
	
	[Export]
	public int MaxHealth;
	[Export]
	public float Speed;
	[Export]
	public float DashSpeed;
	[Export]
	public float Acceleration;
	[Export]
	public float AirAccelerationMult;
	[Export]
	public float Friction;
	[Export]
	public float AirFrictionMult;
	[Export]
	public float JumpSpeed;
	[Export]
	public float BurstJumpSpeed;
	[Export]
	public float Gravity;
	[Export]
	public float SlowFallMult;
	[Export]
	public PackedScene BoidScene { get; set; }
	[Export]
	public int MaxBoids;
	[Export]
	public PackedScene DustScene { get; set; }
	[Export]
	public PackedScene DashTrailScene { get; set; }
	
	public double TimePassed;
	public double TimeSinceLastAttack;
	public List<Boid> Boids = [];
	public int NumBoids;
	public int Health;
	public float LastFloorHeight = 0f;
	
	private Vector2 _bufferSpeed;
	private bool _slowFalling;
	private bool _wasOnFloor;
	private bool _jumping;
	private bool _dashing;
	private bool _isAttacking = false;
	private List<Boid> _boidsToLaunch;
	private const int BOIDS_PER_FRAME = 100;
	private AnimatedSprite2D _playerSprite;
	private Timer _invulnerabilityTimer;
	private Timer _dashTimer;
	private Timer _dashCooldown;
	private Timer _jumpTimer;
	private Timer _attackCooldown;
	private Timer _coyoteTimer;
	private Vector2 _respawnPoint;
	private AudioStreamPlayer2D _movingSound1;
	private AudioStreamPlayer2D _movingSound2;
	private AudioStreamPlayer2D _landingSound;
	
	public override void _Ready() 
	{
		GetNode<PlayerData>("/root/PlayerData").LoadData(this);
		_bufferSpeed = Vector2.Zero;
		_slowFalling = false;
		_dashing = false; 
		_playerSprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		_invulnerabilityTimer = GetNode<Timer>("InvulnerabilityTimer");
		_dashTimer = GetNode<Timer>("DashTimer");
		_dashCooldown = GetNode<Timer>("DashCooldown");
		_jumpTimer = GetNode<Timer>("JumpTimer");
		_attackCooldown = GetNode<Timer>("AttackCooldown");
		_coyoteTimer = GetNode<Timer>("CoyoteTimer");
		_respawnPoint = GlobalPosition;
		_movingSound1 = GetNode<AudioStreamPlayer2D>("MovingSound1");
		_movingSound2 = GetNode<AudioStreamPlayer2D>("MovingSound2");
		_landingSound = GetNode<AudioStreamPlayer2D>("LandingSound");
	}
	
	public override void _PhysicsProcess(double delta) 
	{
		TimePassed += delta;
		TimeSinceLastAttack += delta;
		UpdateVelocity(delta);
		UpdateSprite();
		AttemptBoidSpawn();
		UpdateIdleBoidGoal();
		if (Input.IsActionJustPressed("attack"))
		{
			Attack();
		}
		if (_isAttacking)
		{
			int launchCount = Math.Min(BOIDS_PER_FRAME, _boidsToLaunch.Count);
			for (int i = 0; i < launchCount; i++)
			{
				Boid boid = _boidsToLaunch[0];
				Vector2 mousePosition = GetGlobalMousePosition();
				boid.Launch(mousePosition, 1000f, (float)(Vector2.Right.AngleTo(mousePosition) + GD.Randf() * 3 - 1.5));
				_boidsToLaunch.RemoveAt(0);
			}
			if (_boidsToLaunch.Count == 0)
			{
				_isAttacking = false;
			}
		}
		MoveAndSlide();
	}
	
	private void UpdateVelocity(double delta) 
	{
		if (Input.IsActionPressed("dash") && _dashTimer.IsStopped() && _dashCooldown.IsStopped())
		{
			_dashing = true;
			int direction = 0;
			if (Input.IsActionPressed("left")) 
			{
				direction -= 1;
			}
			if (Input.IsActionPressed("right")) 
			{
				direction += 1;
			}
			if (direction == 0)
			{
				if (_playerSprite.FlipH)
				{
					direction = -1;
				}
				else
				{
					direction = 1;
				}
			}
			Vector2 newVelocity = new Vector2(DashSpeed * direction, 0);
			Velocity = newVelocity;
			_dashTimer.Start();
			
			GpuParticles2D dashTrail = DashTrailScene.Instantiate<GpuParticles2D>();
			dashTrail.Emitting = true;
			dashTrail.Position = _playerSprite.Offset;
			dashTrail.ZIndex = _playerSprite.ZIndex - 1;
			AddChild(dashTrail);
		}
		if (_dashing)
		{
			if (!IsOnFloor())
			{
				Vector2 newVelocity = Velocity;
				newVelocity.Y += Gravity / 2 * (float)delta;
				Velocity = newVelocity;
				if (_wasOnFloor)
				{
					_coyoteTimer.Start();
					_wasOnFloor = false;
				}
			}
			else
			{
				_wasOnFloor = true;
				Vector2 newVelocity = Velocity;
				newVelocity.Y = 0;
				Velocity = newVelocity;
				LastFloorHeight = GlobalPosition.Y;
			}
		}
		else
		{
			Velocity = WalkingVelocity(delta);
		}
		_movingSound1.VolumeDb = Mathf.Lerp(_movingSound1.VolumeDb, (float)(Mathf.Clamp(Velocity.Length() / (Speed / 4), 0, 8) - 26), 0.25f);
		_movingSound1.PitchScale = Mathf.Lerp(_movingSound1.PitchScale, (float)(Mathf.Clamp(Velocity.Length() / (Speed / 2), 0.1, 4) / 8 + 8), 0.25f);
		_movingSound2.VolumeDb = Mathf.Lerp(_movingSound2.VolumeDb, (float)(Mathf.Clamp(Velocity.Length() / (Speed / 4), 0, 8) - 26), 0.25f);
		_movingSound2.PitchScale = Mathf.Lerp(_movingSound2.PitchScale, (float)(Mathf.Clamp(Velocity.Length() / (Speed / 2), 0.1, 4) / 8 + 8), 0.25f);
	}
	
	private Vector2 WalkingVelocity(double delta)
	{
		Vector2 newVelocity = Velocity;
		int direction = 0;
		if (Input.IsActionPressed("left")) 
		{
			direction -= 1;
		}
		if (Input.IsActionPressed("right")) 
		{
			direction += 1;
		}
		float currentFriction = Friction * Math.Abs(newVelocity.X) / Speed;
		float currentAcceleration = Acceleration;
		float currentGravity = Gravity;
		
		if (Math.Abs(newVelocity.X) > Speed) 
		{
			currentAcceleration = 0;
		}
		
		if (!IsOnFloor()) 
		{
			currentAcceleration *= AirAccelerationMult;
			currentFriction *= AirFrictionMult;
			_wasOnFloor = false;
		}
		else
		{
			_coyoteTimer.Start();
			if (!_wasOnFloor)
			{
				_landingSound.PitchScale = 0.8f + (GD.Randf() * 0.4f);
				_landingSound.Play();
				DustKickup newDust = DustScene.Instantiate<DustKickup>(); 
				Vector2 dustPosition = GlobalPosition;
				dustPosition.Y += 22;
				newDust.GlobalPosition = dustPosition;
				newDust.Emitting = true;
				newDust.ZIndex = ZIndex + 1;
				GetParent().AddChild(newDust);
				GD.Print("Landing!");
			}
			_wasOnFloor = true;
		}
		newVelocity.X = Mathf.Lerp(newVelocity.X, newVelocity.X + (direction * currentAcceleration - currentFriction * Math.Sign(Velocity.X)) * (float)delta, 0.55f);
		int currentMovingDirection = Math.Sign(newVelocity.X);
		newVelocity.X = Mathf.Lerp(newVelocity.X, newVelocity.X - currentMovingDirection * currentFriction * (float)delta, 0.55f);
		int newMovingDirection = Math.Sign(newVelocity.X);
		if (currentMovingDirection != newMovingDirection) 
		{
			newVelocity.X = 0;
		}
		if ((IsOnFloor() || (!_coyoteTimer.IsStopped())) && Input.IsActionPressed("jump") && !_jumping) 
		{
			_slowFalling = true;
			_jumping = true;
			_wasOnFloor = false;
			_jumpTimer.Start();
			newVelocity.Y = -BurstJumpSpeed;
		}
		if (_slowFalling && (!(Input.IsActionPressed("jump")) || IsOnFloor())) 
		{
			_slowFalling = false;
		}
		if (_slowFalling) 
		{
			currentGravity *= SlowFallMult;
		}
		if (_jumping) 
		{
			_coyoteTimer.Stop();
			if (!(Input.IsActionPressed("jump")) || _jumpTimer.IsStopped()) 
			{
				_jumping = false;
				if (!(_jumpTimer.IsStopped())) 
				{
					newVelocity.Y /= 4;
				}
				_jumpTimer.Stop();
			}
			else 
			{
				newVelocity.Y = Mathf.Lerp(newVelocity.Y, -JumpSpeed, (float)0.15);
			}
		}
		else if (!IsOnFloor())
		{
			newVelocity.Y += currentGravity * (float)delta;
		}
		else 
		{
			newVelocity.Y = 0;
		}
		return newVelocity;
	}
	
	private void OnDashTimeout()
	{
		_dashing = false;
		_dashCooldown.Start();
		if (_bufferSpeed != Vector2.Zero)
		{
			Vector2 newVelocity = Velocity;
			newVelocity += _bufferSpeed;
			Velocity = newVelocity;
			_bufferSpeed = Vector2.Zero;
		}
	}
	
	private void UpdateSprite() 
	{
		Vector2 newOffset;
		newOffset.X = 0;
		newOffset.Y = (float)(4 * Math.Sin(TimePassed * 2.5) - 4);
		_playerSprite.Offset = newOffset;
		
		if (_dashing)
		{
			_playerSprite.SetFrame(_playerSprite.GetSpriteFrames().GetFrameCount(_playerSprite.Animation) - 1);
		}
		else
		{
			_playerSprite.Stop();
			int numFrames = _playerSprite.GetSpriteFrames().GetFrameCount(_playerSprite.Animation) - 1;
			int newFrame = (int)Math.Floor(numFrames * (Math.Abs(Velocity.X) / Speed));
			if (newFrame >= numFrames) 
			{
				newFrame = numFrames - 1;
			}
			_playerSprite.SetFrame(newFrame);
		}
		if (Velocity.X < 0) 
		{
			_playerSprite.FlipH = true;
			_playerSprite.Animation = "walk_left";
		}
		if (Velocity.X > 0) 
		{
			_playerSprite.FlipH = false;
			_playerSprite.Animation = "walk_right";
		}
		
	}
	
	private void AttemptBoidSpawn() 
	{
		for (int i = 0; i < Math.Floor(Mathf.Clamp(Math.Pow(3, TimeSinceLastAttack) - 1, 0, MaxBoids) - NumBoids); i++)
		{
			Boid newBoid = BoidScene.Instantiate<Boid>();
			Vector2 newPosition = GlobalPosition;
			newPosition.Y += (float)(GD.Randf() * 10f - 15f);
			newBoid.GlobalPosition = newPosition;
			newBoid.Speed = 300;
			newBoid.GoalSeekingTurnAmount = 0.5f;
			Boids.Add(newBoid);
			GetParent().AddChild(newBoid);
			NumBoids++;
		}
	}
	
	private void UpdateIdleBoidGoal()
	{
		List<Boid> BoidsToRemove = [];
		foreach (Boid boid in Boids)
		{
			Vector2 goalPosition = GlobalPosition + new Vector2(0, -25);
			if (_playerSprite.FlipH)
			{
				goalPosition.X += 40;
			}
			else
			{
				goalPosition.X -= 40;
			}
			boid.Goal = goalPosition;
			if ((boid.GlobalPosition - GlobalPosition).Length() > 200)
			{
				BoidsToRemove.Add(boid);
			}
		}
		foreach (Boid boid in BoidsToRemove)
		{
			Boids.Remove(boid);
			boid.EmitSignal("Kill");
			NumBoids--;
			AttemptBoidSpawn();
		}
	}
	
	
	private void Attack()
	{
		if (_attackCooldown.IsStopped() && NumBoids > 0 && !_isAttacking)
		{
			TimeSinceLastAttack = 0;
			
			Vector2 mousePosition = GetGlobalMousePosition();
			Vector2 velocityChange = -(mousePosition - GlobalPosition).Normalized();
			velocityChange.X *= 1100 * ((float)NumBoids / (float)MaxBoids);
			velocityChange.Y *= 600 * ((float)NumBoids / (float)MaxBoids);
			
			if (_dashing && _dashTimer.TimeLeft < _dashTimer.WaitTime / 2)
			{
				_bufferSpeed = velocityChange;
			}
			else
			{
				Velocity += velocityChange;
			}
			
			_boidsToLaunch = new List<Boid>(Boids);
			_isAttacking = true;

			NumBoids = 0;
			Boids.Clear();
			_attackCooldown.Start();
		}
	}
	
	private void OnDamage(float damage)
	{
		if (_invulnerabilityTimer.IsStopped())
		{
			Health -= (int)damage;
			GD.Print(Health);
			_invulnerabilityTimer.Start();
			CheckHealth();
		}
	}
	
	private void CheckHealth()
	{
		if (Health <= 0)
		{
			GetParent<World>().EmitSignal("Death");
		}
	}
	
	private void OnSetRespawn(Vector2 position)
	{
		_respawnPoint = position;
	}
	
	private void OnRespawn()
	{
		GlobalPosition = _respawnPoint;
	}
	
	override public void _ExitTree()
	{
		GetNode<PlayerData>("/root/PlayerData").SaveData(this);
	}
}
