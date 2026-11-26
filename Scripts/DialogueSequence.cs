using Godot;

[GlobalClass]
public partial class DialogueSequence : Resource
{
	[Export]
	public Godot.Collections.Array<DialogueLine> Lines { get; set; } = new();
}
