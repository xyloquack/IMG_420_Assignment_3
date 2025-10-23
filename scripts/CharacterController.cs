using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterController : CharacterBody2D
{
	[Signal]
	public delegate void SetRespawnEventHandler(Vector2 position);
	[Signal]
	public delegate void RespawnEventHandler();
	[Signal]
	public delegate void EquipWeaponEventHandler(PackedScene WeaponScene);
	
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
	public PackedScene DustScene { get; set; }
	[Export]
	public PackedScene DashTrailScene { get; set; }
	
	public double TimePassed;
	public int Health;
	public float LastFloorHeight = 0f;
	public AnimatedSprite2D PlayerSprite;
	public PackedScene EquippedWeaponScene;
	
	private Vector2 _bufferSpeed;
	private bool _slowFalling;
	private bool _wasOnFloor;
	private bool _jumping;
	private bool _dashing;
	private bool _isAttacking = false;
	private ShaderMaterial _shaderMat;
	private Timer _invulnerabilityTimer;
	private Timer _dashTimer;
	private Timer _dashCooldown;
	private Timer _jumpTimer;
	private Timer _coyoteTimer;
	private Timer _flashTimer;
	private Vector2 _respawnPoint;
	private AudioStreamPlayer2D _movingSound1;
	private AudioStreamPlayer2D _movingSound2;
	private AudioStreamPlayer2D _landingSound;
	private AudioStreamPlayer2D _hitSound;
	private Weapon _equippedWeapon = null;
	
	public override void _Ready() 
	{
		GetNode<PlayerData>("/root/PlayerData").LoadData(this);
		_bufferSpeed = Vector2.Zero;
		_slowFalling = false;
		_dashing = false; 
		PlayerSprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		_shaderMat = (ShaderMaterial)PlayerSprite.Material;
		_shaderMat.SetShaderParameter("flash_color", new Vector4(1.0f, 0.3f, 0.2f, 1.0f));
		_invulnerabilityTimer = GetNode<Timer>("InvulnerabilityTimer");
		_dashTimer = GetNode<Timer>("DashTimer");
		_dashCooldown = GetNode<Timer>("DashCooldown");
		_jumpTimer = GetNode<Timer>("JumpTimer");
		_coyoteTimer = GetNode<Timer>("CoyoteTimer");
		_flashTimer = GetNode<Timer>("FlashTimer");
		_respawnPoint = GlobalPosition;
		_movingSound1 = GetNode<AudioStreamPlayer2D>("MovingSound1");
		_movingSound2 = GetNode<AudioStreamPlayer2D>("MovingSound2");
		_landingSound = GetNode<AudioStreamPlayer2D>("LandingSound");
		_hitSound = GetNode<AudioStreamPlayer2D>("HitSound");
	}
	
	public override void _PhysicsProcess(double delta) 
	{
		TimePassed += delta;
		_shaderMat.SetShaderParameter("opacity", _flashTimer.TimeLeft / _flashTimer.WaitTime);
		UpdateVelocity(delta);
		UpdateSprite();
		if (_equippedWeapon != null)
		{
			UpdateWeaponInfo();
		}
		if (Input.IsActionJustPressed("attack") && _equippedWeapon != null)
		{
			_equippedWeapon.Attack();
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
				if (PlayerSprite.FlipH)
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
			dashTrail.Position = PlayerSprite.Offset;
			dashTrail.ZIndex = PlayerSprite.ZIndex - 1;
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
			LastFloorHeight = GlobalPosition.Y;
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
		PlayerSprite.Offset = newOffset;
		
		if (_dashing)
		{
			PlayerSprite.SetFrame(PlayerSprite.GetSpriteFrames().GetFrameCount(PlayerSprite.Animation) - 1);
		}
		else
		{
			PlayerSprite.Stop();
			int numFrames = PlayerSprite.GetSpriteFrames().GetFrameCount(PlayerSprite.Animation) - 1;
			int newFrame = (int)Math.Floor(numFrames * (Math.Abs(Velocity.X) / Speed));
			if (newFrame >= numFrames) 
			{
				newFrame = numFrames - 1;
			}
			PlayerSprite.SetFrame(newFrame);
		}
		if (Velocity.X < 0) 
		{
			PlayerSprite.FlipH = true;
			PlayerSprite.Animation = "walk_left";
		}
		if (Velocity.X > 0) 
		{
			PlayerSprite.FlipH = false;
			PlayerSprite.Animation = "walk_right";
		}
	}

	public void UpdateWeaponInfo()
	{
		_equippedWeapon.ParentPosition = GlobalPosition;
		_equippedWeapon.ParentFlipped = PlayerSprite.FlipH;
	}

	private void OnAttacked(Vector2 velocityChange) 
	{
		if (_dashing && _dashTimer.TimeLeft < _dashTimer.WaitTime / 2)
		{
			if (_bufferSpeed == Vector2.Zero)
			{
				_bufferSpeed = velocityChange;
			}
		}
		else
		{
			Velocity += velocityChange;
		}
	}
	
	public int GetAmmo()
	{
		if (_equippedWeapon != null)
		{
			return _equippedWeapon.GetAmmo();
		}
		return 0;
	}
	
	public void SetAmmo(int num)
	{
		if (_equippedWeapon != null)
		{
			_equippedWeapon.SetAmmo(num);
		}
	}
	
	public int GetMaxAmmo()
	{
		if (_equippedWeapon != null)
		{
			return _equippedWeapon.GetMaxAmmo();
		}
		return 1;
	}
	
	private void OnDamage(float damage)
	{
		if (_invulnerabilityTimer.IsStopped())
		{
			Health -= (int)damage;
			_flashTimer.Start();
			_hitSound.PitchScale = (float)(0.9 + GD.Randf() * 0.2);
			_hitSound.Play();
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
	
	private void OnEquipWeapon(PackedScene WeaponScene)
	{
		if (_equippedWeapon != null)
		{
			_equippedWeapon.Attacked -= OnAttacked;
			_equippedWeapon.QueueFree();
		}

		EquippedWeaponScene = WeaponScene;
		_equippedWeapon = EquippedWeaponScene.Instantiate<Weapon>();
		_equippedWeapon.Attacked += OnAttacked;
		_equippedWeapon.WorldScene = GetParent();
		AddChild(_equippedWeapon);
	}
	
	override public void _ExitTree()
	{
		if (Health == 0)
		{
			Health = MaxHealth;
		}
		GetNode<PlayerData>("/root/PlayerData").SaveData(this);
	}
}
