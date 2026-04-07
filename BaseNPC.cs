using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public abstract class BaseNPC : Component
{
	[Property] public GameObject TargetPlayer { get; set; }

	[Property] public float Health { get; set; } = 100f;
	[Property] public float MaxHealth { get; set; } = 100f;
	[Property] public float AttackRange { get; set; } = 200f;
	[Property] public float AttackCooldown { get; set; } = 2f;
	[Property] public int AttackDamage { get; set; } = 15;
	[Property] public float MoveSpeed { get; set; } = 250f;
	[Property] public bool DebugMode { get; set; } = true;

	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }

	[Property] public GameObject DeathEffectPrefab { get; set; }
	[Property] public SoundEvent HitSound { get; set; }
	[Property] public SoundEvent DeathSound { get; set; }

	[Property] public bool EnableOutline { get; set; } = true;
	[Property] public Color OutlineHighHP { get; set; } = Color.Green;
	[Property] public Color OutlineMediumHP { get; set; } = Color.Yellow;
	[Property] public Color OutlineLowHP { get; set; } = Color.Red;
	[Property] public float OutlineWidth { get; set; } = 2.0f;

	[Property] public float FadeOutTime { get; set; } = 2f;

	protected TimeSince _lastAttack;
	protected NPCState _state = NPCState.Idle;
	protected float _time;
	protected bool _isDying = false;
	protected HighlightOutline _outline;

	public enum NPCState
	{
		Idle,
		Chasing,
		Attacking
	}

	protected override void OnStart()
	{
		if ( ModelRenderer == null )
		{
			ModelRenderer = Components.Get<SkinnedModelRenderer>();
		}

		_outline = Components.Get<HighlightOutline>();

		Health = MaxHealth;
		FindPlayer();

		if ( _outline != null && EnableOutline )
		{
			_outline.Width = OutlineWidth;
			UpdateOutlineColor();
		}

		Log.Info( $"{GetNPCType()} загружен" );
	}

	protected override void OnUpdate()
	{
		if ( _isDying ) return;

		if ( Health <= 0 )
		{
			Die();
			return;
		}

		if ( _outline != null && EnableOutline )
		{
			UpdateOutlineColor();
		}

		if ( TargetPlayer == null || !TargetPlayer.IsValid )
		{
			FindPlayer();
			return;
		}

		UpdateState();
		UpdateAnimation();

		if ( DebugMode && Time.Now % 0.5f < 0.1f )
		{
			float dist = Vector3.DistanceBetween( Transform.Position, TargetPlayer.Transform.Position );
			Log.Info( $"{GetNPCType()}: dist={dist:F0} state={_state} HP={Health}/{MaxHealth}" );
		}
	}

	protected abstract void UpdateState();
	protected abstract void UpdateAnimation();
	protected abstract string GetNPCType();

	protected void UpdateOutlineColor()
	{
		if ( _outline == null ) return;

		float healthPercent = Health / MaxHealth;

		if ( healthPercent > 0.75f )
			_outline.Color = OutlineHighHP;
		else if ( healthPercent > 0.25f )
			_outline.Color = OutlineMediumHP;
		else
			_outline.Color = OutlineLowHP;
	}

	protected virtual void Attack()
	{
		// Будет переопределено в наследниках
	}

	public void TakeDamage( float damage )
	{
		if ( _isDying ) return;

		Health -= damage;
		Log.Info( $"💥 {GetNPCType()} HP: {Health}/{MaxHealth}" );

		if ( HitSound != null )
		{
			Sound.Play( HitSound, Transform.Position );
		}

		_time += 5;

		if ( Health <= 0 ) Die();
	}

	protected void Die()
	{
		if ( _isDying ) return;
		_isDying = true;

		Log.Info( $"💀 {GetNPCType()} умер" );

		var playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();

		if ( DeathSound != null )
		{
			Sound.Play( DeathSound, Transform.Position );
		}

		if ( DeathEffectPrefab != null )
		{
			var effect = DeathEffectPrefab.Clone( Transform.Position );
			effect.Name = "DeathEffect";
		}

		if ( _outline != null )
		{
			_outline.Enabled = false;
		}

		var playerLevel = Scene.GetAllComponents<PlayerLevel>().FirstOrDefault();
		if ( playerLevel != null )
		{
			int expAmount = Random.Shared.Int( 1, 5 );
			playerLevel.AddExp( expAmount );

			if ( playerStats != null )
			{
				playerStats.AddKill();
				playerStats.AddExp( expAmount );
			}
		}
		else
		{
			if ( playerStats != null )
			{
				playerStats.AddKill();
			}
		}

		var collider = Components.Get<Collider>();
		if ( collider != null ) collider.Enabled = false;

		Enabled = false;
		_ = FadeOutAndDestroy( FadeOutTime );
	}

	protected async Task FadeOutAndDestroy( float delay )
	{
		float time = 0;

		while ( time < delay )
		{
			time += Time.Delta;
			float t = time / delay;
			float alpha = 1f - t;

			if ( ModelRenderer != null )
			{
				ModelRenderer.Tint = Color.White.WithAlpha( alpha );
			}

			await Task.Delay( 1 );
		}

		GameObject.Destroy();
	}

	protected void FindPlayer()
	{
		var player = Scene.GetAllComponents<Player>().FirstOrDefault();
		if ( player != null )
		{
			TargetPlayer = player.GameObject;
			Log.Info( $"{GetNPCType()} нашел игрока: {TargetPlayer.Name}" );
		}
	}
}
