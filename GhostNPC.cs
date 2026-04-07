using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed class GhostNPC : BaseNPC
{
	[Property] public GameObject PropPrefab { get; set; }
	[Property] public float PropSpeed { get; set; } = 500f;
	[Property] public float PropLifetime { get; set; } = 2f;
	[Property] public float PropScale { get; set; } = 1f;

	[Property] public float FloatAmplitude { get; set; } = 5f;
	[Property] public float FloatSpeed { get; set; } = 2f;

	protected override string GetNPCType() => "👻 Призрак";

	protected override void UpdateState()
	{
		if ( TargetPlayer == null ) return;

		Vector3 playerPos = TargetPlayer.Transform.Position;
		Vector3 npcPos = Transform.Position;

		float distanceToPlayer = Vector3.DistanceBetween( npcPos, playerPos );

		if ( distanceToPlayer <= AttackRange )
		{
			_state = NPCState.Attacking;

			Vector3 directionToPlayer = (playerPos - npcPos).Normal;
			Transform.Rotation = Rotation.Slerp( Transform.Rotation, Rotation.LookAt( directionToPlayer ), Time.Delta * 10f );

			if ( _lastAttack > AttackCooldown )
			{
				ThrowProp();
				_lastAttack = 0;
			}
		}
		else
		{
			_state = NPCState.Chasing;

			Vector3 directionToPlayer = (playerPos - npcPos).Normal;
			Transform.Position += directionToPlayer * MoveSpeed * Time.Delta;
			Transform.Rotation = Rotation.Slerp( Transform.Rotation, Rotation.LookAt( directionToPlayer ), Time.Delta * 10f );
		}

		// Парение (только для призрака)
		_time += Time.Delta;
		float floatOffset = MathF.Sin( _time * FloatSpeed ) * FloatAmplitude;
		Transform.Position += Vector3.Up * floatOffset * Time.Delta * 0.1f;
	}

	protected override void UpdateAnimation()
	{
		if ( ModelRenderer != null )
		{
			ModelRenderer.Set( "b_moving", _state == NPCState.Chasing );
			ModelRenderer.Set( "b_attack", _state == NPCState.Attacking );
		}
	}

	private void ThrowProp()
	{
		if ( PropPrefab == null )
		{
			Log.Warning( "⚠️ PropPrefab не назначен!" );
			return;
		}

		if ( TargetPlayer == null ) return;

		Vector3 startPos = Transform.Position + Vector3.Up * 50f + Transform.Rotation.Forward * 30f;
		Vector3 direction = (TargetPlayer.Transform.Position - startPos).Normal;

		var prop = PropPrefab.Clone( startPos );
		prop.Transform.Rotation = Rotation.LookAt( direction );
		prop.Transform.Scale = PropScale;

		var projectile = prop.Components.Create<Projectile>();
		projectile.Direction = direction;
		projectile.Speed = PropSpeed;
		projectile.Damage = AttackDamage;
		projectile.Owner = GameObject;
		projectile.Lifetime = PropLifetime;

		Log.Info( $"🎯 Призрак кинул проп! Урон: {AttackDamage}" );
	}
}
