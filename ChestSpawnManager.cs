using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed class ChestSpawnManager : Component
{
	[Property] public GameObject ChestPrefab { get; set; }
	[Property] public GameManager GameManager { get; set; }

	[Property, Group( "Battle Spawn" )] public bool SpawnOnlyDuringFight { get; set; } = true;
	[Property, Group( "Battle Spawn" )] public bool SpawnWaveOnFightStart { get; set; } = true;
	[Property, Group( "Battle Spawn" )] public int SpawnCountOnFightStart { get; set; } = 1;

	[Property, Group( "Spawn" )] public float SpawnIntervalSeconds { get; set; } = 45f;
	[Property, Group( "Spawn" )] public int SpawnPerWave { get; set; } = 1;
	[Property, Group( "Spawn" )] public int MaxAliveChests { get; set; } = 1;

	[Property, Group( "Spawn" )] public bool AvoidSameSpawnPointTwiceInRow { get; set; } = true;

	private readonly Random _rng = new();
	private readonly List<GameObject> _aliveChests = new();
	private readonly Dictionary<GameObject, ChestSpawnPoint> _chestToPoint = new();

	private TimeSince _sinceLastWave;
	private ChestSpawnPoint _lastSpawnPoint;
	private bool _wasFightActiveLastFrame;

	protected override void OnStart()
	{
		GameManager ??= Scene.GetAllComponents<GameManager>().FirstOrDefault();
		_sinceLastWave = 0f;
		_wasFightActiveLastFrame = IsFightActive();
	}

	protected override void OnUpdate()
	{
		CleanupDestroyedChests();

		bool fightActive = IsFightActive();

		// Переход в бой
		if ( fightActive && !_wasFightActiveLastFrame )
		{
			_sinceLastWave = 0f;

			if ( SpawnWaveOnFightStart )
			{
				int toSpawn = Math.Min( SpawnCountOnFightStart, Math.Max( 0, MaxAliveChests - _aliveChests.Count ) );
				if ( toSpawn > 0 )
					SpawnUpTo( toSpawn );
			}
		}

		// Вне боя сундуки не спавнятся, таймер не копится
		if ( SpawnOnlyDuringFight && !fightActive )
		{
			_sinceLastWave = 0f;
			_wasFightActiveLastFrame = false;
			return;
		}

		int missing = Math.Max( 0, MaxAliveChests - _aliveChests.Count );
		if ( missing <= 0 )
		{
			_wasFightActiveLastFrame = fightActive;
			return;
		}

		if ( SpawnIntervalSeconds > 0f && _sinceLastWave < SpawnIntervalSeconds )
		{
			_wasFightActiveLastFrame = fightActive;
			return;
		}

		int count = Math.Min( SpawnPerWave, missing );
		if ( count > 0 )
		{
			SpawnUpTo( count );
			_sinceLastWave = 0f;
		}

		_wasFightActiveLastFrame = fightActive;
	}

	public void SpawnRandomChest()
	{
		if ( SpawnOnlyDuringFight && !IsFightActive() )
			return;

		SpawnUpTo( 1 );
		_sinceLastWave = 0f;
	}

	public void SpawnWaveNow()
	{
		if ( SpawnOnlyDuringFight && !IsFightActive() )
			return;

		int missing = Math.Max( 0, MaxAliveChests - _aliveChests.Count );
		if ( missing <= 0 )
			return;

		int toSpawn = Math.Min( SpawnPerWave, missing );
		if ( toSpawn <= 0 )
			return;

		SpawnUpTo( toSpawn );
		_sinceLastWave = 0f;
	}

	public void NotifyChestOpened( SkillChest chest )
	{
		if ( chest == null || chest.GameObject == null )
			return;

		RemoveTrackedChest( chest.GameObject );
	}

	private bool IsFightActive()
	{
		if ( GameManager == null || !GameManager.IsValid )
			GameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		if ( GameManager == null )
			return !SpawnOnlyDuringFight;

		return GameManager.CurrentState == GameManager.GameState.Fighting;
	}

	private void SpawnUpTo( int count )
	{
		if ( ChestPrefab == null )
		{
			Log.Warning( "ChestSpawnManager: ChestPrefab is null." );
			return;
		}

		if ( MaxAliveChests <= 0 )
			return;

		count = Math.Max( 0, count );

		for ( int i = 0; i < count; i++ )
		{
			if ( _aliveChests.Count >= MaxAliveChests )
				break;

			var point = PickSpawnPoint();
			if ( point == null )
				break;

			var chestGo = ChestPrefab.Clone( point.Transform.Position );
			chestGo.Transform.Rotation = point.Transform.Rotation;

			_aliveChests.Add( chestGo );
			_chestToPoint[chestGo] = point;
			_lastSpawnPoint = point;

			var chest = chestGo.Components.Get<SkillChest>( FindMode.EverythingInSelf );
			if ( chest != null )
				chest.SpawnManager = this;
		}
	}

	private ChestSpawnPoint PickSpawnPoint()
	{
		var allPoints = Scene.GetAllComponents<ChestSpawnPoint>()
			.Where( x => x.EnabledForSpawn && x.GameObject != null && x.GameObject.IsValid )
			.ToList();

		if ( allPoints.Count == 0 )
		{
			Log.Warning( "ChestSpawnManager: no ChestSpawnPoint found." );
			return null;
		}

		var occupied = new HashSet<ChestSpawnPoint>(
			_chestToPoint.Values.Where( x => x != null && x.IsValid )
		);

		var freePoints = allPoints
			.Where( x => !occupied.Contains( x ) )
			.ToList();

		if ( freePoints.Count == 0 )
			return null;

		if ( AvoidSameSpawnPointTwiceInRow && _lastSpawnPoint != null && freePoints.Count > 1 )
			freePoints.Remove( _lastSpawnPoint );

		if ( freePoints.Count == 0 )
			return null;

		return freePoints[_rng.Next( 0, freePoints.Count )];
	}

	private void CleanupDestroyedChests()
	{
		for ( int i = _aliveChests.Count - 1; i >= 0; i-- )
		{
			var go = _aliveChests[i];
			if ( go == null || !go.IsValid )
			{
				_aliveChests.RemoveAt( i );
				_chestToPoint.Remove( go );
			}
		}
	}

	private void RemoveTrackedChest( GameObject chestGo )
	{
		if ( chestGo == null )
			return;

		_aliveChests.Remove( chestGo );
		_chestToPoint.Remove( chestGo );
	}
}
