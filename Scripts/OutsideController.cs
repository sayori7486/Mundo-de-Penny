using Godot;

public partial class OutsideController : Node2D
{
	private const string SplattersPath = "res://Music/Splatters.wav";
	private const string DeathPath = "res://Music/Death.wav";
	private const float MusicFadeDuration = 1.0f;

	private CollectibleTracker _collectibleTracker = null!;
	private int _remainingSlimes;

	public override void _Ready()
	{
		_collectibleTracker = GetNode<CollectibleTracker>("CollectibleTracker");
		_collectibleTracker.SetCollectionEnabled(false);

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
}
