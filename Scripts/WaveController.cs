using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class WaveController : Node2D
{
	private const string SplattersPath = "res://Music/Splatters.wav";
	private const string FinalBossMusicPath = "res://Music/The Power in my Hands (Full).wav";
	private const string DeathPath = "res://Music/Death.wav";
	private const string CastleScenePath = "res://Scenes/castle.tscn";
	private const string OutsideDarkScenePath = "res://Scenes/outside_dark.tscn";
	private const string FinalScenePath = "res://Scenes/final_scene.tscn";
	private const float MusicFadeDuration = 1.0f;
	private const float CastleDoorDistance = 48.0f;
	private const float CastleExitDistance = 48.0f;
	private const float RainbowSpawnChance = 0.1f;
	private const float MinimumSpawnDistance = 56.0f;
	private const float PlayerSpawnClearance = 110.0f;
	private const float SpawnRetryDelay = 0.5f;
	private const int SpawnAttemptsPerEntity = 100;
	private static readonly int[] SlimesPerWave = [3, 5, 7];
	private static readonly Vector2 CastleDoorPosition = new(0.0f, -300.0f);
	private static readonly Vector2 CastleExitPosition = new(0.0f, 403.0f);

	[Export]
	public PackedScene SlimeScene { get; set; } = null!;

	[Export]
	public PackedScene CollectibleScene { get; set; } = null!;

	[Export]
	public Godot.Collections.Array<AtlasTexture> CollectibleTexturesByEffect { get; set; } = new();

	[Export]
	public Rect2 SpawnBounds { get; set; } = new(-520.0f, -405.0f, 1040.0f, 810.0f);

	[Export]
	public Godot.Collections.Array<Rect2> SpawnRegions { get; set; } = new();

	[Export(PropertyHint.Range, "0.5,10,0.5")]
	public float WaveIntervalSeconds { get; set; } = 2.0f;

	[Export]
	public bool PlayOutsideMusic { get; set; } = true;

	[Export]
	public bool TransitionToCastleOnCompletion { get; set; } = true;

	[Export]
	public string WavesCompleteObjectiveText { get; set; } = "Vá para o castelo";

	[Export]
	public string WaveCollectibleLabel { get; set; } = "os itens";

	[Export]
	public bool InteractWithEmmiAfterBoss { get; set; }

	[Export]
	public PackedScene EmmiScene { get; set; } = null!;

	[Export(PropertyHint.Range, "1,200,1")]
	public float EmmiInteractionRange { get; set; } = 48.0f;

	[Export]
	public bool InteractWithSayoriAfterWaves { get; set; }

	[Export(PropertyHint.Range, "1,200,1")]
	public float SayoriInteractionRange { get; set; } = 48.0f;

	[Export(PropertyHint.Range, "0,7,1")]
	public int EnhancedSlimeCountPerWave { get; set; }

	[Export(PropertyHint.Range, "0,20,1")]
	public int EnhancedSlimeHealthBonus { get; set; } = 1;

	[Export(PropertyHint.Range, "1.0,2.0,0.05")]
	public float EnhancedSlimeScale { get; set; } = 1.15f;

	[Export]
	public bool SpawnFinalWaveBoss { get; set; }

	[Export(PropertyHint.Range, "1,20,1")]
	public int FinalBossHealth { get; set; } = 20;

	[Export(PropertyHint.Range, "1.0,3.0,0.1")]
	public float FinalBossScale { get; set; } = 2.0f;

	[Export(PropertyHint.Range, "1,100,1")]
	public float FinalBossMoveSpeed { get; set; } = 18.0f;

	[Export(PropertyHint.Range, "0,100,1")]
	public float FinalBossWanderSpeed { get; set; } = 8.0f;

	[Export(PropertyHint.Range, "0.1,10,0.1")]
	public float FinalBossAttackCooldownSeconds { get; set; } = 1.2f;

	private CollectibleTracker _collectibleTracker = null!;
	private InventoryHUD _inventoryHud = null!;
	private Player _player = null!;
	private CatFollower _sayoriCat = null!;
	private DialogueController _dialogueController = null!;
	private Node2D _actors = null!;
	private CircleShape2D _spawnProbeShape = null!;
	private readonly RandomNumberGenerator _random = new();
	private readonly List<Collectible.EffectType> _effectBag = new();
	private int _remainingSlimes;
	private int _remainingItems;
	private int _waveNumber;
	private float _waveDelayRemaining;
	private float _spawnRetryRemaining;
	private Collectible.EffectType _lastCommonEffect;
	private bool _rainbowSpawnedThisWave;
	private bool _waveStartPending;
	private bool _spawnFailureReported;
	private bool _waveActive;
	private bool _betweenWaves;
	private bool _wavesComplete;
	private bool _transitioningScene;
	private bool _sayoriDialogueStarted;
	private bool _sayoriDialogueCompleted;
	private Node2D _emmi = null!;
	private bool _emmiDialogueStarted;

	public override void _Ready()
	{
		if (SlimeScene == null
			|| CollectibleScene == null
			|| CollectibleTexturesByEffect.Count != 5)
		{
			GD.PushError("WaveController requires slime/collectible scenes and five effect-mapped item textures.");
			return;
		}

		if (SpawnBounds.Size.X <= 0.0f
			|| SpawnBounds.Size.Y <= 0.0f
			|| WaveIntervalSeconds <= 0.0f
			|| string.IsNullOrWhiteSpace(WaveCollectibleLabel)
			|| EnhancedSlimeCountPerWave < 0
			|| EnhancedSlimeHealthBonus < 0
			|| !float.IsFinite(EnhancedSlimeScale)
			|| EnhancedSlimeScale <= 0.0f
			|| (SpawnFinalWaveBoss
				&& (FinalBossHealth < 1
					|| !float.IsFinite(FinalBossScale)
					|| FinalBossScale <= 0.0f
					|| !float.IsFinite(FinalBossMoveSpeed)
					|| FinalBossMoveSpeed <= 0.0f
					|| !float.IsFinite(FinalBossWanderSpeed)
					|| FinalBossWanderSpeed < 0.0f
					|| !float.IsFinite(FinalBossAttackCooldownSeconds)
					|| FinalBossAttackCooldownSeconds <= 0.0f))
			|| (InteractWithSayoriAfterWaves && SayoriInteractionRange <= 0.0f))
		{
			GD.PushError("WaveController has invalid wave, spawn, or enhanced slime settings.");
			return;
		}

		if (InteractWithEmmiAfterBoss
			&& (!SpawnFinalWaveBoss
				|| EmmiScene == null
				|| !float.IsFinite(EmmiInteractionRange)
				|| EmmiInteractionRange <= 0.0f))
		{
			GD.PushError("WaveController requires a final boss, Emmi scene, and valid interaction range.");
			return;
		}

		foreach (Rect2 region in SpawnRegions)
		{
			if (region.Size.X <= 0.0f
				|| region.Size.Y <= 0.0f
				|| region.Position.X < SpawnBounds.Position.X
				|| region.Position.Y < SpawnBounds.Position.Y
				|| region.End.X > SpawnBounds.End.X
				|| region.End.Y > SpawnBounds.End.Y)
			{
				GD.PushError("WaveController has an invalid spawn region outside SpawnBounds.");
				return;
			}
		}

		_collectibleTracker = GetNode<CollectibleTracker>("CollectibleTracker");
		_collectibleTracker.CollectibleCollected += OnCollectibleCollected;
		int totalCollectibles = 0;
		foreach (int waveSize in SlimesPerWave)
		{
			totalCollectibles += waveSize;
		}

		_collectibleTracker.SetExpectedTotal(totalCollectibles);
		_inventoryHud = GetTree().GetFirstNodeInGroup("inventory_hud") as InventoryHUD;
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		_actors = GetNode<Node2D>("Actors");
		if (InteractWithSayoriAfterWaves)
		{
			_sayoriCat = GetNodeOrNull<CatFollower>("Actors/cat_sayori")!;
			_dialogueController =
				GetTree().GetFirstNodeInGroup("dialogue_controller") as DialogueController;
			if (_sayoriCat == null
				|| _dialogueController == null
				|| _dialogueController.Dialogue == null
				|| _dialogueController.Dialogue.Lines.Count == 0)
			{
				GD.PushError(
					"WaveController requires Sayori and a configured dialogue for post-wave interaction."
				);
				return;
			}

			_dialogueController.DialogueFinished += OnSayoriDialogueFinished;
		}

		if (InteractWithEmmiAfterBoss)
		{
			if (_dialogueController == null)
			{
				_dialogueController =
					GetTree().GetFirstNodeInGroup("dialogue_controller") as DialogueController;
			}

			if (_dialogueController == null
				|| _dialogueController.Dialogue == null
				|| _dialogueController.Dialogue.Lines.Count == 0)
			{
				GD.PushError(
					"WaveController requires a configured dialogue for the post-boss Emmi interaction."
				);
				return;
			}

			_dialogueController.DialogueFinished += OnEmmiDialogueFinished;
		}

		_spawnProbeShape = new CircleShape2D
		{
			Radius = 14.0f,
		};
		_random.Randomize();

		if (_inventoryHud == null)
		{
			GD.PushError("WaveController could not find the game HUD.");
		}

		if (_player == null)
		{
			GD.PushError("WaveController could not find the player.");
			return;
		}

		if (PlayOutsideMusic)
		{
			_ = GetNode<MusicManager>("/root/MusicManager")
				.PlayTrackWithFadeIn(SplattersPath, MusicFadeDuration);
		}

		_waveStartPending = true;
	}

	public override void _Process(double delta)
	{
		if (_transitioningScene || _player == null)
		{
			return;
		}

		if (InteractWithEmmiAfterBoss && _emmi != null)
		{
			if (!_emmiDialogueStarted
				&& Input.IsActionJustPressed("interact")
				&& _player.GlobalPosition.DistanceTo(_emmi.GlobalPosition)
					<= EmmiInteractionRange)
			{
				_emmiDialogueStarted = _dialogueController.StartConfiguredDialogue();
			}

			return;
		}

		if (!_wavesComplete)
		{
			return;
		}

		if (InteractWithSayoriAfterWaves)
		{
			if (!_sayoriDialogueCompleted)
			{
				if (!_sayoriDialogueStarted
					&& Input.IsActionJustPressed("interact")
					&& _player.GlobalPosition.DistanceTo(_sayoriCat.GlobalPosition)
						<= SayoriInteractionRange)
				{
					_sayoriDialogueStarted = _dialogueController.StartConfiguredDialogue();
				}

				return;
			}

			if (Input.IsActionJustPressed("interact")
				&& _player.GlobalPosition.DistanceTo(CastleExitPosition) <= CastleExitDistance)
			{
				_transitioningScene = true;
				_ = TransitionToScene(OutsideDarkScenePath);
			}

			return;
		}

		if (!TransitionToCastleOnCompletion || !Input.IsActionJustPressed("interact"))
		{
			return;
		}

		if (_player.GlobalPosition.DistanceTo(CastleDoorPosition) <= CastleDoorDistance)
		{
			_transitioningScene = true;
			_ = TransitionToScene(CastleScenePath);
		}
	}

	private void OnSayoriDialogueFinished()
	{
		if (!_sayoriDialogueStarted || _sayoriDialogueCompleted)
		{
			return;
		}

		_sayoriDialogueCompleted = true;
		_inventoryHud?.SetObjectiveText(
			"Vá até a parte de baixo do castelo e aperte E para sair.",
			showOverlay: true
		);
	}

	private void OnEmmiDialogueFinished()
	{
		if (!_emmiDialogueStarted || _transitioningScene)
		{
			return;
		}

		_transitioningScene = true;
		_ = TransitionToScene(FinalScenePath);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_waveStartPending)
		{
			_spawnRetryRemaining -= (float)delta;
			if (_spawnRetryRemaining > 0.0f)
			{
				return;
			}

			_waveStartPending = false;
			StartNextWave();
			return;
		}

		if (!_betweenWaves)
		{
			return;
		}

		_waveDelayRemaining -= (float)delta;
		if (_waveDelayRemaining <= 0.0f)
		{
			_betweenWaves = false;
			StartNextWave();
		}
	}

	private void OnSlimeDefeated()
	{
		if (!_waveActive || _remainingSlimes <= 0)
		{
			return;
		}

		_remainingSlimes--;
		CheckWaveCompletion();
	}

	private void OnCollectibleCollected(Collectible collectible)
	{
		if (!_waveActive || _remainingItems <= 0)
		{
			return;
		}

		_remainingItems--;
		CheckWaveCompletion();
	}

	private void StartNextWave()
	{
		if (_waveNumber >= SlimesPerWave.Length)
		{
			GD.PushError("WaveController attempted to start a wave past the final wave.");
			return;
		}

		int entityCount = SlimesPerWave[_waveNumber];
		if (!TryGetSpawnPositions(entityCount * 2, out List<Vector2> positions))
		{
			if (!_spawnFailureReported)
			{
				GD.PushError($"Could not find enough safe positions for wave {_waveNumber + 1}; retrying.");
				_spawnFailureReported = true;
			}

			_waveStartPending = true;
			_spawnRetryRemaining = SpawnRetryDelay;
			return;
		}

		_spawnFailureReported = false;
		_waveNumber++;
		_remainingSlimes = entityCount;
		_remainingItems = entityCount;
		_rainbowSpawnedThisWave = false;
		_waveActive = true;
		string objectiveText = _waveNumber == SlimesPerWave.Length && SpawnFinalWaveBoss
			? $"Onda {_waveNumber}/3: derrote os slimes, colete {WaveCollectibleLabel} e vença o chefe."
			: $"Onda {_waveNumber}/3: derrote os slimes e colete {WaveCollectibleLabel}.";
		_inventoryHud?.SetObjectiveText(objectiveText, showOverlay: false);

		for (int i = 0; i < entityCount; i++)
		{
			Collectible.EffectType effect = GetNextEffect();
			bool spawnBoss = SpawnFinalWaveBoss
				&& _waveNumber == SlimesPerWave.Length
				&& i == entityCount - 1;
			if (spawnBoss)
			{
				SpawnFinalBoss(positions[i]);
			}
			else
			{
				SpawnSlime(positions[i], i < EnhancedSlimeCountPerWave);
			}

			SpawnCollectible(
				positions[entityCount + i],
				effect
			);
		}
	}

	private void SpawnSlime(Vector2 position, bool enhanced)
	{
		SlimeEnemy slime = SlimeScene.Instantiate<SlimeEnemy>();
		slime.Position = position;
		if (enhanced)
		{
			slime.MaxHealth += EnhancedSlimeHealthBonus;
			slime.Scale = Vector2.One * EnhancedSlimeScale;
		}

		slime.Defeated += OnSlimeDefeated;
		_actors.AddChild(slime);
	}

	private void SpawnFinalBoss(Vector2 position)
	{
		SlimeEnemy slime = SlimeScene.Instantiate<SlimeEnemy>();
		slime.Position = position;
		slime.MaxHealth = FinalBossHealth;
		slime.MoveSpeed = FinalBossMoveSpeed;
		slime.WanderSpeed = FinalBossWanderSpeed;
		slime.AttackCooldownSeconds = FinalBossAttackCooldownSeconds;
		slime.Scale = Vector2.One * FinalBossScale;
		slime.Defeated += OnSlimeDefeated;
		if (InteractWithEmmiAfterBoss)
		{
			slime.TreeExiting += () => SpawnEmmiAt(slime.GlobalPosition);
		}

		_actors.AddChild(slime);
		if (PlayOutsideMusic)
		{
			_ = GetNode<MusicManager>("/root/MusicManager")
				.TransitionToTrack(FinalBossMusicPath, MusicFadeDuration);
		}
	}

	private void SpawnEmmiAt(Vector2 position)
	{
		if (_emmi != null || EmmiScene == null)
		{
			return;
		}

		_emmi = EmmiScene.Instantiate<Node2D>();
		_actors.AddChild(_emmi);
		_emmi.GlobalPosition = position;
		_inventoryHud?.SetObjectiveText(
			"Aproxime-se da Emmi e aperte E para conversar.",
			showOverlay: true
		);
	}

	private void SpawnCollectible(Vector2 position, Collectible.EffectType effect)
	{
		int textureIndex = (int)effect - (int)Collectible.EffectType.DamageBoost;
		AtlasTexture itemTexture = CollectibleTexturesByEffect[textureIndex];
		AtlasTexture slowdownTexture = CollectibleTexturesByEffect[
			(int)Collectible.EffectType.Slowdown - (int)Collectible.EffectType.DamageBoost
		];

		Collectible collectible = CollectibleScene.Instantiate<Collectible>();
		collectible.Position = position;
		collectible.ItemTexture = itemTexture;
		collectible.SlowdownTexture = slowdownTexture;
		collectible.Effect = effect;
		_actors.AddChild(collectible);
	}

	private bool TryGetSpawnPositions(int count, out List<Vector2> positions)
	{
		positions = new List<Vector2>(count);
		for (int entity = 0; entity < count; entity++)
		{
			bool positionFound = false;
			for (int attempt = 0; attempt < SpawnAttemptsPerEntity; attempt++)
			{
				Vector2 candidate = new(
					_random.RandfRange(SpawnBounds.Position.X, SpawnBounds.End.X),
					_random.RandfRange(SpawnBounds.Position.Y, SpawnBounds.End.Y)
				);

				if (!IsInsideSpawnRegions(candidate)
					|| _player.GlobalPosition.DistanceTo(candidate) < PlayerSpawnClearance
					|| IsTooCloseToReservedPosition(candidate, positions)
					|| IsSpawnBlocked(candidate))
				{
					continue;
				}

				positions.Add(candidate);
				positionFound = true;
				break;
			}

			if (!positionFound)
			{
				return false;
			}
		}

		return true;
	}

	private bool IsInsideSpawnRegions(Vector2 candidate)
	{
		foreach (Rect2 region in SpawnRegions)
		{
			if (region.HasPoint(candidate))
			{
				return true;
			}
		}

		return SpawnRegions.Count == 0;
	}

	private bool IsTooCloseToReservedPosition(Vector2 candidate, List<Vector2> positions)
	{
		foreach (Vector2 position in positions)
		{
			if (candidate.DistanceTo(position) < MinimumSpawnDistance)
			{
				return true;
			}
		}

		return false;
	}

	private bool IsSpawnBlocked(Vector2 candidate)
	{
		var query = new PhysicsShapeQueryParameters2D
		{
			Shape = _spawnProbeShape,
			Transform = new Transform2D(0.0f, candidate),
			CollisionMask = 1,
			CollideWithAreas = false,
			CollideWithBodies = true,
		};

		return GetWorld2D().DirectSpaceState.IntersectShape(query, 1).Count > 0;
	}

	private Collectible.EffectType GetNextEffect()
	{
		if (!_rainbowSpawnedThisWave && _random.Randf() < RainbowSpawnChance)
		{
			_rainbowSpawnedThisWave = true;
			return Collectible.EffectType.Rainbow;
		}

		if (_effectBag.Count == 0)
		{
			RefillEffectBag();
		}

		Collectible.EffectType effect = _effectBag[^1];
		_effectBag.RemoveAt(_effectBag.Count - 1);
		_lastCommonEffect = effect;
		return effect;
	}

	private void RefillEffectBag()
	{
		_effectBag.AddRange(
		[
			Collectible.EffectType.DamageBoost,
			Collectible.EffectType.SpeedBoost,
			Collectible.EffectType.HealthIncrease,
			Collectible.EffectType.Slowdown
		]);

		for (int i = _effectBag.Count - 1; i > 0; i--)
		{
			int swapIndex = _random.RandiRange(0, i);
			(_effectBag[i], _effectBag[swapIndex]) = (_effectBag[swapIndex], _effectBag[i]);
		}

		if (_effectBag.Count > 1 && _effectBag[^1] == _lastCommonEffect)
		{
			(_effectBag[^1], _effectBag[0]) = (_effectBag[0], _effectBag[^1]);
		}
	}

	private void CheckWaveCompletion()
	{
		if (!_waveActive || _remainingSlimes > 0 || _remainingItems > 0)
		{
			return;
		}

		_waveActive = false;
		if (_waveNumber == SlimesPerWave.Length)
		{
			_wavesComplete = true;
			_inventoryHud?.SetObjectiveText(WavesCompleteObjectiveText, showOverlay: true);
			if (PlayOutsideMusic)
			{
				_ = GetNode<MusicManager>("/root/MusicManager")
					.TransitionToTrack(DeathPath, MusicFadeDuration);
			}

			return;
		}

		_betweenWaves = true;
		_waveDelayRemaining = WaveIntervalSeconds;
		_inventoryHud?.SetObjectiveText(
			$"Onda {_waveNumber} concluída. Próxima onda em breve.",
			showOverlay: false
		);
	}

	private async Task TransitionToScene(string scenePath)
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

		Error error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			_transitioningScene = false;
			GD.PushError($"Could not open scene '{scenePath}': {error}");
		}
	}
}
