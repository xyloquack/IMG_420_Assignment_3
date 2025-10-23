using Godot;
using System;

public abstract partial class Weapon : Node2D
{
	[Signal]
	public delegate void AttackedEventHandler(Vector2 velocityChange);

	public Vector2 ParentPosition;
	public bool ParentFlipped;
	public Node WorldScene;

	abstract public void Attack();
	
	virtual public int GetAmmo() { return 1; }
	virtual public void SetAmmo(int ammo) { }
	virtual public int GetMaxAmmo() { return 1; }
}
