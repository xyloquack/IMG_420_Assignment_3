using Godot;
using System;

public partial class DustKickup : AnimatedSprite2D
{
	public void OnFinished()
	{
		QueueFree();
	}
}
