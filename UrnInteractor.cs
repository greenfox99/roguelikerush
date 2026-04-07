using Sandbox;
using System.Linq;

public sealed class UrnInteractor : Component
{
	[Property] public string UseAction { get; set; } = "use";

	public BreakableUrn FocusedUrn { get; private set; }

	private Player _player;
	private TimeSince _refindTimer;

	protected override void OnStart()
	{
		FindPlayer();
	}

	protected override void OnUpdate()
	{
		if ( _player == null || !_player.IsValid )
		{
			if ( _refindTimer > 1f )
			{
				_refindTimer = 0f;
				FindPlayer();
			}

			FocusedUrn = null;
			return;
		}

		FocusedUrn = FindFocusedUrn();

		if ( FocusedUrn != null && Input.Pressed( UseAction ) )
		{
			FocusedUrn.Break( _player );
		}
	}

	private void FindPlayer()
	{
		_player = Scene.GetAllComponents<Player>().FirstOrDefault();
	}

	private BreakableUrn FindFocusedUrn()
	{
		BreakableUrn best = null;
		float bestDist = float.MaxValue;

		Vector3 playerPos = _player.Transform.Position;

		foreach ( var urn in Scene.GetAllComponents<BreakableUrn>() )
		{
			if ( urn == null || !urn.IsValid || urn.IsBroken )
				continue;

			if ( !urn.CanUse( _player ) )
				continue;

			float dist = Vector3.DistanceBetween( playerPos, urn.Transform.Position );
			if ( dist < bestDist )
			{
				bestDist = dist;
				best = urn;
			}
		}

		return best;
	}
}
