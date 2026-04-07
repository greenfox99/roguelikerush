using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class MummySpawner : Component
{
	[Property] public GameObject NPCPrefab { get; set; }

	[Property] public int MaxNPCs { get; set; } = 5;
	[Property] public float SpawnRadius { get; set; } = 500f;
	[Property] public float MinSpawnDistance { get; set; } = 300f;
	[Property] public float SpawnInterval { get; set; } = 5f;
	[Property] public float GroundHeight { get; set; } = 0f;

	[Property] public bool DebugMode { get; set; } = true;

	// ✅ ЖЁСТКИЙ ГЕЙТ: пока не Fighting — ноль спавна
	[Property, Group( "Gate" )] public bool SpawnOnlyDuringFight { get; set; } = true;

	// ✅ Если хочешь, чтобы в момент старта боя сразу добило до MaxNPCs
	[Property, Group( "Gate" )] public bool FillToMaxOnFightStart { get; set; } = true;

	private readonly List<GameObject> _activeNPCs = new();
	private TimeSince _lastSpawn;
	private GameObject _player;

	private GameManager _gm;
	private bool _wasFighting = false;

	protected override void OnStart()
	{
		_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		_player = Scene.GetAllComponents<Player>().FirstOrDefault()?.GameObject;

		LogState( "OnStart" );
	}

	protected override void OnEnabled()
	{
		LogState( "OnEnabled" );
	}

	protected override void OnDisabled()
	{
		LogState( "OnDisabled" );
	}

	protected override void OnUpdate()
	{
		// 0) Ищем менеджер/игрока при необходимости
		if ( _gm == null || !_gm.IsValid )
			_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		if ( _player == null || !_player.IsValid )
			_player = Scene.GetAllComponents<Player>().FirstOrDefault()?.GameObject;

		// 1) Проверяем состояние боя
		bool isFighting = _gm != null && _gm.CurrentState == GameManager.GameState.Fighting;

		// лог только при смене состояния
		if ( isFighting != _wasFighting )
		{
			_wasFighting = isFighting;
			if ( DebugMode )
				Log.Info( $"🧟 MummySpawner: Fighting changed -> {isFighting}" );

			// Если бой начался — можно сразу заполнить до MaxNPCs (быстро)
			if ( isFighting && FillToMaxOnFightStart )
			{
				_lastSpawn = SpawnInterval; // чтобы не ждать интервал
			}
		}

		// 2) ✅ ЖЁСТКИЙ ГЕЙТ
		if ( SpawnOnlyDuringFight && !isFighting )
			return;

		// 3) Без игрока не спавним
		if ( _player == null || !_player.IsValid )
			return;

		// 4) Чистим список
		for ( int i = _activeNPCs.Count - 1; i >= 0; i-- )
		{
			if ( _activeNPCs[i] == null || !_activeNPCs[i].IsValid )
				_activeNPCs.RemoveAt( i );
		}

		// 5) Спавним до MaxNPCs, но только во время боя
		if ( _activeNPCs.Count < MaxNPCs && _lastSpawn > SpawnInterval )
		{
			SpawnMummy();
			_lastSpawn = 0;
		}

		if ( DebugMode && Time.Now % 3f < 0.1f )
			Log.Info( $"🧟 Active mummies: {_activeNPCs.Count}/{MaxNPCs} (Fighting={isFighting}, Enabled={Enabled})" );
	}

	private void SpawnMummy()
	{
		if ( NPCPrefab == null )
		{
			Log.Error( "❌ MummySpawner: NPCPrefab не назначен!" );
			return;
		}

		int maxAttempts = 30;

		for ( int attempt = 0; attempt < maxAttempts; attempt++ )
		{
			var randomAngle = Random.Shared.Float( 0, 360 );
			var randomDir = Rotation.FromYaw( randomAngle ).Forward;
			var distance = Random.Shared.Float( MinSpawnDistance, SpawnRadius );

			var spawnPos = new Vector3(
				_player.Transform.Position.x + randomDir.x * distance,
				_player.Transform.Position.y + randomDir.y * distance,
				GroundHeight
			);

			bool tooClose = false;
			foreach ( var m in _activeNPCs )
			{
				if ( m != null && m.IsValid && Vector3.DistanceBetween( spawnPos, m.Transform.Position ) < 200f )
				{
					tooClose = true;
					break;
				}
			}
			if ( tooClose ) continue;

			var mummyObj = NPCPrefab.Clone( spawnPos );
			mummyObj.Name = $"Mummy_{_activeNPCs.Count + 1}";
			mummyObj.Transform.Rotation = Rotation.FromYaw( Random.Shared.Float( 0, 360 ) );

			var mummyComp = mummyObj.Components.Get<MummyNPC>();
			if ( mummyComp != null )
				mummyComp.TargetPlayer = _player;

			_activeNPCs.Add( mummyObj );

			if ( DebugMode )
				Log.Info( $"✅ Spawn mummy: {mummyObj.Name} at {spawnPos}" );

			return;
		}

		if ( DebugMode )
			Log.Warning( "⚠️ MummySpawner: не удалось найти место после 30 попыток" );
	}

	private void LogState( string where )
	{
		if ( !DebugMode ) return;

		string gmState = "GM=null";
		if ( _gm != null && _gm.IsValid )
			gmState = $"GM={_gm.CurrentState}";

		Log.Info( $"🧟 MummySpawner {where}: Enabled={Enabled}, {gmState}" );
	}
}
