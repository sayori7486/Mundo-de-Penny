using Godot;

[GlobalClass]
public partial class DialogueLine : Resource
{
	[Export]
	public string Speaker { get; set; } = "";

	[Export]
	public string Text { get; set; } = "";

	[Export]
	public bool IsPlayer { get; set; }
}
