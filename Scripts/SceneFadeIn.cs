using Godot;

public partial class SceneFadeIn : CanvasLayer
{
	private ColorRect _overlay = null!;

	public override void _Ready()
	{
		_overlay = GetNode<ColorRect>("Overlay");
		Tween fadeIn = CreateTween();
		fadeIn.TweenProperty(_overlay, "color:a", 0.0f, 0.9f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}
}
