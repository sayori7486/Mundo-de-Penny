using System.Threading.Tasks;
using Godot;

public partial class MusicManager : Node
{
	private const string MusicPath = "res://Music/Doll Dance.wav";

	private AudioStreamPlayer _musicPlayer = null!;
	private bool _shouldLoop;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_musicPlayer = new AudioStreamPlayer
		{
			ProcessMode = ProcessModeEnum.Always,
		};
		AddChild(_musicPlayer);
		_musicPlayer.Finished += OnMusicFinished;
	}

	public void PlayDolldance()
	{
		if (_musicPlayer == null)
		{
			GD.PushError("MusicManager is not ready to play music.");
			return;
		}

		if (_musicPlayer.Playing
			&& _musicPlayer.Stream != null
			&& _musicPlayer.Stream.ResourcePath == MusicPath)
		{
			_shouldLoop = true;
			return;
		}

		if (!LoadTrack(MusicPath))
		{
			return;
		}

		_shouldLoop = true;
		_musicPlayer.VolumeDb = 0.0f;
		_musicPlayer.Play();
	}

	public void StopMusic()
	{
		_shouldLoop = false;
		if (_musicPlayer != null)
		{
			_musicPlayer.Stop();
			_musicPlayer.VolumeDb = 0.0f;
		}
	}

	public async Task PlayTrackWithFadeIn(string path, float duration)
	{
		if (!LoadTrack(path))
		{
			return;
		}

		_shouldLoop = true;
		_musicPlayer.VolumeDb = -80.0f;
		_musicPlayer.Play();
		await FadeVolumeTo(0.0f, duration);
	}

	public async Task TransitionToTrack(string path, float duration)
	{
		await FadeOut(duration);
		await PlayTrackWithFadeIn(path, duration);
	}

	public async Task FadeOut(float duration)
	{
		_shouldLoop = false;
		if (_musicPlayer == null || !_musicPlayer.Playing)
		{
			return;
		}

		await FadeVolumeTo(-80.0f, duration);

		_musicPlayer.Stop();
		_musicPlayer.VolumeDb = 0.0f;
	}

	private bool LoadTrack(string path)
	{
		if (_musicPlayer == null)
		{
			GD.PushError("MusicManager is not ready to play music.");
			return false;
		}

		AudioStream music = GD.Load<AudioStream>(path);
		if (music == null)
		{
			GD.PushError($"MusicManager could not load '{path}'.");
			return false;
		}

		_musicPlayer.Stop();
		_musicPlayer.Stream = music;
		return true;
	}

	private async Task FadeVolumeTo(float volumeDb, float duration)
	{
		Tween fade = CreateTween();
		fade.TweenProperty(
			_musicPlayer,
			"volume_db",
			volumeDb,
			Mathf.Max(0.01f, duration)
		).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		await ToSignal(fade, Tween.SignalName.Finished);
	}

	private void OnMusicFinished()
	{
		if (_shouldLoop)
		{
			_musicPlayer.Play();
		}
	}
}
