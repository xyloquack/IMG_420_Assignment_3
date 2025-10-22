using Godot;
using System;

public partial class MainMenu : Control
{
	public float TimePassed = 0;
	private Camera2D _camera;
	private Sprite2D _cursor;
	
	override public void _Ready()
	{
		Input.SetMouseMode(Input.MouseModeEnum.Hidden);
		_camera = GetNode<Camera2D>("Camera2D");
		_cursor = GetNode<Sprite2D>("Cursor/Cursor");
	}
	
	override public void _Process(double delta)
	{
		_cursor.GlobalPosition = _cursor.GetGlobalMousePosition();
	}
	
	override public void _PhysicsProcess(double delta)
	{
		TimePassed += (float)delta;
		float zoomAmount = (float)((Math.Sin(TimePassed) + 1) * 0.05 + 0.9);
		Vector2 newZoom = new Vector2(zoomAmount, zoomAmount);
		_camera.Zoom = newZoom;
	}
	
	private void OnPlay()
	{
		GetTree().ChangeSceneToFile("uid://c7f8c54cffku6");
	}
	
	private void OnQuit()
	{
		GetTree().Quit();
	}
}
