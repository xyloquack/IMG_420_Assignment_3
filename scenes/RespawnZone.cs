using Godot;
using System;

public partial class RespawnZone : Area2D
{
	private void OnEnter(Node2D body)
	{
		if (body.IsInGroup("player"))
		{
			body.EmitSignal("Respawn");
		}
	}
}
