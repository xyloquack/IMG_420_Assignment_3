using Godot;
using System;

public partial class Hud : CanvasLayer
{
	public CharacterController Player;
	private Sprite2D _cursor;
	private Sprite2D _innerHealth;
	private ShaderMaterial _cursorShaderMat;
	private ShaderMaterial _innerHealthShaderMat;
	
	override public void _Ready()
	{
		Player = GetParent<CharacterController>();
		_cursor = GetNode<Sprite2D>("Cursor");
		_innerHealth = GetNode<Sprite2D>("Health/InnerHealth");
		_cursorShaderMat = (ShaderMaterial)_cursor.Material;
		_innerHealthShaderMat = (ShaderMaterial)_innerHealth.Material;
	}
	
	override public void _Process(double delta)
	{
		_cursor.GlobalPosition = _cursor.GetGlobalMousePosition();
	}
	
	override public void _PhysicsProcess(double delta)
	{
		float cursorPercentFull = (float)(((float)Player.GetAmmo() / (float)Player.GetMaxAmmo()));
		float healthPercentFull = (float)(((float)Player.Health / (float)Player.MaxHealth));
		_cursorShaderMat.SetShaderParameter("percentFull", cursorPercentFull);
		_innerHealthShaderMat.SetShaderParameter("healthPercent", healthPercentFull);
	}
}
