using Godot;
using System;

public partial class Audio : AudioStreamPlayer2D
{
	public string AudioName;
	public AudioController Controller;
	
	private bool _removed = false;
	
	override public void _Ready()
	{
		Finished += OnAudioFinished;
	}
	
	public void OnAudioFinished()
	{
		if (!_removed)
		{
			_removed = true;
			Controller.RemoveAudio(AudioName);
		}
	}
	
	override public void _ExitTree()
	{
		if (!_removed)
		{
			_removed = true;
			Controller.RemoveAudio(AudioName);
		}
	}
}
