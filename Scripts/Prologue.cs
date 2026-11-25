using Godot;

public partial class Prologue : Control
{
	[Export]
	public Font NarrativeFont { get; set; } = null!;

	[Export]
	public Godot.Collections.Array<string> Pages { get; set; } = new()
	{
		"Escreva aqui o primeiro texto da introducao.",
		"Escreva aqui o proximo texto. Adicione mais paginas em Pages.",
	};

	[Export]
	public float CharactersPerSecond { get; set; } = 16.0f;

	private Label _body = null!;
	private Label _continueHint = null!;
	private ColorRect _fade = null!;
	private Tween _typingTween = null!;
	private int _pageIndex;
	private bool _isTyping;
	private bool _transitioning;

	public override void _Ready()
	{
		if (Pages.Count == 0)
		{
			GD.PushError("Prologue requires at least one text page.");
			return;
		}

		BuildInterface();
		ShowPage();

		Tween fadeIn = CreateTween();
		fadeIn.TweenProperty(_fade, "color:a", 0.0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key
			|| key.Keycode != Key.Space
			|| !key.Pressed
			|| key.Echo
			|| _transitioning)
		{
			return;
		}

		GetViewport().SetInputAsHandled();

		if (_isTyping)
		{
			_typingTween.Kill();
			_body.VisibleRatio = 1.0f;
			FinishTyping();
			return;
		}

		if (_pageIndex < Pages.Count - 1)
		{
			_pageIndex++;
			ShowPage();
			return;
		}

		_transitioning = true;
		TransitionToFirstLevel();
	}

	private void BuildInterface()
	{
		_body = new Label
		{
			AnchorLeft = 0.08f,
			AnchorTop = 0.2f,
			AnchorRight = 0.92f,
			AnchorBottom = 0.8f,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		ApplyFont(_body, 6, new Color(0.95f, 0.93f, 0.98f));
		AddChild(_body);

		_continueHint = new Label
		{
			AnchorRight = 1.0f,
			AnchorBottom = 1.0f,
			OffsetLeft = 4,
			OffsetTop = 4,
			OffsetRight = -4,
			OffsetBottom = -4,
			Text = "ESPAÇO - CONTINUAR",
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		ApplyFont(_continueHint, 4, new Color(0.88f, 0.78f, 0.92f));
		AddChild(_continueHint);

		_fade = new ColorRect
		{
			Color = Colors.Black,
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 10,
		};
		_fade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_fade);
	}

	private void ApplyFont(Control control, int size, Color color)
	{
		if (NarrativeFont != null)
		{
			control.AddThemeFontOverride("font", NarrativeFont);
		}

		control.AddThemeFontSizeOverride("font_size", size);
		control.AddThemeColorOverride("font_color", color);
	}

	private void ShowPage()
	{
		_body.Text = Pages[_pageIndex];
		_body.VisibleRatio = 0.0f;
		_continueHint.Visible = false;
		_isTyping = true;

		float duration = Mathf.Max(0.1f, _body.Text.Length / Mathf.Max(1.0f, CharactersPerSecond));
		_typingTween = CreateTween();
		_typingTween.TweenProperty(_body, "visible_ratio", 1.0f, duration);
		_typingTween.Finished += FinishTyping;
	}

	private void FinishTyping()
	{
		_isTyping = false;
		_continueHint.Visible = true;
	}

	private async void TransitionToFirstLevel()
	{
		Tween fadeOut = CreateTween();
		fadeOut.TweenProperty(_fade, "color:a", 1.0f, 0.9f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		const string firstLevelPath = "res://Scenes/penny's_room.tscn";
		Error error = GetTree().ChangeSceneToFile(firstLevelPath);
		if (error != Error.Ok)
		{
			GD.PushError($"Could not open first level '{firstLevelPath}': {error}");
		}
	}
}
