using Godot;
using System;

public partial class DashTrail : GpuParticles2D
{
	private CharacterController _player;
	private Vector2 _currentPosition;
	private float _lerpScale;
	private Vector2 _offset = new Vector2(-10, -5);
	
	override public void _Ready()
	{
		_player = GetParent<CharacterController>();
		_currentPosition = _player.GlobalPosition + _player.GetNode<AnimatedSprite2D>("PlayerSprite").Offset + _offset;
		_lerpScale = (float)(1f / (Amount / (60f * Lifetime)));
		if (_player.Velocity.X < 0)
		{
			_offset.X *= -1;
		}
	}
	
	override public void _Process(double delta)
	{
		GlobalPosition = new Vector2((float)Mathf.Lerp(GlobalPosition.X, _currentPosition.X, _lerpScale), (float)Mathf.Lerp(GlobalPosition.Y, _currentPosition.Y, _lerpScale));
	}
	
	override public void _PhysicsProcess(double delta)
	{
		GlobalPosition = _currentPosition + _offset;
		_currentPosition = _player.GlobalPosition + _player.GetNode<AnimatedSprite2D>("PlayerSprite").Offset + _offset;
	}
	
	public void OnFinished()
	{
		QueueFree();
	}
}
