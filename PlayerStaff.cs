using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public sealed class PlayerStaff : Component
{
	[Property] public GameObject StaffModel { get; set; }
	[Property] public ParticleEffect StaffEffect { get; set; }

	[Property] public float AttackRange { get; set; } = 500f;
	[Property] public float AttackCooldown { get; set; } = 2f;
	[Property] public int Damage { get; set; } = 20;

	[Property] public bool DebugMode { get; set; } = true;

	[Property] public int MaxTargets { get; set; } = 1;
	[Property] public float RangeMultiplier { get; set; } = 1f;
	[Property] public bool EnableStaffEffect { get; set; } = true;

	// ===== Ricochet / Chain lightning =====
	[Property] public int RicochetBounces { get; set; } = 0;
	[Property] public float RicochetRangeMultiplier { get; set; } = 0.7f;
	[Property] public float RicochetDamageMultiplier { get; set; } = 0.85f;

	// ===== Crit =====
	[Property] public float CritChance { get; set; } = 0f;
	[Property] public float CritMultiplier { get; set; } = 2f;

	// ===== Execute =====
	[Property] public bool EnableExecute { get; set; } = false;
	[Property] public float ExecuteHpThreshold { get; set; } = 0.30f;
	[Property] public float ExecuteBonusMultiplier { get; set; } = 1.15f;

	// ===== Kill CDR stacks =====
	[Property] public bool EnableKillCooldownStacks { get; set; } = false;
	[Property] public float KillCooldownReductionPerKill { get; set; } = 0.001f;
	[Property] public float KillCooldownReductionMax { get; set; } = 0.30f;
	[Property] public float KillCooldownReductionCurrent { get; set; } = 0f;

	// ===== Special upgrades =====
	[Property, Group( "Special Upgrades" )] public float BerserkHealthStep { get; set; } = 5f;
	[Property, Group( "Special Upgrades" )] public int BerserkDamagePerStack { get; set; } = 2;
	[Property, Group( "Special Upgrades" )] public int BerserkDamageMaxBonus { get; set; } = 20;
	[Property, Group( "Special Upgrades" )] public float BerserkAttackSpeedPerStack { get; set; } = 0.05f;
	[Property, Group( "Special Upgrades" )] public float BerserkAttackSpeedMaxBonus { get; set; } = 0.50f;
	[Property, Group( "Special Upgrades" )] public float BerserkDuration { get; set; } = 10f;

	[Property, Group( "Special Upgrades" )] public float RadiationChancePerLevel { get; set; } = 0.05f;
	[Property, Group( "Special Upgrades" )] public float RadiationChanceMax { get; set; } = 0.50f;
	[Property, Group( "Special Upgrades" )] public int RadiationTickDamage { get; set; } = 1;
	[Property, Group( "Special Upgrades" )] public float RadiationTickInterval { get; set; } = 0.5f;
	[Property, Group( "Special Upgrades" )] public float RadiationDuration { get; set; } = 5f;
	[Property, Group( "Special Upgrades" )] public int RadiationSicknessMaxLevel { get; set; } = 10;

	[Property, Group( "Special Upgrades" )] public int GoldFeverMaxLevel { get; set; } = 10;

	public int BerserkAbilityLevel { get; private set; } = 0;
	public int RadiationSicknessLevel { get; private set; } = 0;
	public int GoldFeverLevel { get; private set; } = 0;

	public int CurrentBerserkStacks => _berserkStacks;
	public int CurrentBerserkDamageBonus => GetCurrentBerserkDamageBonus();
	public float CurrentBerserkAttackSpeedBonusPercent => GetCurrentBerserkAttackSpeedBonus() * 100f;
	public float CurrentRadiationChancePercent => CurrentRadiationChance * 100f;
	public float CurrentRadiationChance => Clamp( RadiationSicknessLevel * RadiationChancePerLevel, 0f, RadiationChanceMax );
	public int CurrentGoldPerKillBonus => Math.Max( 0, Math.Min( GoldFeverLevel, GoldFeverMaxLevel ) );

	// =========================
	// LINE RENDERER FX
	// =========================
	[Property, Group( "Line FX" )] public bool UseLineRendererFx { get; set; } = true;

	[Property, Group( "Line FX" )] public Material LineMaterial { get; set; }
	[Property, Group( "Line FX" )] public Color LineTint { get; set; } = Color.Cyan;
	[Property, Group( "Line FX" )] public float LineLifetime { get; set; } = 0.18f;
	[Property, Group( "Line FX" )] public float LineWidth { get; set; } = 10f;

	[Property, Group( "Line FX" )] public int LineSegments { get; set; } = 12;
	[Property, Group( "Line FX" )] public float LineJitter { get; set; } = 18f;
	[Property, Group( "Line FX" )] public float LineRefreshRate { get; set; } = 0.03f;

	[Property, Group( "Line FX" )] public bool LineUseCylinder { get; set; } = true;
	[Property, Group( "Line FX" )] public int LineCylinderSegments { get; set; } = 8;

	[Property, Group( "Line FX" )] public bool LineWorldSpaceUv { get; set; } = false;
	[Property, Group( "Line FX" )] public float LineUnitsPerTexture { get; set; } = 256f;
	[Property, Group( "Line FX" )] public float LineTextureScale { get; set; } = 1f;
	[Property, Group( "Line FX" )] public float LineTextureOffset { get; set; } = 0f;
	[Property, Group( "Line FX" )] public float LineTextureScrollSpeed { get; set; } = 35f;

	[Property, Group( "Line FX" )] public float LineDepthFeather { get; set; } = 128f;

	[Property, Group( "Line FX" )] public Vector3 BeamStartOffset { get; set; } = new Vector3( 0, 0, 30f );
	[Property, Group( "Line FX" )] public Vector3 BeamEndOffset { get; set; } = new Vector3( 0, 0, 30f );

	// =========================
	// ANIMATION
	// =========================
	[Property, Group( "Animation" )] public SkinnedModelRenderer PlayerRenderer { get; set; }
	[Property, Group( "Animation" )] public string StaffAttackSequence { get; set; } = "Holditem_RH_Throw_Normal";

	[Property, Group( "Animation" )] public float AttackAnimSpeed { get; set; } = 2.0f;
	[Property, Group( "Animation" )] public float AttackAnimMinInterval { get; set; } = 0.12f;
	[Property, Group( "Animation" )] public float DirectPlaybackCancelAfter { get; set; } = 0.35f;

	[Property, Group( "Animation" )] public string FallbackBoolParam { get; set; } = "b_attack";
	[Property, Group( "Animation" )] public float FallbackPulseTime { get; set; } = 0.05f;

	[Property, Group( "Animation" )] public bool DebugAnim { get; set; } = false;

	private TimeSince _lastAttackAnim;

	private AnimGraphDirectPlayback _dpToCancel;
	private float _dpCancelAt = 0f;

	private SceneModel _pulseModel;
	private float _pulseResetAt = 0f;
	private bool _pulseActive = false;

	private bool _tempRateActive = false;
	private float _tempRateResetAt = 0f;
	private float _savedPlaybackRate = 1f;

	public IReadOnlyList<Component> CurrentTargets => _currentTargets;
	public bool IsAttackLocked => RealTime.Now < _attackLockedUntilReal;
	public float AttackLockSecondsLeft => MathF.Max( 0f, _attackLockedUntilReal - RealTime.Now );
	private readonly List<Component> _currentTargets = new();

	private TimeSince _lastAttack;
	private float _attackLockedUntilReal = 0f;
	private bool _isAttacking = false;

	private Random _rng;

	private readonly Dictionary<Guid, float> _estimatedMaxHpById = new();
	private readonly Dictionary<Guid, RadiationDotState> _radiationDots = new();

	private PlayerHealth _boundPlayerHealth;
	private PlayerStats _playerStats;
	private int _berserkStacks = 0;
	private float _berserkTimer = 0f;

	private struct RadiationDotState
	{
		public Component Target;
		public float TimeLeft;
		public float TickTimer;
	}

	protected override void OnStart()
	{
		_rng = new Random( (int)(Time.Now * 1000f) ^ GameObject.Id.GetHashCode() );

		if ( StaffEffect != null && EnableStaffEffect )
			StaffEffect.Enabled = true;

		if ( PlayerRenderer == null )
		{
			PlayerRenderer =
				GameObject.Root?.Components.GetInChildren<SkinnedModelRenderer>( true )
				?? Components.GetInChildren<SkinnedModelRenderer>( true );
		}

		if ( DebugAnim )
			Log.Info( $"🎬 PlayerRenderer={(PlayerRenderer != null ? PlayerRenderer.GameObject.Name : "NULL")}, DirectPlayback={(PlayerRenderer?.SceneModel?.DirectPlayback != null)}" );

		EnsureUpgradeRefs();
	}

	protected override void OnDisabled()
	{
		UnbindPlayerHealth();
	}

	protected override void OnUpdate()
	{
		try
		{
			EnsureUpgradeRefs();
			UpdateAnimFixups();
			UpdateBerserkState();
			UpdateRadiationDots();

			if ( IsAttackLocked )
			{
				_currentTargets.Clear();
				_isAttacking = false;
				return;
			}

			if ( _isAttacking )
				return;

			var allEnemies = CollectAllEnemiesAlive();

			if ( allEnemies.Count == 0 )
			{
				_currentTargets.Clear();
				return;
			}

			var targets = FindNearestTargets( allEnemies, MaxTargets );

			_currentTargets.Clear();
			_currentTargets.AddRange( targets.Where( t => t != null && t.IsValid ) );

			float cd = GetEffectiveCooldown();

			if ( targets.Count > 0 && _lastAttack > cd )
			{
				StartLightningAttack( targets, allEnemies );
				_lastAttack = 0;
			}
		}
		catch ( Exception ex )
		{
			Log.Error( ex );
		}
	}

	public void LockAttacks( float durationSeconds )
	{
		if ( durationSeconds <= 0f )
			return;

		_attackLockedUntilReal = MathF.Max( _attackLockedUntilReal, RealTime.Now + durationSeconds );
		_isAttacking = false;
		_currentTargets.Clear();
	}

	public void UnlockAttacks()
	{
		_attackLockedUntilReal = 0f;
		_isAttacking = false;
		_currentTargets.Clear();
	}

	private void EnsureUpgradeRefs()
	{
		if ( (_boundPlayerHealth == null || !_boundPlayerHealth.IsValid) && Scene != null )
		{
			var playerHealth = Scene.GetAllComponents<PlayerHealth>().FirstOrDefault();
			if ( !ReferenceEquals( _boundPlayerHealth, playerHealth ) )
			{
				UnbindPlayerHealth();
				_boundPlayerHealth = playerHealth;
				if ( _boundPlayerHealth != null )
					_boundPlayerHealth.OnHealthDamageTaken += HandlePlayerHealthDamageTaken;
			}
		}

		if ( _playerStats == null || !_playerStats.IsValid )
			_playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
	}

	private void UnbindPlayerHealth()
	{
		if ( _boundPlayerHealth != null )
			_boundPlayerHealth.OnHealthDamageTaken -= HandlePlayerHealthDamageTaken;

		_boundPlayerHealth = null;
	}

	public void AddBerserkAbility()
	{
		BerserkAbilityLevel = 1;
	}

	public void AddRadiationSicknessLevel()
	{
		RadiationSicknessLevel = Math.Min( RadiationSicknessMaxLevel, RadiationSicknessLevel + 1 );
	}

	public void AddGoldFeverLevel()
	{
		GoldFeverLevel = Math.Min( GoldFeverMaxLevel, GoldFeverLevel + 1 );
	}
	public void RemoveBerserkAbility()
	{
		BerserkAbilityLevel = 0;
		_berserkStacks = 0;
		_berserkTimer = 0f;
	}

	public void RemoveRadiationSicknessLevel()
	{
		RadiationSicknessLevel = Math.Max( 0, RadiationSicknessLevel - 1 );
	}

	public void RemoveGoldFeverLevel()
	{
		GoldFeverLevel = Math.Max( 0, GoldFeverLevel - 1 );
	}

	private void HandlePlayerHealthDamageTaken( float hpDamage )
	{
		if ( BerserkAbilityLevel <= 0 || hpDamage <= 0f )
			return;

		if ( _berserkStacks > 0 )
			_berserkTimer = MathF.Max( 0.05f, BerserkDuration );

		float step = MathF.Max( 1f, BerserkHealthStep );
		int gainedStacks = Math.Max( 0, (int)MathF.Floor( hpDamage / step ) );
		if ( gainedStacks <= 0 )
			return;

		_berserkStacks = Math.Min( GetBerserkMaxStacks(), _berserkStacks + gainedStacks );
		_berserkTimer = MathF.Max( 0.05f, BerserkDuration );

		if ( DebugMode )
			Log.Info( $"😡 Берсерк: +{gainedStacks} ст. => {_berserkStacks}/{GetBerserkMaxStacks()}" );
	}

	private void UpdateBerserkState()
	{
		if ( _berserkStacks <= 0 )
			return;

		_berserkTimer -= Time.Delta;
		if ( _berserkTimer > 0f )
			return;

		if ( DebugMode )
			Log.Info( "😡 Берсерк спал" );

		_berserkStacks = 0;
		_berserkTimer = 0f;
	}

	private int GetBerserkMaxStacks()
	{
		int byDamage = BerserkDamagePerStack > 0 ? Math.Max( 0, BerserkDamageMaxBonus / BerserkDamagePerStack ) : 0;
		int bySpeed = BerserkAttackSpeedPerStack > 0f ? Math.Max( 0, (int)MathF.Floor( BerserkAttackSpeedMaxBonus / BerserkAttackSpeedPerStack ) ) : 0;

		if ( byDamage <= 0 ) return bySpeed;
		if ( bySpeed <= 0 ) return byDamage;
		return Math.Min( byDamage, bySpeed );
	}

	private int GetCurrentBerserkDamageBonus()
	{
		return ClampInt( _berserkStacks * Math.Max( 0, BerserkDamagePerStack ), 0, Math.Max( 0, BerserkDamageMaxBonus ) );
	}

	private float GetCurrentBerserkAttackSpeedBonus()
	{
		return Clamp( _berserkStacks * MathF.Max( 0f, BerserkAttackSpeedPerStack ), 0f, MathF.Max( 0f, BerserkAttackSpeedMaxBonus ) );
	}

	private int GetEffectiveAttackDamage()
	{
		return Math.Max( 1, Damage + GetCurrentBerserkDamageBonus() );
	}

	private void UpdateRadiationDots()
	{
		if ( _radiationDots.Count <= 0 )
			return;

		var ids = _radiationDots.Keys.ToList();
		float tickInterval = MathF.Max( 0.05f, RadiationTickInterval );
		int tickDamage = Math.Max( 1, RadiationTickDamage );

		for ( int i = 0; i < ids.Count; i++ )
		{
			Guid id = ids[i];
			if ( !_radiationDots.TryGetValue( id, out var dot ) )
				continue;

			if ( dot.Target == null || !dot.Target.IsValid || GetEnemyHealth( dot.Target ) <= 0f )
			{
				_radiationDots.Remove( id );
				continue;
			}

			dot.TimeLeft -= Time.Delta;
			dot.TickTimer -= Time.Delta;

			while ( dot.TickTimer <= 0f && dot.TimeLeft > 0f )
			{
				ApplyDamageToEnemyAndMaybeCountKill( dot.Target, tickDamage, false, false );

				if ( dot.Target == null || !dot.Target.IsValid || GetEnemyHealth( dot.Target ) <= 0f )
					break;

				dot.TickTimer += tickInterval;
			}

			if ( dot.TimeLeft <= 0f || dot.Target == null || !dot.Target.IsValid || GetEnemyHealth( dot.Target ) <= 0f )
			{
				_radiationDots.Remove( id );
				continue;
			}

			_radiationDots[id] = dot;
		}
	}

	private void TryApplyRadiationSickness( Component target )
	{
		if ( RadiationSicknessLevel <= 0 )
			return;

		if ( target == null || !target.IsValid || GetEnemyHealth( target ) <= 0f )
			return;

		float chance = CurrentRadiationChance;
		if ( chance <= 0f )
			return;

		if ( (float)_rng.NextDouble() > chance )
			return;

		_radiationDots[target.GameObject.Id] = new RadiationDotState
		{
			Target = target,
			TimeLeft = MathF.Max( 0.1f, RadiationDuration ),
			TickTimer = MathF.Max( 0.05f, RadiationTickInterval )
		};

		if ( DebugMode )
			Log.Info( $"☣️ Лучевая болезнь на {target.GameObject.Name}" );
	}

	private void UpdateAnimFixups()
	{
		float now = RealTime.Now;

		if ( _dpToCancel != null && now >= _dpCancelAt )
		{
			_dpToCancel.Cancel();
			if ( DebugAnim ) Log.Info( "🎬 DirectPlayback.Cancel()" );
			_dpToCancel = null;
		}

		if ( _pulseActive && _pulseModel != null && now >= _pulseResetAt )
		{
			_pulseModel.SetAnimParameter( FallbackBoolParam, false );
			_pulseActive = false;
			_pulseModel = null;

			if ( DebugAnim ) Log.Info( $"🎬 Pulse reset: {FallbackBoolParam}=false" );
		}

		if ( _tempRateActive && now >= _tempRateResetAt )
		{
			if ( PlayerRenderer != null && PlayerRenderer.IsValid )
				PlayerRenderer.PlaybackRate = _savedPlaybackRate;

			_tempRateActive = false;

			if ( DebugAnim ) Log.Info( $"🎬 PlaybackRate reset -> {_savedPlaybackRate}" );
		}
	}

	private void ApplyTempPlaybackRate( float speed, float holdSeconds )
	{
		if ( PlayerRenderer == null || !PlayerRenderer.IsValid )
			return;

		speed = MathF.Max( 0.01f, speed );
		holdSeconds = MathF.Max( 0.02f, holdSeconds );

		if ( !_tempRateActive )
		{
			_savedPlaybackRate = PlayerRenderer.PlaybackRate;
			_tempRateActive = true;
		}

		PlayerRenderer.PlaybackRate = speed;

		float until = RealTime.Now + holdSeconds;
		_tempRateResetAt = MathF.Max( _tempRateResetAt, until );
	}

	private float GetEffectiveCooldown()
	{
		float attackSpeedMul = 1f + GetCurrentBerserkAttackSpeedBonus();
		float baseCd = MathF.Max( 0.05f, AttackCooldown ) / MathF.Max( 0.01f, attackSpeedMul );

		if ( !EnableKillCooldownStacks )
			return baseCd;

		float cap = Clamp( KillCooldownReductionMax, 0f, 0.95f );
		float cdr = Clamp( KillCooldownReductionCurrent, 0f, cap );

		return MathF.Max( 0.05f, baseCd * (1f - cdr) );
	}

	private List<Component> CollectAllEnemiesAlive()
	{
		var ghosts = Scene.GetAllComponents<NPC>().Where( n => n.Health > 0 ).Cast<Component>().ToList();
		var blackGhosts = Scene.GetAllComponents<BlackGhostNPC>().Where( n => n.Health > 0 ).Cast<Component>().ToList();
		var mummies = Scene.GetAllComponents<MummyNPC>().Where( m => m.Health > 0 ).Cast<Component>().ToList();
		var demons = Scene.GetAllComponents<DemonNPC>().Where( d => d.Health > 0 ).Cast<Component>().ToList();
		var skelets = Scene.GetAllComponents<SkeletNPC>().Where( s => s.Health > 0 ).Cast<Component>().ToList();
		var bosses = Scene.GetAllComponents<BossNPC>().Where( b => b.Health > 0 ).Cast<Component>().ToList();

		var all = new List<Component>( ghosts.Count + blackGhosts.Count + mummies.Count + demons.Count + skelets.Count + bosses.Count );
		all.AddRange( ghosts );
		all.AddRange( blackGhosts );
		all.AddRange( mummies );
		all.AddRange( demons );
		all.AddRange( skelets );
		all.AddRange( bosses );
		return all;
	}

	private List<Component> FindNearestTargets( List<Component> allEnemies, int count )
	{
		float currentRange = AttackRange * RangeMultiplier;
		float rangeSq = currentRange * currentRange;

		return allEnemies
			.Where( e => e != null && e.IsValid && e.Transform.Position.DistanceSquared( Transform.Position ) <= rangeSq )
			.OrderBy( e => e.Transform.Position.DistanceSquared( Transform.Position ) )
			.Take( count )
			.ToList();
	}

	private void StartLightningAttack( List<Component> initialTargets, List<Component> allEnemies )
	{
		_isAttacking = true;

		TriggerAttackAnim();

		var hitTargets = new List<Component>( 12 );

		foreach ( var startTarget in initialTargets )
		{
			if ( startTarget == null || !startTarget.IsValid ) continue;
			BuildAndApplyChainFrom( startTarget, allEnemies, hitTargets );
		}

		_currentTargets.Clear();
		_currentTargets.AddRange( hitTargets.Where( t => t != null && t.IsValid ) );

		_ = FinishAttack();
	}

	private void TriggerAttackAnim()
	{
		if ( _lastAttackAnim < AttackAnimMinInterval ) return;
		_lastAttackAnim = 0f;

		if ( PlayerRenderer == null || !PlayerRenderer.IsValid ) return;

		var sm = PlayerRenderer.SceneModel;
		if ( sm == null ) return;

		float speed = MathF.Max( 0.01f, AttackAnimSpeed );

		if ( sm.DirectPlayback != null )
		{
			sm.DirectPlayback.Play( StaffAttackSequence );

			float hold = MathF.Max( 0.08f, DirectPlaybackCancelAfter / speed );
			ApplyTempPlaybackRate( speed, hold );

			_dpToCancel = sm.DirectPlayback;
			_dpCancelAt = RealTime.Now + MathF.Max( 0.05f, DirectPlaybackCancelAfter / speed );

			if ( DebugAnim ) Log.Info( $"🎬 DP.Play({StaffAttackSequence}) speed={speed} hold={hold:F2}" );
			return;
		}

		if ( !string.IsNullOrWhiteSpace( FallbackBoolParam ) )
		{
			sm.SetAnimParameter( FallbackBoolParam, true );
			_pulseModel = sm;
			_pulseActive = true;
			_pulseResetAt = RealTime.Now + MathF.Max( 0.01f, FallbackPulseTime );

			ApplyTempPlaybackRate( speed, MathF.Max( 0.06f, FallbackPulseTime / speed ) );

			if ( DebugAnim ) Log.Info( $"🎬 Fallback pulse: {FallbackBoolParam}=true speed={speed}" );
		}
	}

	private void BuildAndApplyChainFrom( Component startTarget, List<Component> allEnemies, List<Component> hitTargets )
	{
		int effectiveBaseDamage = GetEffectiveAttackDamage();
		DealStaffHit( startTarget, effectiveBaseDamage, out _, out _ );
		DrawLightningFromStaffTo( startTarget.Transform.Position );
		AddUnique( hitTargets, startTarget );

		if ( RicochetBounces <= 0 )
			return;

		float chainRange = (AttackRange * RangeMultiplier) * RicochetRangeMultiplier;
		float chainRangeSq = chainRange * chainRange;

		Component current = startTarget;

		for ( int bounce = 1; bounce <= RicochetBounces; bounce++ )
		{
			var next = FindNextRicochetTarget( current, allEnemies, hitTargets, chainRangeSq );
			if ( next == null ) break;

			int ricBase = Math.Max( 1, (int)MathF.Round( effectiveBaseDamage * MathF.Pow( Clamp( RicochetDamageMultiplier, 0.05f, 1f ), bounce ) ) );

			DealStaffHit( next, ricBase, out _, out _ );
			DrawLightningBetweenTargets( current.Transform.Position, next.Transform.Position );

			AddUnique( hitTargets, next );
			current = next;
		}
	}

	private Component FindNextRicochetTarget( Component from, List<Component> allEnemies, List<Component> alreadyHit, float rangeSq )
	{
		var fromPos = from.Transform.Position;

		Component best = null;
		float bestD2 = rangeSq;

		for ( int i = 0; i < allEnemies.Count; i++ )
		{
			var e = allEnemies[i];
			if ( e == null || !e.IsValid ) continue;
			if ( ReferenceEquals( e, from ) ) continue;
			if ( ContainsRef( alreadyHit, e ) ) continue;

			float d2 = e.Transform.Position.DistanceSquared( fromPos );
			if ( d2 <= bestD2 )
			{
				bestD2 = d2;
				best = e;
			}
		}

		return best;
	}

	private void DealStaffHit( Component target, int baseDamage, out bool didCrit, out bool didExecute )
	{
		didCrit = false;
		didExecute = false;

		if ( target == null || !target.IsValid ) return;
		if ( baseDamage < 1 ) baseDamage = 1;

		float dmgF = baseDamage;

		if ( EnableExecute )
		{
			if ( TryGetEstimatedHealthFraction( target, out float frac ) )
			{
				if ( frac <= Clamp( ExecuteHpThreshold, 0.01f, 0.99f ) )
				{
					dmgF *= Clamp( ExecuteBonusMultiplier, 1f, 10f );
					didExecute = true;
				}
			}
		}

		if ( CritChance > 0f )
		{
			float chance = Clamp( CritChance, 0f, 1f );
			if ( (float)_rng.NextDouble() < chance )
			{
				dmgF *= Clamp( CritMultiplier, 1f, 10f );
				didCrit = true;
			}
		}

		int finalDamage = Math.Max( 1, (int)MathF.Round( dmgF ) );
		ApplyDamageToEnemyAndMaybeCountKill( target, finalDamage, didCrit, didExecute );
		TryApplyRadiationSickness( target );
	}

	private bool TryGetEstimatedHealthFraction( Component target, out float fraction )
	{
		fraction = 1f;

		Guid id = target.GameObject.Id;
		float hp = GetEnemyHealth( target );
		if ( hp <= 0f ) return false;

		if ( !_estimatedMaxHpById.TryGetValue( id, out float maxEst ) )
		{
			maxEst = hp;
			_estimatedMaxHpById[id] = maxEst;
		}
		else if ( hp > maxEst )
		{
			maxEst = hp;
			_estimatedMaxHpById[id] = maxEst;
		}

		if ( maxEst <= 0.001f ) return false;

		fraction = Clamp( hp / maxEst, 0f, 1f );
		return true;
	}

	private float GetEnemyHealth( Component target )
	{
		if ( target is NPC ghost ) return ghost.Health;
		if ( target is BlackGhostNPC blackGhost ) return blackGhost.Health;
		if ( target is MummyNPC mummy ) return mummy.Health;
		if ( target is DemonNPC demon ) return demon.Health;
		if ( target is SkeletNPC skelet ) return skelet.Health;
		if ( target is BossNPC boss ) return boss.Health;
		return 0f;
	}

	private void ApplyDamageToEnemyAndMaybeCountKill( Component target, int dmg, bool didCrit, bool didExecute )
	{
		if ( dmg < 1 ) dmg = 1;

		var dmgSys = DamageNumberSystem.Get( Scene );

		DamageNumberSystem.DamageNumberKind kind =
			didExecute ? DamageNumberSystem.DamageNumberKind.Execute :
			didCrit ? DamageNumberSystem.DamageNumberKind.Crit :
			DamageNumberSystem.DamageNumberKind.Normal;

		if ( target is NPC ghost )
		{
			float before = ghost.Health;
			if ( before <= 0f ) return;

			ghost.TakeDamage( dmg );

			float after = ghost.Health;
			int dealt = (int)MathF.Round( MathF.Max( 0f, before - MathF.Max( 0f, after ) ) );
			if ( dealt > 0 && dmgSys != null ) dmgSys.SpawnDamage( ghost.Transform.Position, dealt, kind );
			if ( before > 0f && after <= 0f ) OnStaffKill();
			return;
		}

		if ( target is BlackGhostNPC blackGhost )
		{
			float before = blackGhost.Health;
			if ( before <= 0f ) return;

			blackGhost.TakeDamage( dmg );

			float after = blackGhost.Health;
			int dealt = (int)MathF.Round( MathF.Max( 0f, before - MathF.Max( 0f, after ) ) );
			if ( dealt > 0 && dmgSys != null ) dmgSys.SpawnDamage( blackGhost.Transform.Position, dealt, kind );
			if ( before > 0f && after <= 0f ) OnStaffKill();
			return;
		}

		if ( target is MummyNPC mummy )
		{
			float before = mummy.Health;
			if ( before <= 0f ) return;

			mummy.TakeDamage( dmg );

			float after = mummy.Health;
			int dealt = (int)MathF.Round( MathF.Max( 0f, before - MathF.Max( 0f, after ) ) );
			if ( dealt > 0 && dmgSys != null ) dmgSys.SpawnDamage( mummy.Transform.Position, dealt, kind );
			if ( before > 0f && after <= 0f ) OnStaffKill();
			return;
		}

		if ( target is DemonNPC demon )
		{
			float before = demon.Health;
			if ( before <= 0f ) return;

			demon.TakeDamage( dmg );

			float after = demon.Health;
			int dealt = (int)MathF.Round( MathF.Max( 0f, before - MathF.Max( 0f, after ) ) );
			if ( dealt > 0 && dmgSys != null ) dmgSys.SpawnDamage( demon.Transform.Position, dealt, kind );
			if ( before > 0f && after <= 0f ) OnStaffKill();
			return;
		}

		if ( target is SkeletNPC skelet )
		{
			float before = skelet.Health;
			if ( before <= 0f ) return;

			skelet.TakeDamage( dmg );

			float after = skelet.Health;
			int dealt = (int)MathF.Round( MathF.Max( 0f, before - MathF.Max( 0f, after ) ) );
			if ( dealt > 0 && dmgSys != null ) dmgSys.SpawnDamage( skelet.Transform.Position, dealt, kind );
			if ( before > 0f && after <= 0f ) OnStaffKill();
			return;
		}

		if ( target is BossNPC boss )
		{
			float before = boss.Health;
			if ( before <= 0f ) return;

			boss.TakeDamage( dmg );

			float after = boss.Health;
			int dealt = (int)MathF.Round( MathF.Max( 0f, before - MathF.Max( 0f, after ) ) );
			if ( dealt > 0 && dmgSys != null ) dmgSys.SpawnDamage( boss.Transform.Position, dealt, kind );
			if ( before > 0f && after <= 0f ) OnStaffKill();
			return;
		}
	}

	private void OnStaffKill()
	{
		if ( _playerStats != null && GoldFeverLevel > 0 )
		{
			int bonusGold = CurrentGoldPerKillBonus;
			if ( bonusGold > 0 )
				_playerStats.AddCoins( bonusGold );
		}

		if ( !EnableKillCooldownStacks )
			return;

		float add = KillCooldownReductionPerKill;
		KillCooldownReductionCurrent = Clamp( KillCooldownReductionCurrent + add, 0f, Clamp( KillCooldownReductionMax, 0f, 0.95f ) );

		if ( DebugMode )
			Log.Info( $"💀 Kill CDR: +{add * 100f:F2}% => {(KillCooldownReductionCurrent * 100f):F2}% / {(KillCooldownReductionMax * 100f):F0}%" );
	}

	private void DrawLightningFromStaffTo( Vector3 targetPos )
	{
		Vector3 startPos = (StaffModel?.Transform.Position ?? (Transform.Position + Vector3.Up * 30f)) + BeamStartOffset;
		Vector3 endPos = targetPos + BeamEndOffset;
		DrawLightningSegment( startPos, endPos );
	}

	private void DrawLightningBetweenTargets( Vector3 fromPos, Vector3 toPos )
	{
		Vector3 startPos = fromPos + BeamStartOffset;
		Vector3 endPos = toPos + BeamEndOffset;
		DrawLightningSegment( startPos, endPos );
	}

	private void DrawLightningSegment( Vector3 startPos, Vector3 endPos )
	{
		if ( UseLineRendererFx && LineMaterial != null )
		{
			var go = new GameObject( true, "LightningLineFx" );
			var fx = go.Components.Create<LightningLineFx>();

			fx.LineMaterial = LineMaterial;
			fx.Tint = LineTint;

			fx.Lifetime = LineLifetime;
			fx.Width = LineWidth;

			fx.UseCylinder = LineUseCylinder;
			fx.CylinderSegments = LineCylinderSegments;

			fx.Segments = LineSegments;
			fx.Jitter = LineJitter;
			fx.RefreshRate = LineRefreshRate;

			fx.WorldSpaceUv = LineWorldSpaceUv;
			fx.UnitsPerTexture = LineUnitsPerTexture;
			fx.TextureScale = LineTextureScale;
			fx.TextureOffset = LineTextureOffset;
			fx.TextureScrollSpeed = LineTextureScrollSpeed;

			fx.DepthFeather = LineDepthFeather;

			fx.Setup( startPos, endPos, _rng.Next() );
			return;
		}

		if ( DebugMode )
			DebugOverlay.Line( startPos, endPos, LineTint, 0.15f );
	}

	private static void AddUnique( List<Component> list, Component c )
	{
		if ( c == null ) return;
		for ( int i = 0; i < list.Count; i++ )
			if ( ReferenceEquals( list[i], c ) )
				return;
		list.Add( c );
	}

	private static bool ContainsRef( List<Component> list, Component c )
	{
		for ( int i = 0; i < list.Count; i++ )
			if ( ReferenceEquals( list[i], c ) )
				return true;
		return false;
	}

	private static int ClampInt( int v, int min, int max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}

	private async Task FinishAttack()
	{
		await Task.DelaySeconds( MathF.Max( 0.02f, LineLifetime ) );
		_isAttacking = false;
	}
}
