using Godot;
using System;

public partial class DustKickup : GpuParticles2D
{
	public void OnFinished()
	{
		QueueFree();
	}
}
