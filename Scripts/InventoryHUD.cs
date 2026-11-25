using Godot;

public partial class InventoryHUD : CanvasLayer
{
	[Export]
	public string ObjectiveText { get; set; } = "";

	private HBoxContainer _items = null!;
	private Control _objectiveOverlay = null!;
	private Label _objectiveLabel = null!;

	public override void _EnterTree()
	{
		AddToGroup("inventory_hud");
	}

	public override void _Ready()
	{
		var panel = new PanelContainer();
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
		panel.OffsetLeft = -80.0f;
		panel.OffsetTop = -20.0f;
		panel.OffsetRight = -8.0f;
		panel.OffsetBottom = -8.0f;
		panel.MouseFilter = Control.MouseFilterEnum.Ignore;
		panel.AddThemeConstantOverride("margin_left", 2);
		panel.AddThemeConstantOverride("margin_top", 2);
		panel.AddThemeConstantOverride("margin_right", 2);
		panel.AddThemeConstantOverride("margin_bottom", 2);
		AddChild(panel);

		var scroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(68.0f, 14.0f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		panel.AddChild(scroll);

		_items = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_items.AddThemeConstantOverride("separation", 2);
		scroll.AddChild(_items);

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

	public void AddCollectedItem(Texture2D texture)
	{
		var icon = new TextureRect
		{
			Texture = texture,
			CustomMinimumSize = new Vector2(12.0f, 12.0f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_items.AddChild(icon);
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
