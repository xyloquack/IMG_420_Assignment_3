using Godot;
using System;

public partial class TelegraphParticles : GpuParticles2D
{
	private void OnFinished()
	{
		QueueFree();
	}
}
