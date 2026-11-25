using Godot;

public partial class DialogueController : CanvasLayer
{
	[Signal]
	public delegate void DialogueFinishedEventHandler();

	[Export]
	public Godot.Collections.Array<DialogueLine> Lines { get; set; } = new();

	[Export]
	public Texture2D PlayerPortrait { get; set; } = null!;

	[Export(PropertyHint.Range, "1,120,1")]
	public float CharactersPerSecond { get; set; } = 30.0f;

	private Control _panel = null!;
	private TextureRect _portrait = null!;
	private RichTextLabel _text = null!;
	private Tween _typingTween = null!;
	private int _lineIndex;
	private bool _isOpen;
	private bool _isTyping;

	public override void _EnterTree()
	{
		AddToGroup("dialogue_controller");
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		_panel = GetNode<Control>("DialoguePanel");
		_portrait = GetNode<TextureRect>("DialoguePanel/Content/Portrait");
		_text = GetNode<RichTextLabel>("DialoguePanel/Content/Text");

		_portrait.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_text.BbcodeEnabled = true;
		_text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_text.VerticalAlignment = VerticalAlignment.Top;
		const int dialogueFontSize = 8;
		_text.AddThemeFontSizeOverride("normal_font_size", dialogueFontSize);
		_text.AddThemeFontSizeOverride("bold_font_size", dialogueFontSize);
		_text.AddThemeFontSizeOverride("italics_font_size", dialogueFontSize);
		_text.AddThemeFontSizeOverride("bold_italics_font_size", dialogueFontSize);
		_text.AddThemeFontSizeOverride("mono_font_size", dialogueFontSize);

		_panel.Visible = false;
		if (Lines.Count == 0)
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

		if (_lineIndex >= Lines.Count)
		{
			CloseDialogue();
			return;
		}

		ShowCurrentLine();
	}

	private void ShowCurrentLine()
	{
		DialogueLine line = Lines[_lineIndex];
		if (line == null)
		{
			GD.PushError($"Dialogue line {_lineIndex + 1} is empty.");
			CloseDialogue();
			return;
		}

		_text.Text = line.Text;
		StartTyping();
		_portrait.Texture = PlayerPortrait;
		_portrait.Visible = line.IsPlayer;
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
