using Godot;

[Tool]
public partial class Collectible : StaticBody2D
{
	public enum EffectType
	{
		None,
		DamageBoost,
		SpeedBoost,
		HealthIncrease,
		Slowdown,
		Rainbow
	}

	private AtlasTexture _itemTexture = null!;

	[Export]
	public AtlasTexture ItemTexture
	{
		get => _itemTexture;
		set
		{
			_itemTexture = value;
			if (_itemTexture != null)
			{
				_itemTexture.ResourceLocalToScene = true;
			}
			UpdateSpriteTexture();
		}
	}

	private EffectType _effect;

	[Export]
	public EffectType Effect
	{
		get => _effect;
		set
		{
			_effect = value;
		}
	}

	[Export]
	public AtlasTexture SlowdownTexture { get; set; } = null!;

	[Export(PropertyHint.Range, "1,60,1")]
	public float EffectDurationSeconds { get; set; } = 10.0f;

	private Sprite2D _sprite = null!;
	private Area2D _pickupArea = null!;
	private CollectibleTracker _tracker = null!;
	private bool _playerInRange;

	public override void _EnterTree()
	{
		AddToGroup("collectibles");
	}

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_pickupArea = GetNode<Area2D>("PickupArea");
		UpdateSpriteTexture();

		if (ItemTexture == null || ItemTexture.Atlas == null)
		{
			GD.PushError("Assign an AtlasTexture to the collectible's ItemTexture property.");
			return;
		}

		if (Engine.IsEditorHint())
		{
			return;
		}

		Texture2D atlas = ItemTexture.Atlas;
		Rect2 atlasRegion = ItemTexture.Region;
		Vector2 atlasSize = atlas.GetSize();
		if (atlasRegion.Position.X < 0.0f
			|| atlasRegion.Position.Y < 0.0f
			|| atlasRegion.Size.X <= 0.0f
			|| atlasRegion.Size.Y <= 0.0f
			|| atlasRegion.End.X > atlasSize.X
			|| atlasRegion.End.Y > atlasSize.Y)
		{
			GD.PushError($"Collectible atlas region is outside the spritesheet: {atlasRegion}");
			return;
		}

		_sprite.Texture = ItemTexture;

		_tracker = GetTree().GetFirstNodeInGroup("collectible_tracker") as CollectibleTracker;
		if (_tracker == null)
		{
			GD.PushError("CollectibleTracker was not found in the current scene.");
			return;
		}

		_tracker.Register(this);
		_pickupArea.BodyEntered += OnBodyEntered;
		_pickupArea.BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (_playerInRange && Input.IsActionJustPressed("interact"))
		{
			_tracker.Collect(this);
			QueueFree();
		}
	}

	public void ConvertToSlowdown()
	{
		if (Effect == EffectType.Slowdown)
		{
			return;
		}

		if (SlowdownTexture == null || SlowdownTexture.Atlas == null)
		{
			GD.PushError("Collectible cannot convert to slowdown without a purple flower texture.");
			return;
		}

		ItemTexture = SlowdownTexture;
		Effect = EffectType.Slowdown;
	}

	private void UpdateSpriteTexture()
	{
		if (IsInsideTree())
		{
			_sprite = GetNodeOrNull<Sprite2D>("Sprite2D")!;
			if (_sprite != null)
			{
				_sprite.Texture = _itemTexture;
			}
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player)
		{
			_playerInRange = true;
		}

		if (body is SlimeEnemy)
		{
			ConvertToSlowdown();
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is Player)
		{
			_playerInRange = false;
		}
	}
}
