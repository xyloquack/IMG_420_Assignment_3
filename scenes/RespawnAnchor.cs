using Godot;
using System;

public partial class RespawnAnchor : Area2D
{
	public void OnEnter(Node2D body)
	{
		if (body.IsInGroup("player"))
		{
			body.EmitSignal("SetRespawn", GlobalPosition);
		}
	}
}
