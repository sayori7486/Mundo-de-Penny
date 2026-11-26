using Godot;

public partial class ControlsTutorial : Node
{
	[Export]
	public NodePath PlayerPath { get; set; } = new("../Actors/Player");

	[Export]
	public Godot.Collections.Array<string> Hints { get; set; } = new();

	[Export]
	public Godot.Collections.Array<string> TriggerActions { get; set; } = new();

	private DialogueController _dialogueController = null!;
	private Label _hintLabel = null!;
	private int _hintIndex;

	public override void _EnterTree()
	{
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		if (Hints.Count == 0)
		{
			return;
		}

		if (TriggerActions.Count != Hints.Count)
		{
			GD.PushError("ControlsTutorial requires one trigger action for each hint.");
			return;
		}

		Node2D player = GetNodeOrNull<Node2D>(PlayerPath)!;
		if (player == null)
		{
			GD.PushError($"ControlsTutorial could not find the player at '{PlayerPath}'.");
			return;
		}

		_dialogueController =
			GetTree().GetFirstNodeInGroup("dialogue_controller") as DialogueController;
		_hintLabel = new Label
		{
			Position = new Vector2(-70.0f, -45.0f),
			Size = new Vector2(140.0f, 12.0f),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 10,
			Text = Hints[_hintIndex],
		};
		_hintLabel.AddThemeFontSizeOverride("font_size", 4);
		_hintLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.62f, 1.0f));
		_hintLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		_hintLabel.AddThemeConstantOverride("outline_size", 2);
		player.AddChild(_hintLabel);
	}

	public override void _Process(double delta)
	{
		if (_hintLabel != null)
		{
			_hintLabel.Visible = _hintIndex < Hints.Count
				&& (_dialogueController == null
					|| !_dialogueController.IsOpen
					|| TriggerActions[_hintIndex] == "dialogue");
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_hintLabel == null || _hintIndex >= Hints.Count
			|| (_dialogueController != null
				&& _dialogueController.IsOpen
				&& TriggerActions[_hintIndex] != "dialogue"))
		{
			return;
		}

		if (!MatchesTriggerAction(@event, TriggerActions[_hintIndex]))
		{
			return;
		}

		_hintIndex++;
		if (_hintIndex < Hints.Count)
		{
			_hintLabel.Text = Hints[_hintIndex];
		}
		else
		{
			_hintLabel.Visible = false;
		}
	}

	private static bool MatchesTriggerAction(InputEvent @event, string action)
	{
		if (!IsPressedEvent(@event))
		{
			return false;
		}

		switch (action)
		{
			case "move":
				return InputMap.EventIsAction(@event, "move_left")
					|| InputMap.EventIsAction(@event, "move_right")
					|| InputMap.EventIsAction(@event, "move_up")
					|| InputMap.EventIsAction(@event, "move_down");
			case "objectives":
				return IsPressedKey(@event, Key.Tab);
			case "interact":
				return InputMap.EventIsAction(@event, "interact");
			case "dash":
				return InputMap.EventIsAction(@event, "dash");
			case "dialogue":
				return IsPressedKey(@event, Key.Space)
					|| IsPressedKey(@event, Key.Enter);
			case "attack":
				return InputMap.EventIsAction(@event, "attack");
			default:
				return false;
		}
	}

	private static bool IsPressedEvent(InputEvent @event)
	{
		return @event is InputEventKey key && key.Pressed && !key.Echo
			|| @event is InputEventMouseButton mouseButton && mouseButton.Pressed
			|| @event is InputEventJoypadButton joypadButton && joypadButton.Pressed
			|| @event is InputEventJoypadMotion;
	}

	private static bool IsPressedKey(InputEvent @event, Key keyCode)
	{
		return @event is InputEventKey key
			&& key.Pressed
			&& !key.Echo
			&& key.Keycode == keyCode;
	}
}
