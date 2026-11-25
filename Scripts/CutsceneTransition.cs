using Godot;

public partial class CutsceneTransition : CanvasLayer
{
	private const string CutscenePath = "res://Scenes/cutscene1.tscn";
	private ColorRect _fade = null!;
	private bool _transitionStarted;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_fade = new ColorRect
		{
			Color = new Color(0.0f, 0.0f, 0.0f, 0.0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_fade);

		CollectibleTracker tracker = GetParent().GetNode<CollectibleTracker>("CollectibleTracker");
		tracker.AllCollectiblesCollected += OnAllCollectiblesCollected;
	}

	private void OnAllCollectiblesCollected()
	{
		if (_transitionStarted)
		{
			return;
		}

		_transitionStarted = true;

		if (GetTree().GetFirstNodeInGroup("player") is Player player)
		{
			player.SetPhysicsProcess(false);
			player.SetProcess(false);
		}

		FadeToCutscene();
	}

	private async void FadeToCutscene()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_fade, "color:a", 1.0f, 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(tween, Tween.SignalName.Finished);

		Error error = GetTree().ChangeSceneToFile(CutscenePath);
		if (error != Error.Ok)
		{
			GD.PushError($"Could not open cutscene scene '{CutscenePath}': {error}");
		}
	}
}
