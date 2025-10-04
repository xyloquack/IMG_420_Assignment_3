using Godot;
using System;

public partial class HurtBox : Area2D
{
	[Signal]
	private delegate void DamageEventHandler(float damage);
}
