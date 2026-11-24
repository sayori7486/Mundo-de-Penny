using Godot;

public partial class InventoryHUD : CanvasLayer
{
	private HBoxContainer _items = null!;

	public override void _EnterTree()
	{
		AddToGroup("inventory_hud");
	}

	public override void _Ready()
	{
		var panel = new PanelContainer();
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
		panel.OffsetLeft = -80.0f;
		panel.OffsetTop = -26.0f;
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
}
