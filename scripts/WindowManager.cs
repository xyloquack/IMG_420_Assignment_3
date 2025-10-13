using Godot;
using System;

public partial class WindowManager : Node
{
	private bool _fullscreen = false;
	
	override public void _Process(double delta)
	{
		if (Input.IsActionJustPressed("fullscreen"))
		{
			if (_fullscreen)
			{
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
				_fullscreen = false;
			}
			else
			{
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
				_fullscreen = true;
			}
		}
	}
}
