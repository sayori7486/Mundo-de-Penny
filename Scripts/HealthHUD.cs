using Godot;

public partial class HealthHUD : CanvasLayer
{
	private const string ScarfSheetPath = "res://Sprites/Etc/rose/scarf-sheet.png";
	private const string ScarfNoColorPath = "res://Sprites/Etc/rose/scarf-no-color.png";
	private const int ScarfFrameWidth = 106;
	private const int ScarfFrameHeight = 48;
	private const int ScarfFramesPerRow = 4;
	private const float ScarfFrameSpeed = 4.0f;

	private AnimatedSprite2D _animatedHealth = null!;
	private AnimatedSprite2D _lifeStage = null!;
	private Sprite2D _staminaSprite = null!;
	private Texture2D _originalStaminaSpriteTexture = null!;
	private Texture2D _scarfNoColorTexture = null!;
	private Rect2 _originalStaminaSpriteRegion;
	private Vector2 _originalStaminaSpriteScale;
	private bool _originalStaminaSpriteRegionEnabled;
	private bool _showingNoColorSprite;

	public override void _Ready()
	{
		_animatedHealth = GetNode<AnimatedSprite2D>("AnimatedHealth");
		_lifeStage = GetNode<AnimatedSprite2D>("LifeStage");
		_staminaSprite = GetNode<Sprite2D>("Sprite2D");

		_animatedHealth.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_lifeStage.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_lifeStage.Play("full");
		_staminaSprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_originalStaminaSpriteTexture = _staminaSprite.Texture;
		_originalStaminaSpriteRegion = _staminaSprite.RegionRect;
		_originalStaminaSpriteScale = _staminaSprite.Scale;
		_originalStaminaSpriteRegionEnabled = _staminaSprite.RegionEnabled;

		Texture2D scarfSheet = GD.Load<Texture2D>(ScarfSheetPath);
		_scarfNoColorTexture = GD.Load<Texture2D>(ScarfNoColorPath);
		if (scarfSheet == null || _scarfNoColorTexture == null)
		{
			GD.PushError("HealthHUD could not load the scarf stamina textures.");
			return;
		}

		Vector2 scarfSheetSize = scarfSheet.GetSize();
		if (scarfSheetSize.X < ScarfFrameWidth * ScarfFramesPerRow
			|| scarfSheetSize.Y < ScarfFrameHeight * 3)
		{
			GD.PushError($"Scarf spritesheet has invalid dimensions: {scarfSheetSize}.");
			return;
		}

		_animatedHealth.SpriteFrames = CreateScarfSpriteFrames(scarfSheet);
		_animatedHealth.Play("full");

		PlayerHealth health = GetTree().GetFirstNodeInGroup("player_health") as PlayerHealth;
		if (health == null)
		{
			GD.PushError("HealthHUD could not find the player's health component.");
			return;
		}

		health.HealthChanged += UpdateHealthStage;
		UpdateHealthStage(health.CurrentHealth, health.MaxHealth);

		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
		{
			GD.PushError("HealthHUD could not find the player stamina source.");
			return;
		}

		player.StaminaChanged += UpdateStamina;
		UpdateStamina(player.Stamina);
	}

	private static SpriteFrames CreateScarfSpriteFrames(Texture2D scarfSheet)
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AddScarfAnimation(frames, scarfSheet, "full", 0);
		AddScarfAnimation(frames, scarfSheet, "medium", 1);
		AddScarfAnimation(frames, scarfSheet, "empty", 2);
		return frames;
	}

	private static void AddScarfAnimation(
		SpriteFrames frames,
		Texture2D scarfSheet,
		string animation,
		int row
	)
	{
		frames.AddAnimation(animation);
		frames.SetAnimationSpeed(animation, ScarfFrameSpeed);
		frames.SetAnimationLoopMode(animation, SpriteFrames.LoopMode.Linear);
		for (int column = 0; column < ScarfFramesPerRow; column++)
		{
			var frame = new AtlasTexture
			{
				Atlas = scarfSheet,
				Region = new Rect2(
					column * ScarfFrameWidth,
					row * ScarfFrameHeight,
					ScarfFrameWidth,
					ScarfFrameHeight
				),
				ResourceLocalToScene = true,
			};
			frames.AddFrame(animation, frame);
		}
	}

	private void UpdateStamina(float currentStamina)
	{
		string animation = currentStamina >= 1.0f
			? "full"
			: currentStamina > 0.0f
				? "medium"
				: "empty";
		if (_animatedHealth.Animation != animation)
		{
			_animatedHealth.Play(animation);
		}

		bool showNoColorSprite = currentStamina <= 0.0f;
		if (_showingNoColorSprite == showNoColorSprite)
		{
			return;
		}

		if (showNoColorSprite)
		{
			_staminaSprite.Texture = _scarfNoColorTexture;
			_staminaSprite.RegionEnabled = false;
			_staminaSprite.Scale = Vector2.One;
		}
		else
		{
			_staminaSprite.Texture = _originalStaminaSpriteTexture;
			_staminaSprite.RegionEnabled = _originalStaminaSpriteRegionEnabled;
			_staminaSprite.RegionRect = _originalStaminaSpriteRegion;
			_staminaSprite.Scale = _originalStaminaSpriteScale;
		}

		_showingNoColorSprite = showNoColorSprite;
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
