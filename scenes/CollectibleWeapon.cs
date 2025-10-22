using Godot;
using System;

public partial class CollectibleWeapon : Node2D
{
	[Export]
	public PackedScene WeaponScene { get; set;}
	[Export]
	public Texture2D Icon;
	
	override public void _Ready()
	{
		GetNode<Sprite2D>("Sprite2D").Texture = Icon;
	}
	
	private void OnBodyEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			CharacterController player = (CharacterController)body;
			player.EmitSignal("EquipWeapon", WeaponScene);
			QueueFree();
		}
	}
}
