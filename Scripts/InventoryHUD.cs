using Godot;

public partial class InventoryHUD : CanvasLayer
{
	private const string CollectionBarTexturePath = "res://Sprites/Etc/itens bar.png";
	private const int CollectionBarFrameSize = 24;
	private const int CollectionBarSecondFrameX = 33;
	private const int MissingLeftTipY = 9;
	private const int FinalFillTipX = 12;
	private const int FinalFillTipY = 1;
	private static readonly Color LiquidColor = new(0.9411765f, 0.4862745f, 0.8352941f, 1.0f);
	private static readonly int[][] StarInteriorRuns =
	[
		[],
		[12, 13],
		[11, 14],
		[11, 14],
		[10, 15],
		[10, 15],
		[10, 15],
		[9, 16],
		[6, 19],
		[1, 24],
		[2, 23],
		[3, 22],
		[4, 21],
		[5, 20],
		[6, 19],
		[6, 19],
		[5, 20],
		[5, 20],
		[4, 21],
		[4, 10, 15, 21],
		[3, 9, 16, 22],
		[3, 7, 18, 22],
		[3, 5, 20, 22],
		[],
	];

	[Export]
	public string ObjectiveText { get; set; } = "";

	private Control _objectiveOverlay = null!;
	private Label _objectiveLabel = null!;
	private Sprite2D _collectionBarSprite = null!;
	private Sprite2D _liquidFillSprite = null!;
	private Sprite2D _shiftedLeftTipSprite = null!;
	private AtlasTexture _emptyCollectionTexture = null!;
	private AtlasTexture _filledCollectionTexture = null!;
	private Image _collectionBarAtlasImage = null!;
	private ImageTexture _liquidFillTexture = null!;
	private bool[,] _starInteriorMask = null!;

	public override void _EnterTree()
	{
		AddToGroup("inventory_hud");
	}

	public override void _Ready()
	{
		BuildCollectionBar();

		CollectibleTracker tracker =
			GetTree().GetFirstNodeInGroup("collectible_tracker") as CollectibleTracker;
		if (tracker == null)
		{
			GD.PushError("InventoryHUD could not find the collectible tracker.");
		}
		else
		{
			tracker.CollectionProgressChanged += UpdateCollectionProgress;
			UpdateCollectionProgress(tracker.Collected, tracker.ProgressTotal);
		}

		if (!string.IsNullOrWhiteSpace(ObjectiveText))
		{
			BuildObjectiveOverlay();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key
			|| key.Keycode != Key.Tab
			|| !key.Pressed
			|| key.Echo
			|| _objectiveOverlay == null)
		{
			return;
		}

		_objectiveOverlay.Visible = !_objectiveOverlay.Visible;
		GetViewport().SetInputAsHandled();
	}

	private void BuildCollectionBar()
	{
		Texture2D atlas = GD.Load<Texture2D>(CollectionBarTexturePath);
		if (atlas == null)
		{
			GD.PushError($"Could not load collection bar texture '{CollectionBarTexturePath}'.");
			return;
		}

		Vector2 atlasSize = atlas.GetSize();
		if (atlasSize.X < CollectionBarSecondFrameX + CollectionBarFrameSize
			|| atlasSize.Y < CollectionBarFrameSize)
		{
			GD.PushError($"Collection bar texture has invalid dimensions: {atlasSize}.");
			return;
		}

		_collectionBarAtlasImage = atlas.GetImage();
		if (_collectionBarAtlasImage == null || _collectionBarAtlasImage.IsEmpty())
		{
			GD.PushError("Could not read pixel data from the collection bar texture.");
			return;
		}

		BuildStarInteriorMask();
		_emptyCollectionTexture = new AtlasTexture
		{
			Atlas = atlas,
			Region = new Rect2(0.0f, 0.0f, CollectionBarFrameSize, CollectionBarFrameSize),
			ResourceLocalToScene = true,
		};
		_filledCollectionTexture = new AtlasTexture
		{
			Atlas = atlas,
			Region = new Rect2(
				CollectionBarSecondFrameX,
				0.0f,
				CollectionBarFrameSize,
				CollectionBarFrameSize
			),
			ResourceLocalToScene = true,
		};
		_liquidFillTexture = ImageTexture.CreateFromImage(CreateLiquidImage(0.0f));

		var bar = new Node2D
		{
			Position = new Vector2(306.0f, 166.0f),
		};
		_collectionBarSprite = new Sprite2D
		{
			Texture = _emptyCollectionTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			ZIndex = 2,
		};
		_liquidFillSprite = new Sprite2D
		{
			Texture = _liquidFillTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			ZIndex = 1,
		};
		var leftTipPixelImage = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		leftTipPixelImage.SetPixel(
			0,
			0,
			_collectionBarAtlasImage.GetPixel(0, MissingLeftTipY)
		);
		_shiftedLeftTipSprite = new Sprite2D
		{
			Texture = ImageTexture.CreateFromImage(leftTipPixelImage),
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Position = new Vector2(-12.5f, MissingLeftTipY - 11.5f),
			Visible = false,
			ZIndex = 3,
		};

		bar.AddChild(_collectionBarSprite);
		bar.AddChild(_liquidFillSprite);
		bar.AddChild(_shiftedLeftTipSprite);
		AddChild(bar);
	}

	private void UpdateCollectionProgress(int collected, int total)
	{
		if (_collectionBarSprite == null || _liquidFillSprite == null)
		{
			return;
		}

		if (collected <= 0)
		{
			_collectionBarSprite.Position = Vector2.Zero;
			_liquidFillSprite.Position = Vector2.Zero;
			_shiftedLeftTipSprite.Visible = false;
			_collectionBarSprite.Texture = _emptyCollectionTexture;
			_liquidFillTexture.Update(CreateLiquidImage(0.0f));
			return;
		}

		_collectionBarSprite.Position = Vector2.Zero;
		_liquidFillSprite.Position = Vector2.Left;
		_shiftedLeftTipSprite.Visible = true;
		_collectionBarSprite.Texture = _filledCollectionTexture;
		float fillAmount = total > 0
			? Mathf.Clamp((float)collected / total, 0.0f, 1.0f)
			: 0.0f;
		_liquidFillTexture.Update(CreateLiquidImage(fillAmount));
	}

	private Image CreateLiquidImage(float fillAmount)
	{
		var fillImage = Image.CreateEmpty(
			CollectionBarFrameSize,
			CollectionBarFrameSize,
			useMipmaps: false,
			Image.Format.Rgba8
		);
		fillImage.Fill(Colors.Transparent);

		int filledRows = Mathf.RoundToInt(fillAmount * CollectionBarFrameSize);
		int firstFilledRow = CollectionBarFrameSize - filledRows;
		for (int y = firstFilledRow; y < CollectionBarFrameSize; y++)
		{
			for (int x = 0; x < CollectionBarFrameSize; x++)
			{
				if (_starInteriorMask[x, y]
					|| (x > 0 && _starInteriorMask[x - 1, y]))
				{
					fillImage.SetPixel(x, y, LiquidColor);
				}
			}
		}

		if (fillAmount >= 1.0f)
		{
			fillImage.SetPixel(FinalFillTipX, FinalFillTipY, LiquidColor);
		}

		return fillImage;
	}

	private void BuildStarInteriorMask()
	{
		_starInteriorMask = new bool[CollectionBarFrameSize, CollectionBarFrameSize];
		for (int y = 0; y < CollectionBarFrameSize; y++)
		{
			int[] rowRuns = StarInteriorRuns[y];
			for (int run = 0; run < rowRuns.Length; run += 2)
			{
				for (int x = rowRuns[run]; x < rowRuns[run + 1]; x++)
				{
					Color outlinePixel = _collectionBarAtlasImage.GetPixel(
						CollectionBarSecondFrameX + x,
						y
					);
					_starInteriorMask[x, y] = outlinePixel.A <= 0.5f;
				}
			}
		}
	}

	public void SetObjectiveText(string text, bool showOverlay = false)
	{
		ObjectiveText = text;
		if (_objectiveOverlay == null)
		{
			BuildObjectiveOverlay();
		}

		_objectiveLabel.Text = $"Objetivo:\n{ObjectiveText}";
		if (showOverlay)
		{
			_objectiveOverlay.Visible = true;
		}
	}

	private void BuildObjectiveOverlay()
	{
		_objectiveOverlay = new Control
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_objectiveOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_objectiveOverlay);

		var background = new ColorRect
		{
			Color = new Color(0.0f, 0.0f, 0.0f, 0.7f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_objectiveOverlay.AddChild(background);

		_objectiveLabel = new Label
		{
			AnchorLeft = 0.08f,
			AnchorTop = 0.2f,
			AnchorRight = 0.92f,
			AnchorBottom = 0.8f,
			Text = $"Objetivo:\n{ObjectiveText}",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_objectiveLabel.AddThemeFontSizeOverride("font_size", 6);
		_objectiveLabel.AddThemeColorOverride("font_color", Colors.White);
		_objectiveOverlay.AddChild(_objectiveLabel);
	}
}
