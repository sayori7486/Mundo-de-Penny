using Godot;

public partial class DialogueController : CanvasLayer
{
	[Export]
	public Godot.Collections.Array<DialogueLine> Lines { get; set; } = new();

	[Export]
	public Texture2D PlayerPortrait { get; set; } = null!;

	private Control _panel = null!;
	private TextureRect _portrait = null!;
	private Label _text = null!;
	private int _lineIndex;
	private bool _isOpen;

	public override void _EnterTree()
	{
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		_panel = GetNode<Control>("DialoguePanel");
		_portrait = GetNode<TextureRect>("DialoguePanel/Content/Portrait");
		_text = GetNode<Label>("DialoguePanel/Content/Text");

		_portrait.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_text.VerticalAlignment = VerticalAlignment.Center;
		_text.AddThemeFontSizeOverride("font_size", 12);

		_panel.Visible = false;
		if (Lines.Count == 0)
		{
			return;
		}

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
		_portrait.Texture = PlayerPortrait;
		_portrait.Visible = line.IsPlayer;
		_panel.Visible = true;
	}

	private void CloseDialogue()
	{
		_isOpen = false;
		_panel.Visible = false;
		GetTree().Paused = false;
	}
}
