using Godot;

public partial class GameOverScreen : CanvasLayer
{
	private VideoStreamPlayer _video = null!;
	private Control _restartPrompt = null!;
	private ColorRect _fadeOverlay = null!;
	private bool _videoFinished;
	private bool _restartStarted;

	public override void _EnterTree()
	{
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		_video = GetNode<VideoStreamPlayer>("Screen/Video");
		_restartPrompt = GetNode<Control>("Screen/RestartPrompt");
		_fadeOverlay = GetNode<ColorRect>("Screen/FadeOverlay");

		if (_video.Stream == null)
		{
			GD.PushError("GameOverScreen has no video stream assigned.");
			return;
		}

		GetNode<MusicManager>("/root/MusicManager").StopMusic();
		_video.Finished += OnVideoFinished;
		_video.Play();
		FadeInVideo();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey key
			|| key.Keycode != Key.Space
			|| !key.Pressed
			|| key.Echo)
		{
			return;
		}

		GetViewport().SetInputAsHandled();
		if (!_videoFinished || _restartStarted)
		{
			return;
		}

		_restartStarted = true;
		_fadeOverlay.Visible = true;
		RestartCurrentLevel();
	}

	private async void FadeInVideo()
	{
		Tween fade = CreateTween();
		fade.TweenProperty(_fadeOverlay, "color:a", 0.0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(fade, Tween.SignalName.Finished);
		_fadeOverlay.Visible = false;
	}

	private async void RestartCurrentLevel()
	{
		Tween fade = CreateTween();
		fade.TweenProperty(_fadeOverlay, "color:a", 1.0f, 0.6f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(fade, Tween.SignalName.Finished);

		GetTree().Paused = false;
		Error error = GetTree().ReloadCurrentScene();
		if (error != Error.Ok)
		{
			GetTree().Paused = true;
			_restartStarted = false;
			GD.PushError($"GameOverScreen could not reload the current scene: {error}");

			Tween fadeIn = CreateTween();
			fadeIn.TweenProperty(_fadeOverlay, "color:a", 0.0f, 0.3f)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.InOut);
			await ToSignal(fadeIn, Tween.SignalName.Finished);
			_fadeOverlay.Visible = false;
		}
	}

	private void OnVideoFinished()
	{
		_videoFinished = true;
		_restartPrompt.Visible = true;
	}
}
