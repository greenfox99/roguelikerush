using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ЕДИНЫЙ СПАВНЕР ДЛЯ ПРИЗРАКОВ И МУМИЙ (1 скрипт вместо 2).
///
/// Как использовать:
/// - Оставляешь ДВА GameObject в сцене (GhostSpawnerObject и MummySpawnerObject) как сейчас.
/// - На каждый вешаешь ЭТОТ компонент NPCSpawner.
/// - На GhostSpawner:
///     Mode = QueueOnDeath
///     NPCPrefab = Ghost prefab
///     ExtraSpawnChance / SpawnOnStart как раньше у призраков
/// - На MummySpawner:
///     Mode = MaintainMaxDuringFight
///     NPCPrefab = Mummy prefab
///     MaxConcurrentNPCs / SpawnInterval как раньше у мумий
///
/// Важно:
/// - НЕ ломаем GameManager: он как и раньше Components.Get<NPCSpawner>() с обоих объектов.
/// - НЕ используем Random.Shared.
/// - Можно включать/выключать спавнер через Enabled (как у тебя сейчас).
/// - Опционально умеет "гейтить" спавн только во время GameState.Fighting (если нужно).
/// </summary>
public sealed class NPCSpawner : Component
{
	public enum SpawnerMode
	{
		/// <summary>
		/// Режим призраков: есть очередь _pendingSpawns, которая растёт когда NPC умирает,
		/// плюс шанс на дополнительный спавн.
		/// </summary>
		QueueOnDeath = 0,

		/// <summary>
		/// Режим мумий: просто поддерживаем максимум активных NPC.
		/// </summary>
		MaintainMaxDuringFight = 1
	}

	// =========================
	// CONFIG
	// =========================
	[Property] public SpawnerMode Mode { get; set; } = SpawnerMode.QueueOnDeath;

	[Property] public GameObject NPCPrefab { get; set; }

	// Общие
	[Property] public int MaxConcurrentNPCs { get; set; } = 10;
	[Property] public float SpawnRadius { get; set; } = 500f;
	[Property] public float MinSpawnDistance { get; set; } = 300f;
	[Property] public float SpawnInterval { get; set; } = 2f;

	/// <summary>Подъём точки спавна относительно игрока (обычно 0-80).</summary>
	[Property] public float SpawnHeightOffset { get; set; } = 50f;

	/// <summary>Если хочешь жёстко фиксировать Z (как GroundHeight в MummySpawner) — включи.</summary>
	[Property] public bool UseFixedGroundHeight { get; set; } = false;

	/// <summary>Используется только если UseFixedGroundHeight=true.</summary>
	[Property] public float FixedGroundHeight { get; set; } = 0f;

	[Property] public bool DebugMode { get; set; } = true;

	// ===== Gate (как в mummy spawner) =====
	[Property, Group( "Gate" )] public bool SpawnOnlyDuringFight { get; set; } = false;

	/// <summary>
	/// Если бой начался — можно сразу ускорить первый спавн (для мумий приятнее).
	/// Работает в обоих режимах.
	/// </summary>
	[Property, Group( "Gate" )] public bool FillToMaxOnFightStart { get; set; } = true;

	// ===== Queue mode (ghost) =====
	[Property, Group( "Queue Mode" )] public bool SpawnOnStart { get; set; } = true;

	/// <summary>Шанс доп. спавна при смерти (используется только в QueueOnDeath).</summary>
	[Property, Group( "Queue Mode" )] public float ExtraSpawnChance { get; set; } = 0.5f;

	// =========================
	// RUNTIME
	// =========================
	private readonly List<GameObject> _activeNPCs = new();
	private TimeSince _lastSpawn;
	private GameObject _player;
	private GameManager _gm;

	private bool _wasFighting = false;

	private int _totalSpawned = 0;
	private int _pendingSpawns = 0;

	// локальный RNG
	private readonly Random _rng = new Random();

	// public (на будущее для волн)
	public int ActiveCount => _activeNPCs.Count;
	public int PendingCount => _pendingSpawns;

	public void AddSpawnToQueue( int count = 1 )
	{
		_pendingSpawns += Math.Max( 0, count );
	}

	public void ClearQueue()
	{
		_pendingSpawns = 0;
	}

	protected override void OnStart()
	{
		_player = Scene.GetAllComponents<Player>().FirstOrDefault()?.GameObject;
		_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		if ( Mode == SpawnerMode.QueueOnDeath && SpawnOnStart )
		{
			_pendingSpawns = Math.Max( 0, MaxConcurrentNPCs );
			if ( DebugMode )
				Log.Info( $"👾 NPCSpawner({Mode}) старт: +{_pendingSpawns} в очередь" );
		}

		LogState( "OnStart" );
	}

	protected override void OnEnabled()
	{
		LogState( "OnEnabled" );

		// при включении часто хотят "не ждать интервал" если в бою
		if ( FillToMaxOnFightStart )
			_lastSpawn = SpawnInterval;
	}

	protected override void OnDisabled()
	{
		LogState( "OnDisabled" );
	}

	protected override void OnUpdate()
	{
		if ( !Enabled ) return;

		EnsureRefs();

		bool isFighting = IsFighting();

		// лог + реакция на смену боевого состояния
		if ( isFighting != _wasFighting )
		{
			_wasFighting = isFighting;

			if ( DebugMode )
				Log.Info( $"👾 NPCSpawner({Mode}): Fighting changed -> {isFighting}" );

			if ( isFighting && FillToMaxOnFightStart )
				_lastSpawn = SpawnInterval; // чтобы спавн пошел сразу
		}

		// гейт
		if ( SpawnOnlyDuringFight && !isFighting )
			return;

		// без игрока не работаем
		if ( _player == null || !_player.IsValid )
			return;

		// чистим список активных
		CleanupActive();

		// логика по режиму
		if ( Mode == SpawnerMode.QueueOnDeath )
		{
			TickQueueMode();
		}
		else
		{
			TickMaintainMaxMode();
		}

		if ( DebugMode && Time.Now % 3f < 0.1f )
		{
			Log.Info( $"👾 NPCSpawner({Mode}) active={_activeNPCs.Count}/{MaxConcurrentNPCs} pending={_pendingSpawns} fighting={isFighting}" );
		}
	}

	private void TickQueueMode()
	{
		// если кто-то умер — увеличиваем очередь (делается в CleanupActive через флаг)
		// спавним из очереди
		if ( _pendingSpawns > 0 && _activeNPCs.Count < MaxConcurrentNPCs && _lastSpawn > SpawnInterval )
		{
			SpawnOne();
			_pendingSpawns--;
			_lastSpawn = 0;
		}
	}

	private void TickMaintainMaxMode()
	{
		// просто добиваем до максимума
		if ( _activeNPCs.Count < MaxConcurrentNPCs && _lastSpawn > SpawnInterval )
		{
			SpawnOne();
			_lastSpawn = 0;
		}
	}

	private void CleanupActive()
	{
		// Важно: очередь при смерти нужна только для QueueOnDeath.
		// Для MaintainMax мы просто удаляем невалидные.
		for ( int i = _activeNPCs.Count - 1; i >= 0; i-- )
		{
			var go = _activeNPCs[i];

			if ( go != null && go.IsValid )
				continue;

			_activeNPCs.RemoveAt( i );

			if ( Mode == SpawnerMode.QueueOnDeath )
			{
				_pendingSpawns++;

				// шанс на доп.спавн (как раньше у призраков)
				if ( _rng.NextDouble() < ExtraSpawnChance )
				{
					_pendingSpawns++;
					if ( DebugMode )
						Log.Info( $"🎲 Extra spawn! pending={_pendingSpawns}" );
				}

				if ( DebugMode )
					Log.Info( $"💀 NPC died. pending={_pendingSpawns}" );
			}
		}
	}

	private void SpawnOne()
	{
		if ( NPCPrefab == null )
		{
			Log.Error( $"❌ NPCSpawner({Mode}): NPCPrefab не назначен!" );
			return;
		}

		// позиция вокруг игрока
		float angle = (float)(_rng.NextDouble() * 360.0);
		Vector3 dir = Rotation.FromYaw( angle ).Forward;

		float dist = Lerp( MinSpawnDistance, SpawnRadius, (float)_rng.NextDouble() );

		var spawnPos = _player.Transform.Position + dir * dist;
		spawnPos += Vector3.Up * SpawnHeightOffset;

		if ( UseFixedGroundHeight )
			spawnPos = spawnPos.WithZ( FixedGroundHeight );

		var npc = NPCPrefab.Clone( spawnPos );

		_totalSpawned++;
		npc.Name = $"{NPCPrefab.Name}_{_totalSpawned}";
		npc.Transform.Rotation = Rotation.FromYaw( (float)(_rng.NextDouble() * 360.0) );

		// назначаем цель (поддерживаем оба типа NPC)
		var ghost = npc.Components.Get<NPC>();
		if ( ghost != null )
			ghost.TargetPlayer = _player;

		var mummy = npc.Components.Get<MummyNPC>();
		if ( mummy != null )
			mummy.TargetPlayer = _player;

		_activeNPCs.Add( npc );

		if ( DebugMode )
			Log.Info( $"✅ Spawned {npc.Name} ({Mode}). active={_activeNPCs.Count}/{MaxConcurrentNPCs} pending={_pendingSpawns}" );
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
		// если менеджера нет — считаем что “можно спавнить” (чтобы не зависнуть)
		if ( _gm == null || !_gm.IsValid )
			return true;

		return _gm.CurrentState == GameManager.GameState.Fighting;
	}

	private void LogState( string where )
	{
		if ( !DebugMode ) return;

		Log.Info( $"👾 NPCSpawner({Mode}) {where}: Enabled={Enabled} Max={MaxConcurrentNPCs} Interval={SpawnInterval:0.00}" );
	}

	private static float Lerp( float a, float b, float t )
	{
		if ( t < 0f ) t = 0f;
		if ( t > 1f ) t = 1f;
		return a + (b - a) * t;
	}
}
