using Godot;
using System;
using System.Collections.Generic;

public partial class AudioController : Node2D
{
	private const int MAX_AUDIO_PER_TYPE = 20; 
	
	Dictionary<string, Resource> audioDictionary = new Dictionary<string, Resource>();
	Dictionary<string, int> audioCounts = new Dictionary<string, int>();
	
	override public void _Ready()
	{
		audioDictionary.Add("BoidWhooshAudio", GD.Load("uid://b0fofkly8bdp1"));
		audioCounts.Add("BoidWhooshAudio", 0);
		audioDictionary.Add("BoidDeathAudio", GD.Load("uid://d2n7yym8kdv7x"));
		audioCounts.Add("BoidDeathAudio", 0);
	}
	
	public float RequestAudio(string audioName, float volumeDb, Node parentNode)
	{
		if (audioCounts[audioName] < MAX_AUDIO_PER_TYPE)
		{
			Audio audio = new Audio();
			audio.AudioName = audioName;
			audio.Controller = this;
			audio.Stream = (AudioStream)audioDictionary[audioName];
			audio.PitchScale = 0.90f + (GD.Randf() * 0.2f);
			audio.Attenuation = 9f;
			parentNode.AddChild(audio);
			audio.Position = Vector2.Zero;
			audio.Play();
			audioCounts[audioName]++;
			return (float)audio.Stream.GetLength();
		}
		return 0f;
	}
	
	public void RemoveAudio(string audioName)
	{
		audioCounts[audioName]--;
	}
}
