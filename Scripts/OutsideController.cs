using System.Threading.Tasks;
using Godot;

public partial class OutsideController : Node2D
{
	private const string SplattersPath = "res://Music/Splatters.wav";
	private const string DeathPath = "res://Music/Death.wav";
	private const string CastleScenePath = "res://Scenes/castle.tscn";
	private const float MusicFadeDuration = 1.0f;
	private const float CastleDoorDistance = 48.0f;
	private static readonly Vector2 CastleDoorPosition = new(0.0f, -300.0f);

	private CollectibleTracker _collectibleTracker = null!;
	private InventoryHUD _inventoryHud = null!;
	private Player _player = null!;
	private int _remainingSlimes;
	private bool _flowersCollected;
	private bool _transitioningToCastle;

	public override void _Ready()
	{
		_collectibleTracker = GetNode<CollectibleTracker>("CollectibleTracker");
		_collectibleTracker.AllCollectiblesCollected += OnAllFlowersCollected;
		_collectibleTracker.SetCollectionEnabled(false);
		_inventoryHud = GetTree().GetFirstNodeInGroup("inventory_hud") as InventoryHUD;
		_player = GetTree().GetFirstNodeInGroup("player") as Player;

		if (_inventoryHud == null)
		{
			GD.PushError("OutsideController could not find the inventory HUD.");
		}

		if (_player == null)
		{
			GD.PushError("OutsideController could not find the player.");
		}

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is SlimeEnemy slime)
			{
				_remainingSlimes++;
				slime.Defeated += OnSlimeDefeated;
			}
		}

		MusicManager music = GetNode<MusicManager>("/root/MusicManager");
		if (_remainingSlimes == 0)
		{
			_collectibleTracker.SetCollectionEnabled(true);
			_ = music.PlayTrackWithFadeIn(DeathPath, MusicFadeDuration);
			return;
		}

		_ = music.PlayTrackWithFadeIn(SplattersPath, MusicFadeDuration);
	}

	public override void _Process(double delta)
	{
		if (!_flowersCollected
			|| _transitioningToCastle
			|| _player == null
			|| !Input.IsActionJustPressed("interact"))
		{
			return;
		}

		if (_player.GlobalPosition.DistanceTo(CastleDoorPosition) <= CastleDoorDistance)
		{
			_transitioningToCastle = true;
			_ = TransitionToCastle();
		}
	}

	private void OnSlimeDefeated()
	{
		_remainingSlimes--;
		if (_remainingSlimes > 0)
		{
			return;
		}

		_collectibleTracker.SetCollectionEnabled(true);
		_ = GetNode<MusicManager>("/root/MusicManager")
			.TransitionToTrack(DeathPath, MusicFadeDuration);
	}

	private void OnAllFlowersCollected()
	{
		_flowersCollected = true;
		_inventoryHud?.SetObjectiveText("Vá para o castelo", showOverlay: true);
	}

	private async Task TransitionToCastle()
	{
		var fadeLayer = new CanvasLayer
		{
			Layer = 100,
		};
		var fade = new ColorRect
		{
			Color = new Color(0.0f, 0.0f, 0.0f, 0.0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		fadeLayer.AddChild(fade);
		AddChild(fadeLayer);

		Tween tween = CreateTween();
		tween.TweenProperty(fade, "color:a", 1.0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(tween, Tween.SignalName.Finished);

		Error error = GetTree().ChangeSceneToFile(CastleScenePath);
		if (error != Error.Ok)
		{
			_transitioningToCastle = false;
			GD.PushError($"Could not open castle scene '{CastleScenePath}': {error}");
		}
	}
}
