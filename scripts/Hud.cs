using Godot;
using System;

public partial class Hud : CanvasLayer
{
	public CharacterController Player;
	private Sprite2D _cursor;
	private ShaderMaterial _shaderMat;
	
	override public void _Ready()
	{
		Player = GetParent<CharacterController>();
		_cursor = GetNode<Sprite2D>("Cursor");
		_shaderMat = (ShaderMaterial)_cursor.Material;
	}
	
	override public void _Process(double delta)
	{
		_cursor.GlobalPosition = _cursor.GetGlobalMousePosition();
	}
	
	override public void _PhysicsProcess(double delta)
	{
		float percentFull = (float)(((float)Player.NumBoids / (float)Player.MaxBoids));
		GD.Print(percentFull);
		_shaderMat.SetShaderParameter("percentFull", percentFull);
	}
}
