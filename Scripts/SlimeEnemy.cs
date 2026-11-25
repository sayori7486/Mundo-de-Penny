using Godot;

public partial class SlimeEnemy : CharacterBody2D
{
	[Signal]
	public delegate void DefeatedEventHandler();

	[Export]
	public float MoveSpeed { get; set; } = 38.0f;

	[Export(PropertyHint.Range, "1,250,1")]
	public float DetectionRange { get; set; } = 100.0f;

	[Export(PropertyHint.Range, "1,400,1")]
	public float LoseRange { get; set; } = 150.0f;

	[Export(PropertyHint.Range, "1,300,1")]
	public float ItemDetectionRange { get; set; } = 90.0f;

	[Export(PropertyHint.Range, "1,100,1")]
	public float WanderSpeed { get; set; } = 18.0f;

	[Export(PropertyHint.Range, "0.5,10,0.5")]
	public float WanderMoveDurationMin { get; set; } = 1.0f;

	[Export(PropertyHint.Range, "0.5,10,0.5")]
	public float WanderMoveDurationMax { get; set; } = 3.0f;

	[Export(PropertyHint.Range, "0.1,10,0.1")]
	public float WanderPauseDurationMin { get; set; } = 0.5f;

	[Export(PropertyHint.Range, "0.1,10,0.1")]
	public float WanderPauseDurationMax { get; set; } = 1.5f;

	[Export]
	public float AttackRange { get; set; } = 30.0f;

	[Export]
	public float AttackCooldownSeconds { get; set; } = 0.6f;

	[Export(PropertyHint.Range, "1,20,1")]
	public int MaxHealth { get; set; } = 2;

	[Export(PropertyHint.Range, "0,8,1")]
	public int AttackHitFrame { get; set; } = 2;

	private AnimatedSprite2D _sprite = null!;
	private Player _player = null!;
	private string _facing = "down";
	private float _attackCooldownRemaining;
	private float _wanderTimeRemaining;
	private Vector2 _wanderDirection;
	private float _currentHealth;
	private float _originalSpriteAlpha;
	private Tween _hitFlashTween = null!;
	private bool _isAttacking;
	private bool _attackHitApplied;
	private bool _hasDetectedPlayer;
	private bool _isWandering;
	private bool _isDefeated;
	private readonly RandomNumberGenerator _random = new();

	public override void _EnterTree()
	{
		AddToGroup("enemies");
	}

	public override void _Ready()
	{
		if (MaxHealth < 1
			|| DetectionRange < 0.0f
			|| LoseRange <= DetectionRange
			|| ItemDetectionRange <= 0.0f
			|| WanderSpeed < 0.0f
			|| WanderMoveDurationMin < 0.0f
			|| WanderMoveDurationMax < WanderMoveDurationMin
			|| WanderPauseDurationMin < 0.0f
			|| WanderPauseDurationMax < WanderPauseDurationMin)
		{
			GD.PushError("SlimeEnemy has invalid detection or wandering settings.");
			return;
		}

		_random.Randomize();
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_currentHealth = MaxHealth;
		_originalSpriteAlpha = _sprite.Modulate.A;
		_sprite.AnimationFinished += OnAnimationFinished;
		_sprite.FrameChanged += OnAttackFrameChanged;

		foreach (string direction in new[] { "down", "left", "right", "up" })
		{
			_sprite.SpriteFrames.SetAnimationLoopMode(
				$"attack_{direction}",
				SpriteFrames.LoopMode.None
			);
		}

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
		{
			GD.PushError("SlimeEnemy could not find the player.");
			return;
		}

		PlayAnimation("idle_down");
		StartWanderMove();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDefeated || _player == null)
		{
			return;
		}

		_attackCooldownRemaining = Mathf.Max(
			0.0f,
			_attackCooldownRemaining - (float)delta
		);

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float distanceSquared = toPlayer.LengthSquared();

		if (!_hasDetectedPlayer)
		{
			if (distanceSquared > DetectionRange * DetectionRange)
			{
				if (TryPursueCollectible())
				{
					return;
				}

				ProcessWandering(delta);
				return;
			}

			_hasDetectedPlayer = true;
		}
		else if (distanceSquared > LoseRange * LoseRange)
		{
			_hasDetectedPlayer = false;
			_isAttacking = false;
			_attackHitApplied = false;
			_attackCooldownRemaining = 0.0f;
			StopMoving();
			StartWanderPause();
			PlayAnimation($"idle_{_facing}");
			return;
		}

		if (_isAttacking)
		{
			StopMoving();
			return;
		}

		if (toPlayer.Length() <= AttackRange)
		{
			StopMoving();
			if (_attackCooldownRemaining <= 0.0f)
			{
				StartAttack(toPlayer);
			}
			else
			{
				PlayAnimation($"idle_{_facing}");
			}

			return;
		}

		UpdateFacing(toPlayer);
		Velocity = toPlayer.Normalized() * MoveSpeed;
		MoveAndSlide();

		PlayAnimation($"walk_{_facing}");
	}

	public void TakeDamage(float damage = 1.0f)
	{
		if (_isDefeated)
		{
			return;
		}

		if (damage <= 0.0f || !float.IsFinite(damage))
		{
			GD.PushError("SlimeEnemy received invalid damage.");
			return;
		}

		_currentHealth = Mathf.Max(0.0f, _currentHealth - damage);
		_isDefeated = _currentHealth <= 0.0f;
		if (_isDefeated)
		{
			Velocity = Vector2.Zero;
			_isAttacking = false;
			_attackHitApplied = false;
			EmitSignal(SignalName.Defeated);
		}

		FlashAfterHit(_isDefeated);
	}

	private void FlashAfterHit(bool defeated)
	{
		if (_hitFlashTween != null && _hitFlashTween.IsRunning())
		{
			_hitFlashTween.Kill();
		}

		Color modulation = _sprite.Modulate;
		modulation.A = _originalSpriteAlpha;
		_sprite.Modulate = modulation;

		Tween tween = CreateTween();
		_hitFlashTween = tween;
		for (int pulse = 0; pulse < 3; pulse++)
		{
			tween.TweenProperty(
				_sprite,
				"modulate:a",
				_originalSpriteAlpha * 0.25f,
				0.07
			);
			tween.TweenProperty(
				_sprite,
				"modulate:a",
				_originalSpriteAlpha,
				0.07
			);
		}

		if (defeated)
		{
			tween.TweenCallback(Callable.From(() => QueueFree()));
		}
	}

	private void ProcessWandering(double delta)
	{
		_wanderTimeRemaining -= (float)delta;
		if (_wanderTimeRemaining <= 0.0f)
		{
			if (_isWandering)
			{
				StartWanderPause();
			}
			else
			{
				StartWanderMove();
			}
		}

		if (!_isWandering)
		{
			StopMoving();
			PlayAnimation($"idle_{_facing}");
			return;
		}

		UpdateFacing(_wanderDirection);
		Velocity = _wanderDirection * WanderSpeed;
		MoveAndSlide();
		PlayAnimation($"walk_{_facing}");

		if (GetSlideCollisionCount() > 0)
		{
			StartWanderPause();
			StopMoving();
			PlayAnimation($"idle_{_facing}");
		}
	}

	private bool TryPursueCollectible()
	{
		float detectionRangeSquared = ItemDetectionRange * ItemDetectionRange;
		float closestDistanceSquared = detectionRangeSquared;
		Collectible target = null!;

		foreach (Node node in GetTree().GetNodesInGroup("collectibles"))
		{
			if (node is not Collectible collectible
				|| collectible.Effect == Collectible.EffectType.Slowdown
				|| collectible.IsQueuedForDeletion())
			{
				continue;
			}

			float distanceSquared = GlobalPosition.DistanceSquaredTo(collectible.GlobalPosition);
			if (distanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				target = collectible;
			}
		}

		if (target == null)
		{
			return false;
		}

		Vector2 toCollectible = target.GlobalPosition - GlobalPosition;
		UpdateFacing(toCollectible);
		Velocity = toCollectible.Normalized() * MoveSpeed;
		MoveAndSlide();
		PlayAnimation($"walk_{_facing}");
		return true;
	}

	private void StartWanderMove()
	{
		Vector2[] directions =
		[
			Vector2.Down,
			Vector2.Left,
			Vector2.Right,
			Vector2.Up
		];

		_wanderDirection = directions[_random.RandiRange(0, directions.Length - 1)];
		_wanderTimeRemaining = _random.RandfRange(
			WanderMoveDurationMin,
			WanderMoveDurationMax
		);
		_isWandering = true;
	}

	private void StartWanderPause()
	{
		_wanderTimeRemaining = _random.RandfRange(
			WanderPauseDurationMin,
			WanderPauseDurationMax
		);
		_isWandering = false;
	}

	private void StartAttack(Vector2 toPlayer)
	{
		UpdateFacing(toPlayer);
		_isAttacking = true;
		_attackHitApplied = false;
		PlayAnimation($"attack_{_facing}");
	}

	private void OnAttackFrameChanged()
	{
		if (!_isAttacking || _attackHitApplied || _sprite.Frame != AttackHitFrame)
		{
			return;
		}

		_attackHitApplied = true;
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		if (toPlayer.Length() > AttackRange)
		{
			return;
		}

		PlayerHealth health = GetTree().GetFirstNodeInGroup("player_health") as PlayerHealth;
		if (health == null)
		{
			GD.PushError("SlimeEnemy could not find the player's health component.");
			return;
		}

		health.TakeDamage();
	}

	private void OnAnimationFinished()
	{
		if (!_isAttacking)
		{
			return;
		}

		_isAttacking = false;
		_attackCooldownRemaining = Mathf.Max(0.0f, AttackCooldownSeconds);
		PlayAnimation($"idle_{_facing}");
	}

	private void UpdateFacing(Vector2 direction)
	{
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			_facing = direction.X > 0.0f ? "right" : "left";
		}
		else if (direction != Vector2.Zero)
		{
			_facing = direction.Y > 0.0f ? "down" : "up";
		}
	}

	private void StopMoving()
	{
		Velocity = Vector2.Zero;
		MoveAndSlide();
	}

	private void PlayAnimation(string animationName)
	{
		if (_sprite.SpriteFrames.HasAnimation(animationName)
			&& _sprite.Animation != animationName)
		{
			_sprite.Play(animationName);
		}
	}
}
