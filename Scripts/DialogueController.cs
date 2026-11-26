using Godot;

public partial class DialogueController : CanvasLayer
{
	[Signal]
	public delegate void DialogueFinishedEventHandler();

	[Export]
	public DialogueSequence Dialogue { get; set; } = null!;

	[Export]
	public bool AutoStartOnReady { get; set; } = true;

	[Export]
	public Texture2D PlayerPortrait { get; set; } = null!;

	[Export]
	public Vector2 PortraitPosition { get; set; } = new(6.0f, 8.0f);

	[Export]
	public Vector2 PortraitScale { get; set; } = Vector2.One;

	[Export(PropertyHint.Range, "1,64,1")]
	public int SpeakerFontSize { get; set; } = 8;

	[Export(PropertyHint.Range, "1,64,1")]
	public int TextFontSize { get; set; } = 8;

	[Export(PropertyHint.Range, "1,120,1")]
	public float CharactersPerSecond { get; set; } = 30.0f;

	private Control _panel = null!;
	private TextureRect _box = null!;
	private TextureRect _portrait = null!;
	private Label _speaker = null!;
	private RichTextLabel _text = null!;
	private Godot.Collections.Array<DialogueLine> _lines = new();
	private Tween _typingTween = null!;
	private int _lineIndex;
	private bool _isOpen;
	private bool _isTyping;

	public bool IsOpen => _isOpen;

	public override void _EnterTree()
	{
		AddToGroup("dialogue_controller");
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		_panel = GetNode<Control>("DialoguePanel");
		_box = GetNode<TextureRect>("DialoguePanel/Box");
		_portrait = GetNode<TextureRect>("DialoguePanel/Portrait");
		_speaker = GetNode<Label>("DialoguePanel/Speaker");
		_text = GetNode<RichTextLabel>("DialoguePanel/Text");

		_box.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_box.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_box.StretchMode = TextureRect.StretchModeEnum.Keep;
		_portrait.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_portrait.Position = PortraitPosition;
		_portrait.Scale = PortraitScale;
		_text.BbcodeEnabled = true;
		_text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_text.VerticalAlignment = VerticalAlignment.Top;
		Font dialogueFont = _text.GetThemeFont("normal_font");
		_text.AddThemeFontOverride("bold_font", dialogueFont);
		_text.AddThemeFontOverride("italics_font", dialogueFont);
		_text.AddThemeFontOverride("bold_italics_font", dialogueFont);
		_text.AddThemeFontOverride("mono_font", dialogueFont);
		_speaker.AddThemeFontSizeOverride("font_size", SpeakerFontSize);
		Color dialogueTextColor = new(1.0f, 0.93f, 0.96f);
		_speaker.AddThemeColorOverride("font_color", dialogueTextColor);
		_text.AddThemeColorOverride("default_color", dialogueTextColor);
		_text.AddThemeFontSizeOverride("normal_font_size", TextFontSize);
		_text.AddThemeFontSizeOverride("bold_font_size", TextFontSize);
		_text.AddThemeFontSizeOverride("italics_font_size", TextFontSize);
		_text.AddThemeFontSizeOverride("bold_italics_font_size", TextFontSize);
		_text.AddThemeFontSizeOverride("mono_font_size", TextFontSize);

		_panel.Visible = false;
		if (Dialogue == null || Dialogue.Lines.Count == 0)
		{
			return;
		}
		_lines = Dialogue.Lines;
		if (!AutoStartOnReady)
		{
			return;
		}

		SceneFadeIn fadeIn = GetParent().GetNodeOrNull<SceneFadeIn>("SceneFadeIn");
		if (fadeIn != null)
		{
			StartDialogueAfterFade(fadeIn);
			return;
		}

		StartDialogue();
	}

	public bool StartConfiguredDialogue()
	{
		if (_isOpen)
		{
			GD.PushError("Cannot start a configured dialogue while another one is open.");
			return false;
		}

		if (Dialogue == null || Dialogue.Lines.Count == 0)
		{
			GD.PushError("Cannot start a configured dialogue without any dialogue lines.");
			return false;
		}

		_lines = Dialogue.Lines;
		_lineIndex = 0;
		StartDialogue();
		return true;
	}

	private async void StartDialogueAfterFade(SceneFadeIn fadeIn)
	{
		await ToSignal(fadeIn, SceneFadeIn.SignalName.FadeInCompleted);
		if (IsInsideTree())
		{
			StartDialogue();
		}
	}

	private void StartDialogue()
	{
		_isOpen = true;
		GetTree().Paused = true;
		ShowCurrentLine();
	}

	public void ShowMessage(string speaker, string text, bool showPortrait = true)
	{
		if (_isOpen)
		{
			GD.PushError("Cannot show a new dialogue message while another one is open.");
			return;
		}

		if (string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(text))
		{
			GD.PushError("Dialogue messages require a speaker and text.");
			return;
		}

		DialogueLine line = new()
		{
			Speaker = speaker,
			Text = text,
			IsPlayer = showPortrait,
		};
		_lines = new Godot.Collections.Array<DialogueLine>();
		_lines.Add(line);
		_lineIndex = 0;
		StartDialogue();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isOpen
			|| @event is not InputEventKey key
			|| !key.Pressed
			|| key.Echo
			|| key.Keycode is not (Key.Space or Key.Enter or Key.E))
		{
			return;
		}

		GetViewport().SetInputAsHandled();

		if (_isTyping)
		{
			_typingTween.Kill();
			_text.VisibleRatio = 1.0f;
			_isTyping = false;
			return;
		}

		_lineIndex++;

		if (_lineIndex >= _lines.Count)
		{
			CloseDialogue();
			return;
		}

		ShowCurrentLine();
	}

	private void ShowCurrentLine()
	{
		DialogueLine line = _lines[_lineIndex];
		if (line == null)
		{
			GD.PushError($"Dialogue line {_lineIndex + 1} is empty.");
			CloseDialogue();
			return;
		}

		_speaker.Text = line.Speaker;
		_text.Text = line.Text;
		StartTyping();
		Texture2D portrait = line.Portrait;
		if (portrait == null && line.IsPlayer)
		{
			portrait = PlayerPortrait;
		}

		_portrait.Texture = portrait;
		_portrait.Position = line.UseCustomPortraitPosition ? line.PortraitPosition : PortraitPosition;
		_portrait.Scale = line.UseCustomPortraitScale ? line.PortraitScale : PortraitScale;
		_portrait.Visible = portrait != null;
		_panel.Visible = true;
	}

	private void StartTyping()
	{
		_text.VisibleRatio = 0.0f;
		int characterCount = _text.GetTotalCharacterCount();
		if (characterCount == 0)
		{
			_text.VisibleRatio = 1.0f;
			_isTyping = false;
			return;
		}

		float duration = characterCount / Mathf.Max(1.0f, CharactersPerSecond);
		_isTyping = true;
		_typingTween = CreateTween();
		_typingTween.TweenProperty(_text, "visible_ratio", 1.0f, duration);
		_typingTween.Finished += OnTypingFinished;
	}

	private void OnTypingFinished()
	{
		_isTyping = false;
	}

	private void CloseDialogue()
	{
		if (_isTyping)
		{
			_typingTween.Kill();
			_isTyping = false;
		}

		_isOpen = false;
		_panel.Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.DialogueFinished);
	}
}
