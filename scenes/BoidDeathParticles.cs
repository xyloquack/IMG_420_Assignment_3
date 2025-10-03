using Godot;
using System;

public partial class BoidDeathParticles : CpuParticles2D
{
	public void OnFinished()
	{
		QueueFree();
	}
}
