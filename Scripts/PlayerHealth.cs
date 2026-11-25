using Godot;

public partial class PlayerHealth : Node
{
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	[Export(PropertyHint.Range, "1,10,1")]
	public int MaxHealth { get; set; } = 3;

	[Export(PropertyHint.Range, "0,5,0.1")]
	public float DamageInvulnerabilitySeconds { get; set; } = 1.0f;

	public int CurrentHealth { get; private set; } = 3;

	private AnimatedSprite2D _sprite = null!;
	private float _originalSpriteAlpha;
	private Tween _hitFlashTween = null!;
	private float _invulnerabilityRemaining;

	public override void _EnterTree()
	{
		AddToGroup("player_health");
	}

	public override void _Ready()
	{
		if (MaxHealth < 1)
		{
			GD.PushError("PlayerHealth MaxHealth must be at least one.");
			return;
		}

		CurrentHealth = MaxHealth;
		_sprite = GetParent().GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("PlayerHealth could not find the player's AnimatedSprite2D.");
			return;
		}

		_originalSpriteAlpha = _sprite.Modulate.A;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	public override void _Process(double delta)
	{
		_invulnerabilityRemaining = Mathf.Max(
			0.0f,
			_invulnerabilityRemaining - (float)delta
		);
	}

	public void TakeDamage(int amount = 1)
	{
		if (CurrentHealth <= 0 || _invulnerabilityRemaining > 0.0f)
		{
			return;
		}

		CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(1, amount));
		_invulnerabilityRemaining = Mathf.Max(0.0f, DamageInvulnerabilitySeconds);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
		FlashAfterHit();
	}

	public void IncreaseMaxHealth(int amount = 1)
	{
		if (amount < 1)
		{
			GD.PushError("PlayerHealth max-health increase must be at least one.");
			return;
		}

		MaxHealth += amount;
		CurrentHealth += amount;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	public void AddTemporaryMaxHealth(int amount = 1)
	{
		if (amount < 1)
		{
			GD.PushError("PlayerHealth temporary max-health bonus must be at least one.");
			return;
		}

		MaxHealth += amount;
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	public void RemoveTemporaryMaxHealth(int amount = 1)
	{
		if (amount < 1)
		{
			GD.PushError("PlayerHealth temporary max-health removal must be at least one.");
			return;
		}

		MaxHealth = Mathf.Max(1, MaxHealth - amount);
		CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	private void FlashAfterHit()
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
	}
}
