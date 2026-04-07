using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// DualNPCSpawner - один объект-спавнер, который умеет спавнить ШЕСТЬ типов NPC пачками:
/// - Ghost
/// - BlackGhost
/// - Skelet
/// - Mummy
/// - Demon
/// - Boss
///
/// Босс:
/// - обычный спавн по таймеру (25 минут)
/// - принудительный спавн через ForceSpawnBossNow()
/// </summary>
public sealed class DualNPCSpawner : Component
{
	// =========================
	// Prefabs
	// =========================
	[Property, Group( "Prefabs" )] public GameObject GhostPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject BlackGhostPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject MummyPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject SkeletPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject DemonPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject BossPrefab { get; set; }

	// =========================
	// Common spawn shape
	// =========================
	[Property, Group( "Common" )] public float SpawnRadius { get; set; } = 500f;
	[Property, Group( "Common" )] public float MinSpawnDistance { get; set; } = 300f;
	[Property, Group( "Common" )] public float SpawnHeightOffset { get; set; } = 50f;

	[Property, Group( "Common" )] public bool UseFixedGroundHeight { get; set; } = false;
	[Property, Group( "Common" )] public float FixedGroundHeight { get; set; } = 0f;

	// =========================
	// Gate / lifecycle
	// =========================
	[Property, Group( "Gate" )] public bool SpawnOnlyDuringFight { get; set; } = true;
	[Property, Group( "Gate" )] public bool InstantSpawnOnFightStart { get; set; } = true;

	// =========================
	// Ghost scaling (waves)
	// =========================
	[Property, Group( "Ghost Waves" )] public float GhostEarlyCapSeconds { get; set; } = 30f;
	[Property, Group( "Ghost Waves" )] public int GhostMaxAtEarlyCap { get; set; } = 4;
	[Property, Group( "Ghost Waves" )] public int GhostMaxAt60Min { get; set; } = 40;

	[Property, Group( "Ghost Waves" )] public int GhostBatchAtEarlyCap { get; set; } = 4;
	[Property, Group( "Ghost Waves" )] public int GhostBatchAt60Min { get; set; } = 12;

	[Property, Group( "Ghost Waves" )] public float GhostWaveIntervalAtEarlyCap { get; set; } = 18f;
	[Property, Group( "Ghost Waves" )] public float GhostWaveIntervalAt60Min { get; set; } = 6f;

	[Property, Group( "Ghost Waves" )] public bool GhostSpawnOnFightStart { get; set; } = true;
	[Property, Group( "Ghost Waves" )] public float GhostExtraSpawnChance { get; set; } = 0.5f;

	// =========================
	// Black Ghost scaling (waves)
	// =========================
	[Property, Group( "Black Ghost Waves" )] public float BlackGhostStartDelaySeconds { get; set; } = 35f;

	[Property, Group( "Black Ghost Waves" )] public int BlackGhostMaxAtStart { get; set; } = 1;
	[Property, Group( "Black Ghost Waves" )] public int BlackGhostMaxAt60Min { get; set; } = 10;

	[Property, Group( "Black Ghost Waves" )] public int BlackGhostBatchAtStart { get; set; } = 1;
	[Property, Group( "Black Ghost Waves" )] public int BlackGhostBatchAt60Min { get; set; } = 3;

	[Property, Group( "Black Ghost Waves" )] public float BlackGhostWaveIntervalAtStart { get; set; } = 28f;
	[Property, Group( "Black Ghost Waves" )] public float BlackGhostWaveIntervalAt60Min { get; set; } = 12f;

	// =========================
	// Skelet scaling (waves)
	// =========================
	[Property, Group( "Skelet Waves" )] public float SkeletStartDelaySeconds { get; set; } = 18f;

	[Property, Group( "Skelet Waves" )] public int SkeletMaxAtStart { get; set; } = 2;
	[Property, Group( "Skelet Waves" )] public int SkeletMaxAt60Min { get; set; } = 18;

	[Property, Group( "Skelet Waves" )] public int SkeletBatchAtStart { get; set; } = 1;
	[Property, Group( "Skelet Waves" )] public int SkeletBatchAt60Min { get; set; } = 4;

	[Property, Group( "Skelet Waves" )] public float SkeletWaveIntervalAtStart { get; set; } = 16f;
	[Property, Group( "Skelet Waves" )] public float SkeletWaveIntervalAt60Min { get; set; } = 8f;

	// =========================
	// Mummy scaling (waves)
	// =========================
	[Property, Group( "Mummy Waves" )] public float MummyStartDelaySeconds { get; set; } = 45f;

	[Property, Group( "Mummy Waves" )] public int MummyMaxAtStart { get; set; } = 2;
	[Property, Group( "Mummy Waves" )] public int MummyMaxAt60Min { get; set; } = 10;

	[Property, Group( "Mummy Waves" )] public int MummyBatchAtStart { get; set; } = 1;
	[Property, Group( "Mummy Waves" )] public int MummyBatchAt60Min { get; set; } = 3;

	[Property, Group( "Mummy Waves" )] public float MummyWaveIntervalAtStart { get; set; } = 28f;
	[Property, Group( "Mummy Waves" )] public float MummyWaveIntervalAt60Min { get; set; } = 14f;

	// =========================
	// Demon scaling (waves)
	// =========================
	[Property, Group( "Demon Waves" )] public float DemonStartDelaySeconds { get; set; } = 75f;

	[Property, Group( "Demon Waves" )] public int DemonMaxAtStart { get; set; } = 1;
	[Property, Group( "Demon Waves" )] public int DemonMaxAt60Min { get; set; } = 6;

	[Property, Group( "Demon Waves" )] public int DemonBatchAtStart { get; set; } = 1;
	[Property, Group( "Demon Waves" )] public int DemonBatchAt60Min { get; set; } = 2;

	[Property, Group( "Demon Waves" )] public float DemonWaveIntervalAtStart { get; set; } = 40f;
	[Property, Group( "Demon Waves" )] public float DemonWaveIntervalAt60Min { get; set; } = 18f;

	// =========================
	// Boss
	// =========================
	[Property, Group( "Boss" )] public float BossStartDelaySeconds { get; set; } = 1500f; // 25 минут
	[Property, Group( "Boss" )] public bool BossSpawnOnlyOnce { get; set; } = true;
	[Property, Group( "Boss" )] public string BossObjectName { get; set; } = "npc_boss";
	[Property, Group( "Boss" )] public bool EnableSpawnerOnManualBossSummon { get; set; } = true;

	// =========================
	// Debug
	// =========================
	[Property] public bool DebugMode { get; set; } = true;

	// =========================
	// Runtime
	// =========================
	private GameObject _player;
	private GameManager _gm;

	private readonly List<GameObject> _ghostActive = new();
	private readonly List<GameObject> _blackGhostActive = new();
	private readonly List<GameObject> _skeletActive = new();
	private readonly List<GameObject> _mummyActive = new();
	private readonly List<GameObject> _demonActive = new();

	private GameObject _bossActive;
	private bool _bossSpawnedOnce = false;

	private TimeSince _ghostLastWave;
	private TimeSince _blackGhostLastWave;
	private TimeSince _skeletLastWave;
	private TimeSince _mummyLastWave;
	private TimeSince _demonLastWave;

	private TimeSince _fightTime;
	private bool _wasFighting = false;

	private int _ghostPending = 0;

	private int _ghostTotalSpawned = 0;
	private int _blackGhostTotalSpawned = 0;
	private int _skeletTotalSpawned = 0;
	private int _mummyTotalSpawned = 0;
	private int _demonTotalSpawned = 0;
	private int _bossTotalSpawned = 0;

	private readonly Random _rng = new Random();

	public int GhostActiveCount => _ghostActive.Count;
	public int BlackGhostActiveCount => _blackGhostActive.Count;
	public int SkeletActiveCount => _skeletActive.Count;
	public int MummyActiveCount => _mummyActive.Count;
	public int DemonActiveCount => _demonActive.Count;
	public int GhostPendingCount => _ghostPending;
	public bool BossIsAlive => _bossActive != null && _bossActive.IsValid;

	public void AddGhostToQueue( int count = 1 )
	{
		_ghostPending += Math.Max( 0, count );
	}

	public void ClearGhostQueue()
	{
		_ghostPending = 0;
	}

	/// <summary>
	/// Принудительный спавн босса в обход таймера.
	/// Возвращает true, если босс реально был заспавнен.
	/// </summary>
	public bool ForceSpawnBossNow()
	{
		EnsureRefs();
		CleanupBoss();

		if ( BossPrefab == null || !BossPrefab.IsValid )
		{
			if ( DebugMode )
				Log.Warning( "❌ ForceSpawnBossNow: BossPrefab не назначен." );
			return false;
		}

		if ( _player == null || !_player.IsValid )
		{
			if ( DebugMode )
				Log.Warning( "❌ ForceSpawnBossNow: игрок не найден." );
			return false;
		}

		if ( BossIsAlive )
		{
			if ( DebugMode )
				Log.Info( "ℹ️ ForceSpawnBossNow: босс уже жив, повторный спавн отменён." );
			return false;
		}

		if ( BossSpawnOnlyOnce && _bossSpawnedOnce )
		{
			if ( DebugMode )
				Log.Info( "ℹ️ ForceSpawnBossNow: BossSpawnOnlyOnce=true и босс уже был призван." );
			return false;
		}

		if ( EnableSpawnerOnManualBossSummon && !Enabled )
			Enabled = true;

		SpawnBoss();

		if ( DebugMode )
			Log.Info( "👹 Босс принудительно призван через trigger zone." );

		return true;
	}

	protected override void OnStart()
	{
		_player = Scene.GetAllComponents<Player>().FirstOrDefault()?.GameObject;
		_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		LogState( "OnStart" );
	}

	protected override void OnEnabled() => LogState( "OnEnabled" );
	protected override void OnDisabled() => LogState( "OnDisabled" );

	protected override void OnUpdate()
	{
		if ( !Enabled ) return;

		EnsureRefs();

		bool isFighting = IsFighting();

		if ( isFighting != _wasFighting )
		{
			_wasFighting = isFighting;

			if ( DebugMode )
				Log.Info( $"🌊 DualSpawner: Fighting -> {isFighting}" );

			if ( isFighting )
			{
				_fightTime = 0;

				if ( InstantSpawnOnFightStart )
				{
					_ghostLastWave = 999f;
					_blackGhostLastWave = 999f;
					_skeletLastWave = 999f;
					_mummyLastWave = 999f;
					_demonLastWave = 999f;
				}

				if ( GhostSpawnOnFightStart && GhostPrefab != null )
				{
					int earlyBatch = Math.Max( 0, GhostBatchAtEarlyCap );
					_ghostPending += earlyBatch;
					SpawnGhostsFromPending( GhostMaxAtEarlyCap );

					if ( DebugMode )
						Log.Info( $"👻 Start wave: +{earlyBatch} pending (early cap {GhostMaxAtEarlyCap})" );
				}
			}
		}

		if ( SpawnOnlyDuringFight && !isFighting )
			return;

		if ( _player == null || !_player.IsValid )
			return;

		CleanupGhost();
		CleanupBlackGhost();
		CleanupSkelet();
		CleanupMummy();
		CleanupDemon();
		CleanupBoss();

		float t = _fightTime;

		// Ghost waves
		if ( GhostPrefab != null )
		{
			ComputeGhostScaling( t, out int ghostMax, out int ghostBatch, out float ghostWaveInterval );

			bool canWaveGhost =
				t >= GhostEarlyCapSeconds &&
				_ghostLastWave > ghostWaveInterval;

			if ( canWaveGhost )
			{
				_ghostPending += ghostBatch;
				_ghostLastWave = 0;

				if ( DebugMode )
					Log.Info( $"👻 Ghost wave: t={t:F1}s max={ghostMax} batch=+{ghostBatch} interval={ghostWaveInterval:F1}s pending={_ghostPending}" );
			}

			SpawnGhostsFromPending( ghostMax );
		}

		// Black Ghost waves
		if ( BlackGhostPrefab != null )
		{
			if ( t >= BlackGhostStartDelaySeconds )
			{
				ComputeBlackGhostScaling( t, out int blackGhostMax, out int blackGhostBatch, out float blackGhostWaveInterval );

				bool canWaveBlackGhost = _blackGhostLastWave > blackGhostWaveInterval;

				if ( canWaveBlackGhost )
				{
					_blackGhostLastWave = 0;

					int need = Math.Max( 0, blackGhostMax - _blackGhostActive.Count );
					int toSpawn = Math.Min( blackGhostBatch, need );

					if ( toSpawn > 0 )
					{
						for ( int i = 0; i < toSpawn; i++ )
							SpawnBlackGhost();

						if ( DebugMode )
							Log.Info( $"🖤 BlackGhost wave: t={t:F1}s max={blackGhostMax} spawned={toSpawn}/{blackGhostBatch} interval={blackGhostWaveInterval:F1}s active={_blackGhostActive.Count}" );
					}
				}
			}
		}

		// Skelet waves
		if ( SkeletPrefab != null )
		{
			if ( t >= SkeletStartDelaySeconds )
			{
				ComputeSkeletScaling( t, out int skeletMax, out int skeletBatch, out float skeletWaveInterval );

				bool canWaveSkelet = _skeletLastWave > skeletWaveInterval;

				if ( canWaveSkelet )
				{
					_skeletLastWave = 0;

					int need = Math.Max( 0, skeletMax - _skeletActive.Count );
					int toSpawn = Math.Min( skeletBatch, need );

					if ( toSpawn > 0 )
					{
						for ( int i = 0; i < toSpawn; i++ )
							SpawnSkelet();

						if ( DebugMode )
							Log.Info( $"💀 Skelet wave: t={t:F1}s max={skeletMax} spawned={toSpawn}/{skeletBatch} interval={skeletWaveInterval:F1}s active={_skeletActive.Count}" );
					}
				}
			}
		}

		// Mummy waves
		if ( MummyPrefab != null )
		{
			if ( t >= MummyStartDelaySeconds )
			{
				ComputeMummyScaling( t, out int mummyMax, out int mummyBatch, out float mummyWaveInterval );

				bool canWaveMummy = _mummyLastWave > mummyWaveInterval;

				if ( canWaveMummy )
				{
					_mummyLastWave = 0;

					int need = Math.Max( 0, mummyMax - _mummyActive.Count );
					int toSpawn = Math.Min( mummyBatch, need );

					if ( toSpawn > 0 )
					{
						for ( int i = 0; i < toSpawn; i++ )
							SpawnMummy();

						if ( DebugMode )
							Log.Info( $"🧟 Mummy wave: t={t:F1}s max={mummyMax} spawned={toSpawn}/{mummyBatch} interval={mummyWaveInterval:F1}s active={_mummyActive.Count}" );
					}
				}
			}
		}

		// Demon waves
		if ( DemonPrefab != null )
		{
			if ( t >= DemonStartDelaySeconds )
			{
				ComputeDemonScaling( t, out int demonMax, out int demonBatch, out float demonWaveInterval );

				bool canWaveDemon = _demonLastWave > demonWaveInterval;

				if ( canWaveDemon )
				{
					_demonLastWave = 0;

					int need = Math.Max( 0, demonMax - _demonActive.Count );
					int toSpawn = Math.Min( demonBatch, need );

					if ( toSpawn > 0 )
					{
						for ( int i = 0; i < toSpawn; i++ )
							SpawnDemon();

						if ( DebugMode )
							Log.Info( $"😈 Demon wave: t={t:F1}s max={demonMax} spawned={toSpawn}/{demonBatch} interval={demonWaveInterval:F1}s active={_demonActive.Count}" );
					}
				}
			}
		}

		// Boss timer spawn
		if ( BossPrefab != null )
		{
			if ( t >= BossStartDelaySeconds )
			{
				bool canSpawnBoss = (_bossActive == null || !_bossActive.IsValid);

				if ( BossSpawnOnlyOnce && _bossSpawnedOnce )
					canSpawnBoss = false;

				if ( canSpawnBoss )
				{
					SpawnBoss();

					if ( DebugMode )
						Log.Info( $"👹 Boss spawned by timer at t={t:F1}s ({t / 60f:F1} min)" );
				}
			}
		}

		if ( DebugMode && Time.Now % 3f < 0.1f )
		{
			int gm = (GhostPrefab != null) ? ComputeGhostMaxOnly( _fightTime ) : 0;
			int bgm = (BlackGhostPrefab != null) ? ComputeBlackGhostMaxOnly( _fightTime ) : 0;
			int sm = (SkeletPrefab != null) ? ComputeSkeletMaxOnly( _fightTime ) : 0;
			int mm = (MummyPrefab != null) ? ComputeMummyMaxOnly( _fightTime ) : 0;
			int dm = (DemonPrefab != null) ? ComputeDemonMaxOnly( _fightTime ) : 0;

			Log.Info( $"🌀 DualSpawner: t={_fightTime:F1}s | ghost={_ghostActive.Count}/{gm} pending={_ghostPending} | blackGhost={_blackGhostActive.Count}/{bgm} | skelet={_skeletActive.Count}/{sm} | mummy={_mummyActive.Count}/{mm} | demon={_demonActive.Count}/{dm} | bossAlive={BossIsAlive} bossSpawnedOnce={_bossSpawnedOnce}" );
		}
	}

	// =========================
	// Cleanup
	// =========================
	private void CleanupGhost()
	{
		for ( int i = _ghostActive.Count - 1; i >= 0; i-- )
		{
			var go = _ghostActive[i];
			if ( go != null && go.IsValid )
				continue;

			_ghostActive.RemoveAt( i );
			_ghostPending++;

			if ( _rng.NextDouble() < GhostExtraSpawnChance )
			{
				_ghostPending++;
				if ( DebugMode ) Log.Info( $"🎲 DualSpawner: ghost extra spawn! pending={_ghostPending}" );
			}
		}
	}

	private void CleanupBlackGhost()
	{
		for ( int i = _blackGhostActive.Count - 1; i >= 0; i-- )
		{
			var go = _blackGhostActive[i];
			if ( go != null && go.IsValid )
				continue;

			_blackGhostActive.RemoveAt( i );
		}
	}

	private void CleanupSkelet()
	{
		for ( int i = _skeletActive.Count - 1; i >= 0; i-- )
		{
			var go = _skeletActive[i];
			if ( go != null && go.IsValid )
				continue;

			_skeletActive.RemoveAt( i );
		}
	}

	private void CleanupMummy()
	{
		for ( int i = _mummyActive.Count - 1; i >= 0; i-- )
		{
			var go = _mummyActive[i];
			if ( go != null && go.IsValid )
				continue;

			_mummyActive.RemoveAt( i );
		}
	}

	private void CleanupDemon()
	{
		for ( int i = _demonActive.Count - 1; i >= 0; i-- )
		{
			var go = _demonActive[i];
			if ( go != null && go.IsValid )
				continue;

			_demonActive.RemoveAt( i );
		}
	}

	private void CleanupBoss()
	{
		if ( _bossActive != null && _bossActive.IsValid )
			return;

		_bossActive = null;
	}

	// =========================
	// Spawning
	// =========================
	private void SpawnGhostsFromPending( int ghostMax )
	{
		int safety = 256;
		while ( safety-- > 0 && _ghostPending > 0 && _ghostActive.Count < ghostMax )
		{
			SpawnGhost();
			_ghostPending--;
		}
	}

	private void SpawnGhost()
	{
		var pos = PickSpawnPosition();
		var npc = GhostPrefab.Clone( pos );
		NpcBuffDirector.Instance?.ApplyTo( npc );

		_ghostTotalSpawned++;
		npc.Name = $"Ghost_{_ghostTotalSpawned}";
		npc.Transform.Rotation = Rotation.FromYaw( (float)(_rng.NextDouble() * 360.0) );

		var ghost = npc.Components.Get<NPC>();
		if ( ghost != null )
			ghost.TargetPlayer = _player;

		_ghostActive.Add( npc );

		if ( DebugMode )
			Log.Info( $"✅ Spawn Ghost {npc.Name} active={_ghostActive.Count} pending={_ghostPending}" );
	}

	private void SpawnBlackGhost()
	{
		var pos = PickSpawnPosition();
		var npc = BlackGhostPrefab.Clone( pos );
		NpcBuffDirector.Instance?.ApplyTo( npc );

		_blackGhostTotalSpawned++;
		npc.Name = $"BlackGhost_{_blackGhostTotalSpawned}";
		npc.Transform.Rotation = Rotation.FromYaw( (float)(_rng.NextDouble() * 360.0) );

		var blackGhost = npc.Components.Get<BlackGhostNPC>();
		if ( blackGhost != null )
			blackGhost.TargetPlayer = _player;

		_blackGhostActive.Add( npc );

		if ( DebugMode )
			Log.Info( $"✅ Spawn BlackGhost {npc.Name} active={_blackGhostActive.Count}" );
	}

	private void SpawnSkelet()
	{
		var pos = PickSpawnPosition();
		var npc = SkeletPrefab.Clone( pos );
		NpcBuffDirector.Instance?.ApplyTo( npc );

		_skeletTotalSpawned++;
		npc.Name = $"Skelet_{_skeletTotalSpawned}";
		npc.Transform.Rotation = Rotation.FromYaw( (float)(_rng.NextDouble() * 360.0) );

		var skelet = npc.Components.Get<SkeletNPC>();
		if ( skelet != null )
			skelet.TargetPlayer = _player;

		_skeletActive.Add( npc );

		if ( DebugMode )
			Log.Info( $"✅ Spawn Skelet {npc.Name} active={_skeletActive.Count}" );
	}

	private void SpawnMummy()
	{
		var pos = PickSpawnPosition();
		var npc = MummyPrefab.Clone( pos );
		NpcBuffDirector.Instance?.ApplyTo( npc );

		_mummyTotalSpawned++;
		npc.Name = $"Mummy_{_mummyTotalSpawned}";
		npc.Transform.Rotation = Rotation.FromYaw( (float)(_rng.NextDouble() * 360.0) );

		var mummy = npc.Components.Get<MummyNPC>();
		if ( mummy != null )
			mummy.TargetPlayer = _player;

		_mummyActive.Add( npc );

		if ( DebugMode )
			Log.Info( $"✅ Spawn Mummy {npc.Name} active={_mummyActive.Count}" );
	}

	private void SpawnDemon()
	{
		var pos = PickSpawnPosition();
		var npc = DemonPrefab.Clone( pos );
		NpcBuffDirector.Instance?.ApplyTo( npc );

		_demonTotalSpawned++;
		npc.Name = $"Demon_{_demonTotalSpawned}";
		npc.Transform.Rotation = Rotation.FromYaw( (float)(_rng.NextDouble() * 360.0) );

		var demon = npc.Components.Get<DemonNPC>();
		if ( demon != null )
			demon.TargetPlayer = _player;

		_demonActive.Add( npc );

		if ( DebugMode )
			Log.Info( $"✅ Spawn Demon {npc.Name} active={_demonActive.Count}" );
	}

	private void SpawnBoss()
	{
		var pos = PickSpawnPosition();
		var npc = BossPrefab.Clone( pos );
		NpcBuffDirector.Instance?.ApplyTo( npc );

		_bossTotalSpawned++;
		npc.Name = BossObjectName;
		npc.Transform.Rotation = Rotation.FromYaw( (float)(_rng.NextDouble() * 360.0) );

		var boss = npc.Components.Get<BossNPC>();
		if ( boss != null )
			boss.TargetPlayer = _player;

		_bossActive = npc;
		_bossSpawnedOnce = true;

		if ( DebugMode )
			Log.Info( $"✅ Spawn Boss {npc.Name}" );
	}

	// =========================
	// Scaling
	// =========================
	private void ComputeGhostScaling( float fightSeconds, out int max, out int batch, out float waveInterval )
	{
		if ( fightSeconds <= GhostEarlyCapSeconds )
		{
			max = GhostMaxAtEarlyCap;
			batch = GhostBatchAtEarlyCap;
			waveInterval = 999f;
			return;
		}

		float p = Clamp01( (fightSeconds - GhostEarlyCapSeconds) / (3600f - GhostEarlyCapSeconds) );

		max = RoundToInt( Lerp( GhostMaxAtEarlyCap, GhostMaxAt60Min, p ) );
		batch = RoundToInt( Lerp( GhostBatchAtEarlyCap, GhostBatchAt60Min, p ) );
		waveInterval = Lerp( GhostWaveIntervalAtEarlyCap, GhostWaveIntervalAt60Min, p );

		if ( max < GhostMaxAtEarlyCap ) max = GhostMaxAtEarlyCap;
		if ( batch < 1 ) batch = 1;
		if ( waveInterval < 0.5f ) waveInterval = 0.5f;
	}

	private void ComputeBlackGhostScaling( float fightSeconds, out int max, out int batch, out float waveInterval )
	{
		if ( fightSeconds <= BlackGhostStartDelaySeconds )
		{
			max = 0;
			batch = 0;
			waveInterval = 999f;
			return;
		}

		float p = Clamp01( (fightSeconds - BlackGhostStartDelaySeconds) / (3600f - BlackGhostStartDelaySeconds) );

		max = RoundToInt( Lerp( BlackGhostMaxAtStart, BlackGhostMaxAt60Min, p ) );
		batch = RoundToInt( Lerp( BlackGhostBatchAtStart, BlackGhostBatchAt60Min, p ) );
		waveInterval = Lerp( BlackGhostWaveIntervalAtStart, BlackGhostWaveIntervalAt60Min, p );

		if ( max < 0 ) max = 0;
		if ( batch < 1 ) batch = 1;
		if ( waveInterval < 1.0f ) waveInterval = 1.0f;
	}

	private void ComputeSkeletScaling( float fightSeconds, out int max, out int batch, out float waveInterval )
	{
		if ( fightSeconds <= SkeletStartDelaySeconds )
		{
			max = 0;
			batch = 0;
			waveInterval = 999f;
			return;
		}

		float p = Clamp01( (fightSeconds - SkeletStartDelaySeconds) / (3600f - SkeletStartDelaySeconds) );

		max = RoundToInt( Lerp( SkeletMaxAtStart, SkeletMaxAt60Min, p ) );
		batch = RoundToInt( Lerp( SkeletBatchAtStart, SkeletBatchAt60Min, p ) );
		waveInterval = Lerp( SkeletWaveIntervalAtStart, SkeletWaveIntervalAt60Min, p );

		if ( max < 0 ) max = 0;
		if ( batch < 1 ) batch = 1;
		if ( waveInterval < 1.0f ) waveInterval = 1.0f;
	}

	private void ComputeMummyScaling( float fightSeconds, out int max, out int batch, out float waveInterval )
	{
		if ( fightSeconds <= MummyStartDelaySeconds )
		{
			max = 0;
			batch = 0;
			waveInterval = 999f;
			return;
		}

		float p = Clamp01( (fightSeconds - MummyStartDelaySeconds) / (3600f - MummyStartDelaySeconds) );

		max = RoundToInt( Lerp( MummyMaxAtStart, MummyMaxAt60Min, p ) );
		batch = RoundToInt( Lerp( MummyBatchAtStart, MummyBatchAt60Min, p ) );
		waveInterval = Lerp( MummyWaveIntervalAtStart, MummyWaveIntervalAt60Min, p );

		if ( max < 0 ) max = 0;
		if ( batch < 1 ) batch = 1;
		if ( waveInterval < 1.0f ) waveInterval = 1.0f;
	}

	private int ComputeGhostMaxOnly( float fightSeconds )
	{
		ComputeGhostScaling( fightSeconds, out int max, out _, out _ );
		return max;
	}

	private int ComputeBlackGhostMaxOnly( float fightSeconds )
	{
		if ( fightSeconds < BlackGhostStartDelaySeconds ) return 0;
		ComputeBlackGhostScaling( fightSeconds, out int max, out _, out _ );
		return max;
	}

	private int ComputeSkeletMaxOnly( float fightSeconds )
	{
		if ( fightSeconds < SkeletStartDelaySeconds ) return 0;
		ComputeSkeletScaling( fightSeconds, out int max, out _, out _ );
		return max;
	}

	private int ComputeMummyMaxOnly( float fightSeconds )
	{
		if ( fightSeconds < MummyStartDelaySeconds ) return 0;
		ComputeMummyScaling( fightSeconds, out int max, out _, out _ );
		return max;
	}

	private void ComputeDemonScaling( float fightSeconds, out int max, out int batch, out float waveInterval )
	{
		if ( fightSeconds <= DemonStartDelaySeconds )
		{
			max = 0;
			batch = 0;
			waveInterval = 999f;
			return;
		}

		float p = Clamp01( (fightSeconds - DemonStartDelaySeconds) / (3600f - DemonStartDelaySeconds) );

		max = RoundToInt( Lerp( DemonMaxAtStart, DemonMaxAt60Min, p ) );
		batch = RoundToInt( Lerp( DemonBatchAtStart, DemonBatchAt60Min, p ) );
		waveInterval = Lerp( DemonWaveIntervalAtStart, DemonWaveIntervalAt60Min, p );

		if ( max < 0 ) max = 0;
		if ( batch < 1 ) batch = 1;
		if ( waveInterval < 1.0f ) waveInterval = 1.0f;
	}

	private int ComputeDemonMaxOnly( float fightSeconds )
	{
		if ( fightSeconds < DemonStartDelaySeconds ) return 0;
		ComputeDemonScaling( fightSeconds, out int max, out _, out _ );
		return max;
	}

	// =========================
	// Position / refs / gate
	// =========================
	private Vector3 PickSpawnPosition()
	{
		float angle = (float)(_rng.NextDouble() * 360.0);
		Vector3 dir = Rotation.FromYaw( angle ).Forward;
		float dist = Lerp( MinSpawnDistance, SpawnRadius, (float)_rng.NextDouble() );

		var spawnPos = _player.Transform.Position + dir * dist;
		spawnPos += Vector3.Up * SpawnHeightOffset;

		if ( UseFixedGroundHeight )
			spawnPos = spawnPos.WithZ( FixedGroundHeight );

		return spawnPos;
	}

	private void EnsureRefs()
	{
		if ( _gm == null || !_gm.IsValid )
			_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		if ( _player == null || !_player.IsValid )
			_player = Scene.GetAllComponents<Player>().FirstOrDefault()?.GameObject;
	}

	private bool IsFighting()
	{
		if ( _gm == null || !_gm.IsValid )
			return true;

		return _gm.CurrentState == GameManager.GameState.Fighting;
	}

	private void LogState( string where )
	{
		if ( !DebugMode ) return;

		Log.Info( $"🌀 DualSpawner {where}: Enabled={Enabled} SpawnOnlyDuringFight={SpawnOnlyDuringFight} GhostPrefab={(GhostPrefab != null)} BlackGhostPrefab={(BlackGhostPrefab != null)} SkeletPrefab={(SkeletPrefab != null)} MummyPrefab={(MummyPrefab != null)} DemonPrefab={(DemonPrefab != null)} BossPrefab={(BossPrefab != null)}" );
	}

	// =========================
	// Math helpers
	// =========================
	private static float Lerp( float a, float b, float t )
	{
		t = Clamp01( t );
		return a + (b - a) * t;
	}

	private static float Clamp01( float t )
	{
		if ( t < 0f ) return 0f;
		if ( t > 1f ) return 1f;
		return t;
	}

	private static int RoundToInt( float v )
	{
		return (int)MathF.Round( v );
	}
}
