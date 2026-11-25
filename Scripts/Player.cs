using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed { get; set; } = 120.0f;

	[Export]
	public bool FollowPlayer { get; set; } = false;

	[Export]
	public Rect2 CameraLimits { get; set; } = new Rect2(0, 0, 320, 256);

	private AnimatedSprite2D _sprite = null!;
	private Camera2D _camera = null!;
	private string _facing = "down";

	public override void _EnterTree()
	{
		AddToGroup("player");
	}

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_camera = GetNodeOrNull<Camera2D>("Camera2D");
		ConfigureCamera();
		BuildAnimations();
		PlayAnimation("idle_down");
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
			PlayAnimation($"idle_{_facing}");
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

		PlayAnimation($"walk_{_facing}");
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

	private void BuildAnimations()
	{
		SpriteFrames frames = new();
		string[] directions = ["down", "right", "left", "up"];
		string[] walkFiles =
		[
			"res://Sprites/Penny/Walk/penny-walk-frame-0.png",
			"res://Sprites/Penny/Walk/penny-walk-frame-1.png",
			"res://Sprites/Penny/Walk/penny-walk-frame-2.png",
			"res://Sprites/Penny/Walk/penny-walk-frame-3.png",
		];

		for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
		{
			string direction = directions[directionIndex];

			frames.AddAnimation($"idle_{direction}");
			frames.SetAnimationSpeed($"idle_{direction}", 1.0f);
			frames.AddFrame(
				$"idle_{direction}",
				CreateDirectionTexture(walkFiles[0], directionIndex)
			);

			string walkAnimation = $"walk_{direction}";
			frames.AddAnimation(walkAnimation);
			frames.SetAnimationSpeed(walkAnimation, 8.0f);

			foreach (string filePath in walkFiles)
			{
				frames.AddFrame(walkAnimation, CreateDirectionTexture(filePath, directionIndex));
			}
		}

		_sprite.SpriteFrames = frames;
	}

	private static AtlasTexture CreateDirectionTexture(string filePath, int directionIndex)
	{
		Texture2D texture = GD.Load<Texture2D>(filePath);

		return new AtlasTexture
		{
			Atlas = texture,
			Region = new Rect2(directionIndex * 24, 0, 24, 32),
		};
	}
}
