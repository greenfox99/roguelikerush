using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class StatueManager : Component
{
	[Property] public GameObject StatuePrefab { get; set; }
	[Property] public List<GameObject> SpawnPoints { get; set; } = new();

	[Property, Group( "Spawn" )] public int InitialStatueCount { get; set; } = 4;
	[Property, Group( "Spawn" )] public float SpawnInterval { get; set; } = 60f;
	[Property, Group( "Spawn" )] public int MaxAliveStatues { get; set; } = 6;

	[Property, Group( "Lifetime" )] public float UncapturedLifetime { get; set; } = 180f;
	[Property, Group( "Lifetime" )] public float CapturedLifetime { get; set; } = 20f;

	[Property, Group( "Debug" )] public bool DebugMode { get; set; } = true;

	private readonly List<TrackedStatue> _statues = new();
	private readonly Random _rng = new();

	private float _spawnTimer = 0f;
	private GameManager _gameManager;
	private bool _wasFightActive = false;

	private class TrackedStatue
	{
		public GameObject Obj;
		public StatueCapturePoint Capture;
		public float Lifetime;
		public bool WasCaptured;
	}

	protected override void OnStart()
	{
		_gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		_spawnTimer = SpawnInterval;

		// На старте игры статуй не создаём.
		// Они появятся только когда начнётся бой.
		ClearAllStatuesImmediate();

		if ( DebugMode )
			Log.Info( "🗿 StatueManager started. Waiting for fight..." );
	}

	protected override void OnUpdate()
	{
		bool fightActive = IsFightActive();

		// Вход в бой
		if ( fightActive && !_wasFightActive )
		{
			OnFightStarted();
		}

		// Выход из боя
		if ( !fightActive && _wasFightActive )
		{
			OnFightEnded();
		}

		_wasFightActive = fightActive;

		// Пока бой не идёт — ничего не спавним и не апдейтим
		if ( !fightActive )
			return;

		float dt = Time.Delta;

		UpdateStatues( dt );

		_spawnTimer -= dt;
		if ( _spawnTimer <= 0f )
		{
			_spawnTimer = SpawnInterval;
			SpawnOneStatue();
		}
	}

	private bool IsFightActive()
	{
		if ( _gameManager == null || !_gameManager.IsValid )
			_gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		if ( _gameManager == null )
			return false;

		return _gameManager.CurrentState == GameManager.GameState.Fighting;
	}

	private void OnFightStarted()
	{
		// Сбрасываем экономику статуй при каждом новом бою
		StatueRewardSystem.ResetProgress();

		// На всякий случай чистим старое
		ClearAllStatuesImmediate();

		_spawnTimer = SpawnInterval;

		for ( int i = 0; i < InitialStatueCount; i++ )
			SpawnOneStatue();

		if ( DebugMode )
			Log.Info( $"🗿 Бой начался. Статуи активированы: {InitialStatueCount}" );
	}

	private void OnFightEnded()
	{
		ClearAllStatuesImmediate();

		if ( DebugMode )
			Log.Info( "🗿 Бой закончился. Все статуи удалены." );
	}

	private void ClearAllStatuesImmediate()
	{
		for ( int i = _statues.Count - 1; i >= 0; i-- )
		{
			var s = _statues[i];
			if ( s?.Obj != null && s.Obj.IsValid )
				s.Obj.Destroy();
		}

		_statues.Clear();
	}

	private void UpdateStatues( float dt )
	{
		for ( int i = _statues.Count - 1; i >= 0; i-- )
		{
			var s = _statues[i];

			if ( s == null || s.Obj == null || !s.Obj.IsValid || s.Capture == null || !s.Capture.IsValid )
			{
				_statues.RemoveAt( i );
				continue;
			}

			if ( s.Capture.IsCaptured && !s.WasCaptured )
			{
				s.WasCaptured = true;
				s.Lifetime = CapturedLifetime;

				if ( DebugMode )
					Log.Info( $"🟢 Статуя захвачена и будет удалена через {CapturedLifetime:0.0} сек: {s.Obj.Name}" );
			}

			s.Lifetime -= dt;

			if ( s.Lifetime <= 0f )
			{
				if ( DebugMode )
					Log.Info( $"🗑️ Удаляем статую: {s.Obj.Name}" );

				s.Obj.Destroy();
				_statues.RemoveAt( i );
			}
		}

		// Если живых статуй слишком много — удаляем самую старую незахваченную
		while ( _statues.Count > MaxAliveStatues )
		{
			int idx = FindOldestUncapturedStatueIndex();
			if ( idx < 0 )
				idx = 0;

			var s = _statues[idx];
			if ( s?.Obj != null && s.Obj.IsValid )
			{
				if ( DebugMode )
					Log.Info( $"🗑️ Лимит статуй превышен, удаляем: {s.Obj.Name}" );

				s.Obj.Destroy();
			}

			_statues.RemoveAt( idx );
		}
	}

	private int FindOldestUncapturedStatueIndex()
	{
		int best = -1;
		float lowestLifetime = float.MaxValue;

		for ( int i = 0; i < _statues.Count; i++ )
		{
			var s = _statues[i];
			if ( s == null || s.Capture == null ) continue;
			if ( s.Capture.IsCaptured ) continue;

			if ( s.Lifetime < lowestLifetime )
			{
				lowestLifetime = s.Lifetime;
				best = i;
			}
		}

		return best;
	}

	private void SpawnOneStatue()
	{
		if ( StatuePrefab == null || !StatuePrefab.IsValid )
		{
			if ( DebugMode ) Log.Warning( "❌ StatueManager: StatuePrefab не назначен" );
			return;
		}

		if ( SpawnPoints == null || SpawnPoints.Count == 0 )
		{
			if ( DebugMode ) Log.Warning( "❌ StatueManager: SpawnPoints пустой" );
			return;
		}

		var point = PickFreeSpawnPoint();
		if ( point == null )
		{
			if ( DebugMode ) Log.Warning( "⚠️ StatueManager: не нашли свободную точку спавна" );
			return;
		}

		var obj = StatuePrefab.Clone( point.Transform.Position, point.Transform.Rotation );
		obj.Name = $"Statue_{Time.Now:0.00}";

		var capture = obj.Components.Get<StatueCapturePoint>( FindMode.EnabledInSelfAndDescendants );
		if ( capture == null )
		{
			if ( DebugMode ) Log.Warning( $"⚠️ Statue prefab '{obj.Name}' не содержит StatueCapturePoint" );
		}

		_statues.Add( new TrackedStatue
		{
			Obj = obj,
			Capture = capture,
			Lifetime = UncapturedLifetime,
			WasCaptured = false
		} );

		if ( DebugMode )
			Log.Info( $"🗿 Спавн статуи: {obj.Name} at {point.Transform.Position}" );
	}

	private GameObject PickFreeSpawnPoint()
	{
		var candidates = new List<GameObject>();

		foreach ( var p in SpawnPoints )
		{
			if ( p == null || !p.IsValid ) continue;

			bool occupied = false;

			foreach ( var s in _statues )
			{
				if ( s == null || s.Obj == null || !s.Obj.IsValid ) continue;

				float dist = Vector3.DistanceBetween( p.Transform.Position, s.Obj.Transform.Position );
				if ( dist < 100f )
				{
					occupied = true;
					break;
				}
			}

			if ( !occupied )
				candidates.Add( p );
		}

		if ( candidates.Count == 0 )
			return null;

		int index = _rng.Next( 0, candidates.Count );
		return candidates[index];
	}
}
