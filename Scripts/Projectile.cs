using Godot;

public partial class Projectile : Area2D
{
	[Export]
	public float Speed { get; set; } = 150.0f;

	[Export]
	public float LifetimeSeconds { get; set; } = 3.0f;

	[Export(PropertyHint.Range, "0,1000,1")]
	public float FadeStartDistance { get; set; } = 180.0f;

	[Export(PropertyHint.Range, "1,500,1")]
	public float FadeDistance { get; set; } = 50.0f;

	public Vector2 Direction { get; set; } = Vector2.Right;

	private AnimatedSprite2D _sprite = null!;
	private float _initialAlpha;
	private float _distanceTravelled;
	private float _lifetimeRemaining;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("travel");
		_initialAlpha = _sprite.Modulate.A;
		_lifetimeRemaining = LifetimeSeconds;
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 movement = Direction * Speed * (float)delta;
		GlobalPosition += movement;
		_distanceTravelled += movement.Length();
		_lifetimeRemaining -= (float)delta;

		float fadeProgress = Mathf.Clamp(
			(_distanceTravelled - FadeStartDistance) / FadeDistance,
			0.0f,
			1.0f
		);
		Color modulation = _sprite.Modulate;
		modulation.A = _initialAlpha * (1.0f - fadeProgress);
		_sprite.Modulate = modulation;

		if (fadeProgress >= 1.0f || _lifetimeRemaining <= 0.0f)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("player"))
		{
			return;
		}

		if (body is SlimeEnemy slime)
		{
			slime.TakeDamage();
		}

		QueueFree();
	}
}
