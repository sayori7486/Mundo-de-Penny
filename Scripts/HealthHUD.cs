using Godot;

public partial class HealthHUD : CanvasLayer
{
	private AnimatedSprite2D _animatedHealth = null!;
	private AnimatedSprite2D _lifeStage = null!;

	public override void _Ready()
	{
		_animatedHealth = GetNode<AnimatedSprite2D>("AnimatedHealth");
		_lifeStage = GetNode<AnimatedSprite2D>("LifeStage");

		_animatedHealth.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_animatedHealth.Play("idle");
		_lifeStage.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_lifeStage.Play("full");

		PlayerHealth health = GetTree().GetFirstNodeInGroup("player_health") as PlayerHealth;
		if (health == null)
		{
			GD.PushError("HealthHUD could not find the player's health component.");
			return;
		}

		health.HealthChanged += UpdateHealthStage;
		UpdateHealthStage(health.CurrentHealth, health.MaxHealth);
	}

	private void UpdateHealthStage(int currentHealth, int maxHealth)
	{
		int mediumThreshold = (maxHealth + 1) / 2;
		string animation = currentHealth >= maxHealth
			? "full"
			: currentHealth >= mediumThreshold
				? "medium"
				: "low";

		if (_lifeStage.Animation != animation)
		{
			_lifeStage.Play(animation);
		}
	}
}
