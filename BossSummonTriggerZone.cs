using Sandbox;
using System.Linq;

/// <summary>
/// Trigger zone для ручного призыва босса.
/// Когда игрок внутри зоны и жмёт E/use -> вызывает ForceSpawnBossNow() у спавнера.
/// Плюс даёт HUD информацию, когда надо показывать prompt.
/// </summary>
public sealed class BossSummonTriggerZone : Component, Component.ITriggerListener
{
	[Property] public GameObject SpawnerObject { get; set; }

	[Property, Group( "Input" )] public string UseActionName { get; set; } = "use";

	[Property, Group( "HUD" )] public string PromptText { get; set; } = "ДЛЯ ВЫЗОВА БОССА";

	[Property, Group( "Behavior" )] public bool SingleUse { get; set; } = true;
	[Property, Group( "Behavior" )] public bool DisableAfterUse { get; set; } = true;

	[Property, Group( "Debug" )] public bool DebugMode { get; set; } = true;

	private DualNPCSpawner _spawner;
	private bool _playerInside = false;
	private bool _used = false;

	public bool IsPlayerInside => _playerInside;

	public bool IsPromptVisible
	{
		get
		{
			ResolveSpawner();

			if ( _used && SingleUse )
				return false;

			if ( !_playerInside )
				return false;

			if ( _spawner == null || !_spawner.IsValid )
				return false;

			if ( _spawner.BossIsAlive )
				return false;

			return true;
		}
	}

	protected override void OnStart()
	{
		ResolveSpawner();
	}

	protected override void OnUpdate()
	{
		if ( _used && SingleUse )
			return;

		if ( !_playerInside )
			return;

		ResolveSpawner();
		if ( _spawner == null )
			return;

		if ( Input.Pressed( UseActionName ) )
		{
			bool spawned = _spawner.ForceSpawnBossNow();

			if ( spawned )
			{
				_used = true;

				if ( DebugMode )
					Log.Info( "👹 BossSummonTriggerZone: босс успешно призван." );

				if ( DisableAfterUse )
					Enabled = false;
			}
			else
			{
				if ( DebugMode )
					Log.Info( "ℹ️ BossSummonTriggerZone: спавн босса не выполнен." );
			}
		}
	}

	void Component.ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( IsPlayerCollider( other ) )
		{
			_playerInside = true;

			if ( DebugMode )
				Log.Info( "🟢 Игрок вошёл в boss trigger zone" );
		}
	}

	void Component.ITriggerListener.OnTriggerExit( Collider other )
	{
		if ( IsPlayerCollider( other ) )
		{
			_playerInside = false;

			if ( DebugMode )
				Log.Info( "🔴 Игрок вышел из boss trigger zone" );
		}
	}

	private void ResolveSpawner()
	{
		if ( _spawner != null && _spawner.IsValid )
			return;

		if ( SpawnerObject != null && SpawnerObject.IsValid )
		{
			_spawner = SpawnerObject.Components.Get<DualNPCSpawner>();
			if ( _spawner != null )
				return;
		}

		_spawner = Scene.GetAllComponents<DualNPCSpawner>().FirstOrDefault();
	}

	private bool IsPlayerCollider( Collider other )
	{
		if ( other == null || !other.IsValid )
			return false;

		var go = other.GameObject;
		if ( go == null || !go.IsValid )
			return false;

		if ( go.Components.Get<Player>( FindMode.EnabledInSelfAndDescendants ) != null )
			return true;

		var root = go.Root;
		if ( root != null && root.IsValid )
		{
			if ( root.Components.Get<Player>( FindMode.EnabledInSelfAndDescendants ) != null )
				return true;
		}

		return false;
	}
}
