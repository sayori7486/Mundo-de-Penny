using Godot;

public partial class PennyRoomTutorial : Node
{
	private CollectibleTracker _collectibleTracker = null!;
	private DialogueController _dialogueController = null!;

	public override void _Ready()
	{
		_collectibleTracker = GetParent().GetNodeOrNull<CollectibleTracker>("CollectibleTracker")!;
		_dialogueController = GetParent().GetNodeOrNull<DialogueController>("DialogueUI")!;

		if (_collectibleTracker == null || _dialogueController == null)
		{
			GD.PushError("PennyRoomTutorial requires a collectible tracker and dialogue UI.");
			return;
		}

		_collectibleTracker.CollectibleCollected += OnCollectibleCollected;
	}

	public override void _ExitTree()
	{
		if (_collectibleTracker != null)
		{
			_collectibleTracker.CollectibleCollected -= OnCollectibleCollected;
		}
	}

	private void OnCollectibleCollected(Collectible collectible)
	{
		if (string.IsNullOrWhiteSpace(collectible.TutorialText))
		{
			return;
		}

		_dialogueController.ShowMessage("Penny", collectible.TutorialText);
	}
}
