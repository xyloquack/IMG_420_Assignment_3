using Godot;
using System;

public partial class Audio : AudioStreamPlayer2D
{
	public string AudioName;
	public AudioController Controller;
	
	override public void _Ready()
	{
		Finished += OnAudioFinished;
	}
	
	public void OnAudioFinished()
	{
		Controller.RemoveAudio(AudioName);
	}
}
