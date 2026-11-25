using System.Collections.Generic;
using Godot;

public partial class CatFollower : CharacterBody2D
{
	[Export]
	public float FollowSpeed { get; set; } = 130.0f;

	[Export]
	public float FollowDistance { get; set; } = 20.0f;

	[Export]
	public float WaypointTolerance { get; set; } = 1.5f;

	private const float TrailSampleDistance = 4.0f;
	private const int TrailPruneThreshold = 128;
	private readonly List<Vector2> _trailPoints = new();
	private readonly List<float> _trailDistances = new();
	private Player _player = null!;
	private AnimatedSprite2D _sprite = null!;
	private Vector2 _lastPlayerSample;
	private float _trailLength;
	private int _catTrailIndex;
	private string _facing = "down";
	private bool _isFollowing;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		PlayAnimation("idle_down");

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
		{
			GD.PushError("CatFollower could not find the player.");
			return;
		}

		DialogueController dialogue =
			GetTree().GetFirstNodeInGroup("dialogue_controller") as DialogueController;
		if (dialogue == null)
		{
			StartFollowing();
			return;
		}

		dialogue.DialogueFinished += StartFollowing;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isFollowing)
		{
			return;
		}

		RecordPlayerTrail();
		FollowTrail();
	}

	private void StartFollowing()
	{
		if (_isFollowing)
		{
			return;
		}

		AddTrailPoint(GlobalPosition);
		AddTrailPoint(_player.GlobalPosition);
		_lastPlayerSample = _player.GlobalPosition;
		_isFollowing = true;
	}

	private void RecordPlayerTrail()
	{
		Vector2 playerPosition = _player.GlobalPosition;
		Vector2 toPlayer = playerPosition - _lastPlayerSample;

		while (toPlayer.Length() >= TrailSampleDistance)
		{
			_lastPlayerSample += toPlayer.Normalized() * TrailSampleDistance;
			AddTrailPoint(_lastPlayerSample);
			toPlayer = playerPosition - _lastPlayerSample;
		}
	}

	private void AddTrailPoint(Vector2 point)
	{
		if (_trailPoints.Count == 0)
		{
			_trailPoints.Add(point);
			_trailDistances.Add(0.0f);
			return;
		}

		float segmentLength = _trailPoints[^1].DistanceTo(point);
		if (segmentLength <= 0.001f)
		{
			return;
		}

		_trailLength += segmentLength;
		_trailPoints.Add(point);
		_trailDistances.Add(_trailLength);
	}

	private void FollowTrail()
	{
		float targetDistance = _trailLength - Mathf.Max(0.0f, FollowDistance);
		float currentDistance = _trailDistances[_catTrailIndex];
		if (targetDistance <= currentDistance)
		{
			StopMoving();
			return;
		}

		int nextIndex = _catTrailIndex + 1;
		float nextDistance = _trailDistances[nextIndex];
		Vector2 target;

		if (nextDistance <= targetDistance)
		{
			target = _trailPoints[nextIndex];
			if (MoveToward(target))
			{
				_catTrailIndex = nextIndex;
				PruneTrail();
			}

			return;
		}

		float segmentLength = nextDistance - currentDistance;
		float progress = (targetDistance - currentDistance) / segmentLength;
		target = _trailPoints[_catTrailIndex].Lerp(_trailPoints[nextIndex], progress);
		MoveToward(target);
	}

	private bool MoveToward(Vector2 target)
	{
		Vector2 toTarget = target - GlobalPosition;
		if (toTarget.Length() <= Mathf.Max(0.1f, WaypointTolerance))
		{
			StopMoving();
			return true;
		}

		Vector2 previousPosition = GlobalPosition;
		Velocity = toTarget.Normalized() * FollowSpeed;
		MoveAndSlide();

		Vector2 movement = GlobalPosition - previousPosition;
		if (movement.LengthSquared() > 0.0001f)
		{
			UpdateAnimation(movement);
		}
		else
		{
			Velocity = Vector2.Zero;
			PlayAnimation($"idle_{_facing}");
		}

		return GlobalPosition.DistanceTo(target) <= Mathf.Max(0.1f, WaypointTolerance);
	}

	private void StopMoving()
	{
		Velocity = Vector2.Zero;
		MoveAndSlide();
		PlayAnimation($"idle_{_facing}");
	}

	private void PruneTrail()
	{
		if (_catTrailIndex < TrailPruneThreshold)
		{
			return;
		}

		float distanceOffset = _trailDistances[_catTrailIndex];
		_trailPoints.RemoveRange(0, _catTrailIndex);
		_trailDistances.RemoveRange(0, _catTrailIndex);
		for (int i = 0; i < _trailDistances.Count; i++)
		{
			_trailDistances[i] -= distanceOffset;
		}

		_trailLength -= distanceOffset;
		_catTrailIndex = 0;
	}

	private void UpdateAnimation(Vector2 direction)
	{
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			_facing = direction.X > 0.0f ? "right" : "left";
		}
		else if (direction != Vector2.Zero)
		{
			_facing = direction.Y > 0.0f ? "down" : "up";
		}

		PlayAnimation($"walk_{_facing}");
	}

	private void PlayAnimation(string animationName)
	{
		if (_sprite.SpriteFrames != null
			&& _sprite.SpriteFrames.HasAnimation(animationName)
			&& _sprite.Animation != animationName)
		{
			_sprite.Play(animationName);
		}
	}
}
