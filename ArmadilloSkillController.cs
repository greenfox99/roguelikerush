using Sandbox;
using System;
using System.Linq;

public sealed class ArmadilloSkillController : Component
{
	[Property] public float DamageReductionFraction { get; set; } = 0.80f;
	[Property] public float ReflectIncomingDamageMultiplier { get; set; } = 1.0f;

	[Property] public SoundEvent ActivateSound { get; set; }
	[Property] public SoundEvent EndSound { get; set; }

	[Property, Group( "Outline" )] public bool UseOutline { get; set; } = true;
	[Property, Group( "Outline" )] public Color OutlineColor { get; set; } = new Color( 0.35f, 0.75f, 1f, 1f );
	[Property, Group( "Outline" )] public float OutlineWidth { get; set; } = 0.1f;

	public bool IsActive => _isActive;
	public float TimeLeft => _timeLeft;

	private bool _isActive;
	private float _timeLeft;
	private HighlightOutline _outline;

	protected override void OnStart()
	{
		_outline = FindOutline();
		UpdateOutlineState();
	}

	protected override void OnUpdate()
	{
		if ( !_isActive )
			return;

		_timeLeft -= Time.Delta;

		if ( _timeLeft <= 0f )
			Deactivate();
	}

	protected override void OnDisabled()
	{
		_isActive = false;
		_timeLeft = 0f;
		UpdateOutlineState();
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		_isActive = false;
		_timeLeft = 0f;
		UpdateOutlineState();
		base.OnDestroy();
	}

	public void Activate( float duration, float damageReductionFraction, float reflectIncomingDamageMultiplier )
	{
		DamageReductionFraction = Clamp01( damageReductionFraction );
		ReflectIncomingDamageMultiplier = MathF.Max( 0f, reflectIncomingDamageMultiplier );

		float finalDuration = MathF.Max( 0.05f, duration );
		bool wasInactive = !_isActive;

		_isActive = true;
		_timeLeft = MathF.Max( _timeLeft, finalDuration );

		UpdateOutlineState();

		if ( wasInactive && ActivateSound != null )
			Sound.Play( ActivateSound, Transform.Position );

		Log.Info( $"🛡️ Броненосец активирован на {finalDuration:0.0}s" );
	}

	public float ModifyIncomingDamage( float incomingDamage, GameObject attacker )
	{
		if ( !_isActive || incomingDamage <= 0f )
			return incomingDamage;

		float reflectDamage = incomingDamage * ReflectIncomingDamageMultiplier;
		ReflectDamageToAttacker( attacker, reflectDamage );

		float reducedDamage = incomingDamage * (1f - Clamp01( DamageReductionFraction ));
		return MathF.Max( 0f, reducedDamage );
	}

	private void Deactivate()
	{
		if ( !_isActive )
			return;

		_isActive = false;
		_timeLeft = 0f;

		UpdateOutlineState();

		if ( EndSound != null )
			Sound.Play( EndSound, Transform.Position );

		Log.Info( "🛡️ Броненосец закончился" );
	}

	private void UpdateOutlineState()
	{
		if ( !UseOutline )
			return;

		_outline ??= FindOutline();

		if ( _outline == null || !_outline.IsValid )
			return;

		_outline.Color = OutlineColor;
		_outline.Width = OutlineWidth;
		_outline.Enabled = _isActive;
	}

	private HighlightOutline FindOutline()
	{
		return Components.Get<HighlightOutline>()
			?? Components.GetInParent<HighlightOutline>()
			?? Components.GetInChildren<HighlightOutline>( true )
			?? Scene.GetAllComponents<HighlightOutline>().FirstOrDefault();
	}

	private void ReflectDamageToAttacker( GameObject attacker, float damage )
	{
		if ( damage <= 0f || attacker == null || !attacker.IsValid )
			return;

		var damageable = FindDamageableUpHierarchy( attacker );
		if ( damageable == null )
			return;

		damageable.TakeDamage( damage );

		Log.Info( $"🛡️ Броненосец отразил {damage:0.0} урона в {attacker.Name}" );
	}

	private IMyDamageable FindDamageableUpHierarchy( GameObject go )
	{
		var cur = go;

		while ( cur != null )
		{
			var d = cur.Components.Get<IMyDamageable>();
			if ( d != null )
				return d;

			cur = cur.Parent;
		}

		return null;
	}

	private float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}
}
