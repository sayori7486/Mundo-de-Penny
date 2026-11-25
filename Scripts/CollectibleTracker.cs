using System.Collections.Generic;
using Godot;

public partial class CollectibleTracker : Node
{
	[Signal]
	public delegate void CollectionProgressChangedEventHandler(int collected, int total);

	[Signal]
	public delegate void AllCollectiblesCollectedEventHandler();

	private readonly HashSet<Collectible> _remaining = new();
	private int _total;

	public int Total => _total;
	public int Remaining => _remaining.Count;
	public bool CollectionEnabled { get; private set; } = true;

	public override void _EnterTree()
	{
		AddToGroup("collectible_tracker");
	}

	public void SetCollectionEnabled(bool enabled)
	{
		CollectionEnabled = enabled;
	}

	public void Register(Collectible collectible)
	{
		if (_remaining.Add(collectible))
		{
			_total++;
		}
	}

	public void Collect(Collectible collectible)
	{
		if (!CollectionEnabled || !_remaining.Remove(collectible))
		{
			return;
		}

		InventoryHUD inventory = GetTree().GetFirstNodeInGroup("inventory_hud") as InventoryHUD;
		if (inventory == null)
		{
			GD.PushError("InventoryHUD was not found in the current scene.");
		}
		else
		{
			inventory.AddCollectedItem(collectible.ItemTexture);
		}

		int collected = _total - _remaining.Count;
		EmitSignal(SignalName.CollectionProgressChanged, collected, _total);

		if (_remaining.Count == 0 && _total > 0)
		{
			EmitSignal(SignalName.AllCollectiblesCollected);
		}
	}
}
