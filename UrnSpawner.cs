using Sandbox;
using System;
using System.Linq;

public sealed class UrnSpawner : Component
{
	[Property] public GameObject UrnPrefab { get; set; }

	[Property, Group( "Game" )] public GameObject GameManagerObject { get; set; }
	[Property, Group( "Game" )] public bool AutoFindGameManager { get; set; } = true;

	[Property, Group( "Fight Spawn" )] public float SpawnIntervalDuringFight { get; set; } = 8f;
	[Property, Group( "Fight Spawn" )] public int MaxActiveUrns { get; set; } = 6;
	[Property, Group( "Fight Spawn" )] public bool SpawnOneImmediatelyOnFightStart { get; set; } = true;
	[Property, Group( "Fight Spawn" )] public bool ClearUrnsWhenFightStops { get; set; } = true;
	[Property, Group( "Fight Spawn" )] public int MaxAttemptsPerSpawn { get; set; } = 18;

	[Property, Group( "Area" )] public Vector2 SpawnAreaSize { get; set; } = new Vector2( 4000f, 4000f );
	[Property, Group( "Area" )] public float TraceStartHeight { get; set; } = 2000f;
	[Property, Group( "Area" )] public float TraceLength { get; set; } = 5000f;
	[Property, Group( "Area" )] public float SpawnHeightOffset { get; set; } = 2f;
	[Property, Group( "Area" )] public float MinGroundNormalZ { get; set; } = 0.65f;

	[Property, Group( "Rules" )] public float MinDistanceFromPlayer { get; set; } = 350f;
	[Property, Group( "Rules" )] public float MinDistanceFromOtherUrns { get; set; } = 220f;

	private Player _player;
	private GameManager _gameManager;

	private TimeSince _spawnTimer;
	private TimeSince _refindTimer;

	private bool _wasFightingLastFrame;

	protected override void OnStart()
	{
		FindRefs();
		_spawnTimer = 0f;
		_wasFightingLastFrame = IsFightActive();

		if ( !_wasFightingLastFrame && ClearUrnsWhenFightStops )
		{
			ClearAllUrns();
		}
	}

	protected override void OnUpdate()
	{
		if ( _refindTimer > 1f )
		{
			_refindTimer = 0f;

			if ( _player == null || !_player.IsValid || _gameManager == null || !_gameManager.IsValid )
			{
				FindRefs();
			}
		}

		bool isFighting = IsFightActive();

		if ( isFighting && !_wasFightingLastFrame )
		{
			_spawnTimer = 9999f;

			if ( SpawnOneImmediatelyOnFightStart && GetActiveUrnCount() < MaxActiveUrns )
			{
				TrySpawnOne();
				_spawnTimer = 0f;
			}
		}

		if ( !isFighting && _wasFightingLastFrame )
		{
			if ( ClearUrnsWhenFightStops )
			{
				ClearAllUrns();
			}

			_spawnTimer = 0f;
		}

		_wasFightingLastFrame = isFighting;

		if ( !isFighting )
			return;

		if ( GetActiveUrnCount() >= MaxActiveUrns )
			return;

		if ( _spawnTimer < SpawnIntervalDuringFight )
			return;

		_spawnTimer = 0f;
		TrySpawnOne();
	}

	private void FindRefs()
	{
		_player = Scene.GetAllComponents<Player>().FirstOrDefault();

		if ( GameManagerObject != null && GameManagerObject.IsValid )
		{
			_gameManager = GameManagerObject.Components.Get<GameManager>();
		}

		if ( (_gameManager == null || !_gameManager.IsValid) && AutoFindGameManager )
		{
			_gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		}
	}

	private bool IsFightActive()
	{
		if ( _gameManager == null || !_gameManager.IsValid )
			return false;

		return _gameManager.CurrentState == GameManager.GameState.Fighting;
	}

	private int GetActiveUrnCount()
	{
		return Scene.GetAllComponents<BreakableUrn>()
			.Count( x => x != null && x.IsValid && !x.IsBroken );
	}

	private void ClearAllUrns()
	{
		foreach ( var urn in Scene.GetAllComponents<BreakableUrn>().ToList() )
		{
			if ( urn == null || !urn.IsValid || urn.GameObject == null || !urn.GameObject.IsValid )
				continue;

			urn.GameObject.Destroy();
		}
	}

	private bool TrySpawnOne()
	{
		if ( UrnPrefab == null )
			return false;

		for ( int i = 0; i < MaxAttemptsPerSpawn; i++ )
		{
			if ( !TryFindGroundPoint( out var pos ) )
				continue;

			if ( !IsFarEnoughFromPlayer( pos ) )
				continue;

			if ( !IsFarEnoughFromOtherUrns( pos ) )
				continue;

			var urn = UrnPrefab.Clone( pos + Vector3.Up * SpawnHeightOffset );
			urn.Name = "BreakableUrn";

			float yaw = (float)(Random.Shared.NextDouble() * 360f);
			urn.Transform.Rotation = Rotation.FromYaw( yaw );

			return true;
		}

		return false;
	}

	private bool TryFindGroundPoint( out Vector3 pos )
	{
		float x = ((float)Random.Shared.NextDouble() - 0.5f) * SpawnAreaSize.x;
		float y = ((float)Random.Shared.NextDouble() - 0.5f) * SpawnAreaSize.y;

		Vector3 sample = Transform.Position + new Vector3( x, y, 0f );
		Vector3 start = sample + Vector3.Up * TraceStartHeight;
		Vector3 end = start + Vector3.Down * TraceLength;

		var tr = Scene.Trace.Ray( start, end ).Run();

		if ( !tr.Hit )
		{
			pos = default;
			return false;
		}

		if ( tr.Normal.z < MinGroundNormalZ )
		{
			pos = default;
			return false;
		}

		pos = tr.HitPosition;
		return true;
	}

	private bool IsFarEnoughFromPlayer( Vector3 pos )
	{
		if ( _player == null || !_player.IsValid )
			return true;

		return Vector3.DistanceBetween( pos, _player.Transform.Position ) >= MinDistanceFromPlayer;
	}

	private bool IsFarEnoughFromOtherUrns( Vector3 pos )
	{
		foreach ( var urn in Scene.GetAllComponents<BreakableUrn>() )
		{
			if ( urn == null || !urn.IsValid || urn.IsBroken )
				continue;

			float dist = Vector3.DistanceBetween( pos, urn.Transform.Position );
			if ( dist < MinDistanceFromOtherUrns )
				return false;
		}

		return true;
	}
}
