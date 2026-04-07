using Sandbox;
using System;
using System.Linq;

public sealed class Projectile : Component, Component.ITriggerListener
{
	[Property] public Vector3 Direction { get; set; }
	[Property] public float Speed { get; set; } = 500f;
	[Property] public int Damage { get; set; } = 15;
	[Property] public GameObject Owner { get; set; }
	[Property] public float Lifetime { get; set; } = 2f;
	[Property] public float HitRadius { get; set; } = 50f;

	private float _lifeTimer = 0f;
	private bool _consumed = false;

	private bool IsPlayerOwned()
	{
		if ( Owner == null || !Owner.IsValid )
			return false;

		if ( Owner.Components.Get<Player>() != null )
			return true;

		if ( Owner.Components.GetInParent<Player>() != null )
			return true;

		return false;
	}

	private float GetProjectileDelta()
	{
		if ( IsPlayerOwned() )
			return Time.Delta;

		return Time.Delta * TimeSlowSystem.WorldScale;
	}

	protected override void OnStart()
	{
		_lifeTimer = 0f;
		_consumed = false;

		if ( Direction.Length > 0.001f )
			Direction = Direction.Normal;
	}

	protected override void OnUpdate()
	{
		if ( _consumed )
			return;

		float dt = GetProjectileDelta();
		_lifeTimer += dt;

		Transform.Position += Direction * Speed * dt;
		Transform.Rotation *= Rotation.FromYaw( 10f * dt );

		// Fallback на случай, если trigger иногда пропускает быстрый projectile.
		CheckHitFallback();

		if ( _lifeTimer >= Lifetime )
			DestroySelf();
	}

	private void CheckHitFallback()
	{
		if ( IsPlayerOwned() )
			return;

		var player = Scene.GetAllComponents<Player>().FirstOrDefault();
		if ( player == null || !player.IsValid )
			return;

		var playerHealth = FindPlayerHealth( player.GameObject );
		if ( playerHealth == null )
			return;

		float dist = Vector3.DistanceBetween( Transform.Position, playerHealth.Transform.Position );
		if ( dist > HitRadius )
			return;

		ApplyDamageToPlayer( playerHealth, "radius fallback" );
	}

	private PlayerHealth FindPlayerHealth( GameObject go )
	{
		if ( go == null || !go.IsValid )
			return null;

		return go.Components.Get<PlayerHealth>()
			?? go.Components.GetInParent<PlayerHealth>()
			?? go.Components.GetInChildren<PlayerHealth>( true );
	}

	private bool HasNpcTag( GameObject go )
	{
		if ( go == null || !go.IsValid )
			return false;

		return go.Tags.Has( "npc" );
	}

	private bool BelongsToOwner( GameObject other )
	{
		if ( other == null || !other.IsValid || Owner == null || !Owner.IsValid )
			return false;

		if ( other == Owner )
			return true;

		var ownerPlayer = Owner.Components.Get<Player>() ?? Owner.Components.GetInParent<Player>();
		var otherPlayer = other.Components.Get<Player>() ?? other.Components.GetInParent<Player>();
		if ( ownerPlayer != null && otherPlayer != null && ownerPlayer == otherPlayer )
			return true;

		if ( Owner.Root == other.Root )
			return true;

		return false;
	}

	private void ApplyDamageToPlayer( PlayerHealth playerHealth, string source )
	{
		if ( _consumed || playerHealth == null || !playerHealth.IsValid )
			return;

		_consumed = true;

		GameObject attacker = (Owner != null && Owner.IsValid) ? Owner : GameObject;

		Log.Info( $"💥 Projectile hit player via {source}. Damage: {Damage}" );
		playerHealth.TakeDamage( Damage, attacker );

		DestroySelf();
	}

	private void DestroySelf()
	{
		if ( !GameObject.IsValid )
			return;

		GameObject.Destroy();
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( _consumed )
			return;

		if ( other == null || !other.IsValid )
			return;

		GameObject otherGo = other.GameObject;
		if ( otherGo == null || !otherGo.IsValid )
			return;

		if ( BelongsToOwner( otherGo ) )
			return;

		var playerHealth = FindPlayerHealth( otherGo );
		if ( !IsPlayerOwned() && playerHealth != null )
		{
			ApplyDamageToPlayer( playerHealth, $"trigger:{otherGo.Name}" );
			return;
		}

		// Любой объект с тегом npc игнорируем полностью: projectile летит сквозь него.
		// Теги у GameObject наследуются от родителя, так что достаточно поставить тег npc
		// на корневой объект NPC в инспекторе.
		if ( HasNpcTag( otherGo ) )
			return;

		// Всё остальное считаем миром/препятствием и уничтожаем projectile.
		DestroySelf();
	}
}
