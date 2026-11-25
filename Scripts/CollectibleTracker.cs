using System.Collections.Generic;
using Godot;

public partial class CollectibleTracker : Node
{
	[Signal]
	public delegate void CollectionProgressChangedEventHandler(int collected, int total);

	[Signal]
	public delegate void AllCollectiblesCollectedEventHandler();

	[Signal]
	public delegate void CollectibleCollectedEventHandler(Collectible collectible);

	private readonly HashSet<Collectible> _remaining = new();
	private int _total;
	private int _expectedTotal;

	public int Total => _total;
	public int Remaining => _remaining.Count;
	public int Collected => _total - _remaining.Count;
	public int ProgressTotal => _expectedTotal > 0 ? _expectedTotal : _total;

	public override void _EnterTree()
	{
		AddToGroup("collectible_tracker");
	}

	public void Register(Collectible collectible)
	{
		if (_remaining.Add(collectible))
		{
			_total++;
			EmitSignal(SignalName.CollectionProgressChanged, Collected, ProgressTotal);
		}
	}

	public void SetExpectedTotal(int total)
	{
		if (total <= 0 || total < _total)
		{
			GD.PushError("CollectibleTracker expected total must be positive and not below registered items.");
			return;
		}

		_expectedTotal = total;
		EmitSignal(SignalName.CollectionProgressChanged, Collected, ProgressTotal);
	}

	public void Collect(Collectible collectible)
	{
		if (!_remaining.Remove(collectible))
		{
			return;
		}

		if (GetTree().GetFirstNodeInGroup("player") is Player player)
		{
			player.ApplyCollectibleEffect(collectible.Effect, collectible.EffectDurationSeconds);
		}
		else
		{
			GD.PushError("Player was not found while applying a collectible effect.");
		}

		EmitSignal(SignalName.CollectibleCollected, collectible);
		EmitSignal(SignalName.CollectionProgressChanged, Collected, ProgressTotal);

		if (_remaining.Count == 0 && _total > 0)
		{
			EmitSignal(SignalName.AllCollectiblesCollected);
		}
	}
}
