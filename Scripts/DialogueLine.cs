using Godot;

[GlobalClass]
public partial class DialogueLine : Resource
{
	[Export]
	public string Speaker { get; set; } = "";

	[Export]
	public string Text { get; set; } = "";

	[Export]
	public Texture2D Portrait { get; set; } = null!;

	[Export]
	public bool UseCustomPortraitPosition { get; set; }

	[Export]
	public Vector2 PortraitPosition { get; set; } = new(6.0f, 8.0f);

	[Export]
	public bool UseCustomPortraitScale { get; set; }

	[Export]
	public Vector2 PortraitScale { get; set; } = Vector2.One;

	[Export]
	public bool IsPlayer { get; set; }
}
