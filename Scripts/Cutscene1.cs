using Godot;

public partial class Cutscene1 : Control
{
	private VideoStreamPlayer _video = null!;
	private ColorRect _fade = null!;

	public override void _Ready()
	{
		GetNode<MusicManager>("/root/MusicManager").StopMusic();

		_video = GetNode<VideoStreamPlayer>("AspectRatioContainer/Video");
		_fade = GetNode<ColorRect>("FadeOverlay");
		_video.TextureFilter = CanvasItem.TextureFilterEnum.Linear;

		if (_video.Stream == null)
		{
			GD.PushError("Cutscene1 has no video stream assigned.");
			return;
		}

		_video.Finished += OnVideoFinished;
		_video.Play();

		Tween tween = CreateTween();
		tween.TweenProperty(_fade, "color:a", 0.0f, 1.2f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private async void OnVideoFinished()
	{
		Tween fadeOut = CreateTween();
		fadeOut.TweenProperty(_fade, "color:a", 1.0f, 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		const string outsidePath = "res://Scenes/outside.tscn";
		Error error = GetTree().ChangeSceneToFile(outsidePath);
		if (error != Error.Ok)
		{
			GD.PushError($"Could not open outside scene '{outsidePath}': {error}");
		}
	}
}
