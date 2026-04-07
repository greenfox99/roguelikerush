using Sandbox;
using System;
using System.Linq;

public sealed class MagnetAbilityController : Component
{
	public static MagnetAbilityController Instance { get; private set; }

	[Property] public float CooldownSeconds { get; set; } = 145f;
	[Property] public float ActiveSeconds { get; set; } = 20f;
	[Property] public float GlobalMagnetDistance { get; set; } = 50000f;
	[Property] public float GlobalCollectDistance { get; set; } = 220f;
	[Property] public float ActiveMagnetSpeedMultiplier { get; set; } = 3f;

	[Property] public bool Unlocked { get; private set; } = false;
	[Property] public bool IsActive { get; private set; } = false;
	[Property] public float CooldownRemaining { get; private set; } = 0f;
	[Property] public float ActiveRemaining { get; private set; } = 0f;

	protected override void OnStart()
	{
		Instance = this;
	}

	protected override void OnDisabled()
	{
		if ( Instance == this )
			Instance = null;
	}

	public static MagnetAbilityController FindIn( Scene scene )
	{
		if ( Instance != null && Instance.IsValid )
			return Instance;

		if ( scene == null )
			return null;

		Instance = scene.GetAllComponents<MagnetAbilityController>().FirstOrDefault();
		return Instance;
	}

	public static bool IsUnlockedIn( Scene scene )
	{
		var ctrl = FindIn( scene );
		return ctrl != null && ctrl.Unlocked;
	}

	public static MagnetAbilityController GetOrCreate( Component requester )
	{
		if ( requester == null )
			return null;

		var existing = FindIn( requester.Scene );
		if ( existing != null && existing.IsValid )
			return existing;

		var host = requester.GameObject;
		if ( host == null || !host.IsValid )
			return null;

		Instance = host.Components.Create<MagnetAbilityController>();
		return Instance;
	}

	public void Unlock()
	{
		if ( Unlocked )
			return;

		Unlocked = true;
		ActivateNow();
	}

	public void RemoveUnlock()
	{
		Unlocked = false;
		IsActive = false;
		ActiveRemaining = 0f;
		CooldownRemaining = 0f;
	}

	public void ActivateNow()
	{
		if ( !Unlocked )
			return;

		IsActive = true;
		ActiveRemaining = MathF.Max( 0.01f, ActiveSeconds );
		CooldownRemaining = 0f;
	}

	public float GetProgress01()
	{
		if ( !Unlocked )
			return 0f;

		if ( IsActive )
			return Clamp01( ActiveRemaining / MathF.Max( 0.01f, ActiveSeconds ) );

		return 1f - Clamp01( CooldownRemaining / MathF.Max( 0.01f, CooldownSeconds ) );
	}

	public float GetDisplaySeconds()
	{
		if ( !Unlocked )
			return 0f;

		return IsActive ? MathF.Max( 0f, ActiveRemaining ) : MathF.Max( 0f, CooldownRemaining );
	}

	protected override void OnUpdate()
	{
		if ( !Unlocked )
			return;

		if ( IsActive )
		{
			ActiveRemaining -= Time.Delta;
			if ( ActiveRemaining <= 0f )
			{
				ActiveRemaining = 0f;
				IsActive = false;
				CooldownRemaining = MathF.Max( 0.01f, CooldownSeconds );
			}
			return;
		}

		if ( CooldownRemaining > 0f )
		{
			CooldownRemaining -= Time.Delta;
			if ( CooldownRemaining <= 0f )
			{
				CooldownRemaining = 0f;
				ActivateNow();
			}
			return;
		}

		ActivateNow();
	}

	private static float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}
}
