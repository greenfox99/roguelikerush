using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class LootSpawner : Component
{
	[Property] public GameObject LootChestPrefab { get; set; }
	[Property] public int MaxChests { get; set; } = 5;
	[Property] public float SpawnRadius { get; set; } = 1000f;
	[Property] public float MinSpawnDistance { get; set; } = 300f;
	[Property] public float SpawnInterval { get; set; } = 30f;
	[Property] public bool SpawnOnStart { get; set; } = false;
	[Property] public bool DebugMode { get; set; } = true;

	private List<GameObject> _activeChests = new();
	private TimeSince _lastSpawn;
	private GameObject _player;
	private bool _hasSpawnedFirst = false;

	private bool _isSpawningActive = false;

	protected override void OnStart()
	{
		Log.Info( $"📦 LootSpawner готов. Макс сундуков: {MaxChests}" );
	}

	protected override void OnUpdate()
	{
		if ( !Enabled ) return;

		if ( _player == null || !_player.IsValid )
		{
			_player = Scene.GetAllComponents<Player>().FirstOrDefault()?.GameObject;
			if ( _player != null )
			{
				Log.Info( $"✅ LootSpawner нашел игрока: {_player.Name}" );
			}
			else
			{
				return;
			}
		}

		// Очищаем собранные сундуки
		for ( int i = _activeChests.Count - 1; i >= 0; i-- )
		{
			if ( _activeChests[i] == null || !_activeChests[i].IsValid )
			{
				_activeChests.RemoveAt( i );
				if ( DebugMode ) Log.Info( $"🗑️ Сундук удален из списка" );
			}
		}

		// Спавним новые ТОЛЬКО если активен спавн
		if ( _isSpawningActive )
		{
			if ( !_hasSpawnedFirst && _activeChests.Count < MaxChests )
			{
				SpawnChest();
				_hasSpawnedFirst = true;
				_lastSpawn = 0;
			}

			if ( _activeChests.Count < MaxChests && _lastSpawn > SpawnInterval )
			{
				SpawnChest();
				_lastSpawn = 0;
			}
		}

		// Визуализация для отладки (только сферы, без текста)
		if ( DebugMode )
		{
			// Показываем позицию игрока
			DebugOverlay.Sphere( new Sphere( _player.Transform.Position, 50f ), Color.Green, 0f );

			// Показываем радиус спавна
			DebugOverlay.Sphere( new Sphere( _player.Transform.Position, SpawnRadius ), Color.Yellow.WithAlpha( 0.2f ), 0f );

			// Показываем активные сундуки
			foreach ( var chest in _activeChests )
			{
				if ( chest != null && chest.IsValid )
				{
					DebugOverlay.Sphere( new Sphere( chest.Transform.Position, 30f ), _isSpawningActive ? Color.Blue : Color.Gray, 0f );
				}
			}

			if ( Time.Now % 5f < 0.1f )
			{
				string status = _isSpawningActive ? "АКТИВЕН" : "НЕАКТИВЕН";
				Log.Info( $"📦 Активных сундуков: {_activeChests.Count}/{MaxChests}, Статус: {status}" );
			}
		}
	}

	public void StartSpawning()
	{
		_isSpawningActive = true;
		_hasSpawnedFirst = false;
		_lastSpawn = SpawnInterval;
		Log.Info( $"📦 Спавн сундуков ВКЛЮЧЕН" );
	}

	public void StopSpawning()
	{
		_isSpawningActive = false;
		Log.Info( $"📦 Спавн сундуков ВЫКЛЮЧЕН" );
	}

	private void SpawnChest()
	{
		if ( LootChestPrefab == null )
		{
			Log.Error( "❌ LootChestPrefab не назначен!" );
			return;
		}

		if ( _player == null ) return;

		Log.Info( $"🎲 Попытка создать сундук..." );

		int maxAttempts = 30;
		for ( int attempt = 0; attempt < maxAttempts; attempt++ )
		{
			var randomAngle = Random.Shared.Float( 0, 360 );
			var randomDir = Rotation.FromYaw( randomAngle ).Forward;
			var distance = Random.Shared.Float( MinSpawnDistance, SpawnRadius );

			var spawnPos = _player.Transform.Position + randomDir * distance;
			spawnPos += Vector3.Up * 20f;

			bool tooClose = false;
			foreach ( var chest in _activeChests )
			{
				if ( chest != null && chest.IsValid )
				{
					float distToOther = Vector3.DistanceBetween( spawnPos, chest.Transform.Position );
					if ( distToOther < 200f )
					{
						tooClose = true;
						break;
					}
				}
			}

			if ( !tooClose )
			{
				var chest = LootChestPrefab.Clone( spawnPos );
				chest.Name = $"LootChest_{_activeChests.Count + 1}";

				_activeChests.Add( chest );

				Log.Info( $"✅ Создан сундук {chest.Name} на {spawnPos}" );
				return;
			}
		}

		Log.Warning( "⚠️ Не удалось найти место для сундука после 30 попыток" );
	}
}
