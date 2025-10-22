using Godot;
using System;

public abstract partial class Weapon : Node2D
{
	abstract public Vector2 Attack();
	
	abstract public int GetAmmo();
	abstract public void SetAmmo(int ammo);
	
	abstract public int GetMaxAmmo();
}
