using Godot;

public partial class FinalSceneMusic : Node
{
	private const string MusicPath = "res://Music/I'll be Here (Full).wav";
	private const float MusicFadeDuration = 1.0f;
	private MusicManager _musicManager = null!;

	public override void _Ready()
	{
		_musicManager = GetNode<MusicManager>("/root/MusicManager");
		VideoStreamPlayer video = GetParent()
			.GetNode<VideoStreamPlayer>("AspectRatioContainer/Video");
		video.Finished += OnVideoFinished;
		_ = _musicManager.TransitionToTrack(MusicPath, MusicFadeDuration);
	}

	private void OnVideoFinished()
	{
		_ = _musicManager.FadeOut(MusicFadeDuration);
	}
}
