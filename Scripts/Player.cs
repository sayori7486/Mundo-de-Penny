using System.Collections.Generic;
using Godot;

public partial class Player : CharacterBody2D
{
	public enum MovementAnimationSet
	{
		Walk,
		Staff
	}

	[Export]
	public float Speed { get; set; } = 120.0f;

	[Export]
	public MovementAnimationSet AnimationSet { get; set; } = MovementAnimationSet.Walk;

	[Export]
	public PackedScene ProjectileScene { get; set; } = null!;

	[Export(PropertyHint.Range, "0.1,20,0.1")]
	public float BaseProjectileDamage { get; set; } = 1.0f;

	[Export(PropertyHint.Range, "0,4,1")]
	public int ProjectileLaunchFrame { get; set; } = 3;

	[Export]
	public bool FollowPlayer { get; set; } = false;

	[Export]
	public Rect2 CameraLimits { get; set; } = new Rect2(0, 0, 320, 256);

	private AnimatedSprite2D _sprite = null!;
	private Camera2D _camera = null!;
	private PlayerHealth _health = null!;
	private readonly Dictionary<Collectible.EffectType, float> _activeEffects = new();
	private static readonly Collectible.EffectType[] TimedEffects =
	[
		Collectible.EffectType.DamageBoost,
		Collectible.EffectType.SpeedBoost,
		Collectible.EffectType.Slowdown,
		Collectible.EffectType.Rainbow
	];
	private Color _baseSpriteColor;
	private float _rainbowHue;
	private string _facing = "down";
	private bool _hasPermanentHealthEffect;
	private bool _isAttacking;
	private bool _projectileSpawned;

	public override void _EnterTree()
	{
		AddToGroup("player");
	}

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_camera = GetNodeOrNull<Camera2D>("Camera2D");
		_health = GetNodeOrNull<PlayerHealth>("Health")!;
		ConfigureCamera();

		if (_sprite.SpriteFrames == null)
		{
			GD.PushError("Player AnimatedSprite2D has no SpriteFrames resource.");
			return;
		}

		if (_health == null)
		{
			GD.PushError("Player could not find its PlayerHealth component.");
		}

		_baseSpriteColor = _sprite.Modulate;

		foreach (string direction in new[] { "down", "left", "right", "up" })
		{
			_sprite.SpriteFrames.SetAnimationLoopMode(
				$"attack_{direction}",
				SpriteFrames.LoopMode.None
			);
		}

		_sprite.AnimationFinished += OnAnimationFinished;
		_sprite.FrameChanged += OnAttackFrameChanged;
		PlayAnimation(GetIdleAnimationName());
	}

	public override void _Process(double delta)
	{
		if (_activeEffects.Count == 0)
		{
			return;
		}

		bool effectExpired = false;
		foreach (Collectible.EffectType effect in TimedEffects)
		{
			if (!_activeEffects.TryGetValue(effect, out float remaining))
			{
				continue;
			}

			remaining -= (float)delta;
			if (remaining > 0.0f)
			{
				_activeEffects[effect] = remaining;
				continue;
			}

			_activeEffects.Remove(effect);
			effectExpired = true;
			if (effect == Collectible.EffectType.Rainbow)
			{
				_health.RemoveTemporaryMaxHealth();
			}
		}

		if (HasEffect(Collectible.EffectType.Rainbow))
		{
			_rainbowHue = Mathf.PosMod(_rainbowHue + (float)delta * 0.5f, 1.0f);
			UpdateEffectAppearance();
		}
		else if (effectExpired)
		{
			UpdateEffectAppearance();
		}
	}

	public void ApplyCollectibleEffect(
		Collectible.EffectType effect,
		float durationSeconds
	)
	{
		switch (effect)
		{
			case Collectible.EffectType.None:
				return;
			case Collectible.EffectType.HealthIncrease:
				if (_health == null)
				{
					GD.PushError("Player cannot apply a health effect without PlayerHealth.");
					return;
				}

				ReplaceActiveEffect();
				_health.IncreaseMaxHealth();
				_hasPermanentHealthEffect = true;
				UpdateEffectAppearance();
				return;
			case Collectible.EffectType.DamageBoost:
			case Collectible.EffectType.SpeedBoost:
			case Collectible.EffectType.Slowdown:
			case Collectible.EffectType.Rainbow:
				if (durationSeconds <= 0.0f || !float.IsFinite(durationSeconds))
				{
					GD.PushError($"Collectible effect '{effect}' has an invalid duration.");
					return;
				}

				if (effect == Collectible.EffectType.Rainbow && _health == null)
				{
					GD.PushError("Player cannot apply the rainbow effect without PlayerHealth.");
					return;
				}

				ReplaceActiveEffect();
				ApplyTemporaryEffect(effect, durationSeconds);
				return;
			default:
				GD.PushError($"Player received an unsupported collectible effect: {effect}.");
				return;
		}
	}

	private void ConfigureCamera()
	{
		if (_camera == null)
		{
			return;
		}

		_camera.Enabled = FollowPlayer;
		_camera.LimitLeft = (int)CameraLimits.Position.X;
		_camera.LimitTop = (int)CameraLimits.Position.Y;
		_camera.LimitRight = (int)(CameraLimits.Position.X + CameraLimits.Size.X);
		_camera.LimitBottom = (int)(CameraLimits.Position.Y + CameraLimits.Size.Y);

		if (FollowPlayer)
		{
			_camera.MakeCurrent();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (AnimationSet == MovementAnimationSet.Staff
			&& !_isAttacking
			&& Input.IsActionJustPressed("attack"))
		{
			StartAttack();
		}

		if (_isAttacking)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 direction = new(
			Input.GetAxis("move_left", "move_right"),
			Input.GetAxis("move_up", "move_down")
		);

		if (direction.Length() > 1.0f)
		{
			direction = direction.Normalized();
		}

		float speedMultiplier = 1.0f;
		if (HasEffect(Collectible.EffectType.SpeedBoost))
		{
			speedMultiplier *= 1.15f;
		}

		if (HasEffect(Collectible.EffectType.Slowdown))
		{
			speedMultiplier *= 0.9f;
		}

		if (HasEffect(Collectible.EffectType.Rainbow))
		{
			speedMultiplier *= 1.15f;
		}

		Velocity = direction * Speed * speedMultiplier;
		MoveAndSlide();

		if (direction == Vector2.Zero)
		{
			PlayAnimation(GetIdleAnimationName());
			return;
		}

		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			_facing = direction.X > 0.0f ? "right" : "left";
		}
		else
		{
			_facing = direction.Y > 0.0f ? "down" : "up";
		}

		string animationPrefix = AnimationSet == MovementAnimationSet.Staff ? "staff" : "walk";
		PlayAnimation($"{animationPrefix}_{_facing}");
	}

	private void StartAttack()
	{
		string animationName = $"attack_{_facing}";
		if (!_sprite.SpriteFrames.HasAnimation(animationName))
		{
			GD.PushError($"Player is missing the '{animationName}' animation.");
			return;
		}

		_isAttacking = true;
		_projectileSpawned = false;
		UpdateEffectAppearance();
		_sprite.Play(animationName);
	}

	private void OnAttackFrameChanged()
	{
		if (!_isAttacking || _projectileSpawned || _sprite.Frame != ProjectileLaunchFrame)
		{
			return;
		}

		_projectileSpawned = true;
		SpawnProjectile();
	}

	private void OnAnimationFinished()
	{
		if (!_isAttacking)
		{
			return;
		}

		_isAttacking = false;
		UpdateEffectAppearance();
		PlayAnimation(GetIdleAnimationName());
	}

	private void SpawnProjectile()
	{
		if (ProjectileScene == null)
		{
			GD.PushError("Player has no ProjectileScene assigned.");
			return;
		}

		Node parent = GetParent();
		if (parent == null)
		{
			GD.PushError("Player cannot spawn a projectile without a parent node.");
			return;
		}

		Vector2 direction = _facing switch
		{
			"up" => Vector2.Up,
			"left" => Vector2.Left,
			"right" => Vector2.Right,
			_ => Vector2.Down,
		};

		Projectile projectile = ProjectileScene.Instantiate<Projectile>();
		projectile.Direction = direction;
		float damageMultiplier = 1.0f;
		if (HasEffect(Collectible.EffectType.DamageBoost))
		{
			damageMultiplier *= 1.15f;
		}

		if (HasEffect(Collectible.EffectType.Rainbow))
		{
			damageMultiplier *= 1.15f;
		}

		projectile.Damage = Mathf.Max(0.1f, BaseProjectileDamage) * damageMultiplier;
		parent.AddChild(projectile);
		projectile.GlobalPosition = GlobalPosition + new Vector2(0.0f, -8.0f)
			+ direction * 12.0f;
	}

	private string GetIdleAnimationName()
	{
		string animationPrefix = AnimationSet == MovementAnimationSet.Staff
			? "staff_idle"
			: "idle";
		return $"{animationPrefix}_{_facing}";
	}

	private void PlayAnimation(string animationName)
	{
		if (_sprite.SpriteFrames == null)
		{
			return;
		}

		if (_sprite.SpriteFrames.HasAnimation(animationName))
		{
			if (_sprite.Animation != animationName)
			{
				_sprite.Play(animationName);
			}
		}
	}

	private void ApplyTemporaryEffect(
		Collectible.EffectType effect,
		float durationSeconds
	)
	{
		if (effect == Collectible.EffectType.Rainbow)
		{
			_health.AddTemporaryMaxHealth();
		}

		_activeEffects[effect] = durationSeconds;
		UpdateEffectAppearance();
	}

	private void ReplaceActiveEffect()
	{
		if (HasEffect(Collectible.EffectType.Rainbow))
		{
			_health.RemoveTemporaryMaxHealth();
		}

		_activeEffects.Clear();
		_hasPermanentHealthEffect = false;
		UpdateEffectAppearance();
	}

	private bool HasEffect(Collectible.EffectType effect)
	{
		return _activeEffects.ContainsKey(effect);
	}

	private void UpdateEffectAppearance()
	{
		_sprite.SpeedScale = _isAttacking && HasEffect(Collectible.EffectType.Rainbow)
			? 1.15f
			: 1.0f;

		Color tint;
		if (HasEffect(Collectible.EffectType.Rainbow))
		{
			tint = Color.FromHsv(_rainbowHue, 0.75f, 1.0f);
		}
		else if (HasEffect(Collectible.EffectType.DamageBoost))
		{
			tint = new Color(1.0f, 0.72f, 0.72f);
		}
		else if (HasEffect(Collectible.EffectType.SpeedBoost))
		{
			tint = new Color(1.0f, 0.92f, 0.68f);
		}
		else if (HasEffect(Collectible.EffectType.Slowdown))
		{
			tint = new Color(0.86f, 0.74f, 1.0f);
		}
		else if (_hasPermanentHealthEffect)
		{
			tint = new Color(0.72f, 1.0f, 0.72f);
		}
		else
		{
			tint = Colors.White;
		}

		Color modulation = _baseSpriteColor.Lerp(tint, 0.35f);
		modulation.A = _sprite.Modulate.A;
		_sprite.Modulate = modulation;
	}
}
