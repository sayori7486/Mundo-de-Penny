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

	[Export(PropertyHint.Range, "0,4,1")]
	public int ProjectileLaunchFrame { get; set; } = 3;

	[Export]
	public bool FollowPlayer { get; set; } = false;

	[Export]
	public Rect2 CameraLimits { get; set; } = new Rect2(0, 0, 320, 256);

	private AnimatedSprite2D _sprite = null!;
	private Camera2D _camera = null!;
	private string _facing = "down";
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
		ConfigureCamera();

		if (_sprite.SpriteFrames == null)
		{
			GD.PushError("Player AnimatedSprite2D has no SpriteFrames resource.");
			return;
		}

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

		Velocity = direction * Speed;
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

}
