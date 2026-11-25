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
	}
}
