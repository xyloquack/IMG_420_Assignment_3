using Godot;
using System;

public partial class BoidDeathParticles : GpuParticles2D
{
	public void OnFinished()
	{
		QueueFree();
	}
}
