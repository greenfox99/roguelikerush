using Sandbox;
using Sandbox.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;

public sealed class BossNPC : Component, IMyDamageable
{
	// =========================================================
	// REFERENCES
	// =========================================================
	[Property] public GameObject TargetPlayer { get; set; }
	[Property] public NavMeshAgent Agent { get; set; }
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }

	// =========================================================
	// BASIC
	// =========================================================
	[Property, Group( "Basic" )] public float Health { get; set; } = 1000f;
	[Property, Group( "Basic" )] public float MaxHealth { get; set; } = 1000f;

	[Property, Group( "Basic" )] public float AttackRange { get; set; } = 115f;
	[Property, Group( "Basic" )] public float AttackCooldown { get; set; } = 2.2f;

	[Property, Group( "Basic" )] public float WalkSpeed { get; set; } = 130f;
	[Property, Group( "Basic" )] public float RunSpeed { get; set; } = 260f;
	[Property, Group( "Basic" )] public float RunStartDistance { get; set; } = 500f;

	// =========================================================
	// ATTACK CHANCES
	// =========================================================
	[Property, Group( "Attack Chances" )] public int SwordChance { get; set; } = 50;
	[Property, Group( "Attack Chances" )] public int HandChance { get; set; } = 30;
	[Property, Group( "Attack Chances" )] public int KickChance { get; set; } = 20;

	// =========================================================
	// ATTACK - SWORD
	// =========================================================
	[Property, Group( "Attack / Sword" )] public string SwordSequence { get; set; } = "boss_attack_sword";
	[Property, Group( "Attack / Sword" )] public int SwordDamage { get; set; } = 40;
	[Property, Group( "Attack / Sword" )] public float SwordDuration { get; set; } = 1.35f;
	[Property, Group( "Attack / Sword" )] public float SwordHitDelay { get; set; } = 0.52f;

	// =========================================================
	// ATTACK - HAND
	// =========================================================
	[Property, Group( "Attack / Hand" )] public string HandSequence { get; set; } = "boss_attack_hand";
	[Property, Group( "Attack / Hand" )] public int HandDamage { get; set; } = 28;
	[Property, Group( "Attack / Hand" )] public float HandDuration { get; set; } = 1.10f;
	[Property, Group( "Attack / Hand" )] public float HandHitDelay { get; set; } = 0.35f;

	// =========================================================
	// ATTACK - KICK
	// =========================================================
	[Property, Group( "Attack / Kick" )] public string KickSequence { get; set; } = "boss_kick";
	[Property, Group( "Attack / Kick" )] public int KickDamage { get; set; } = 34;
	[Property, Group( "Attack / Kick" )] public float KickDuration { get; set; } = 1.15f;
	[Property, Group( "Attack / Kick" )] public float KickHitDelay { get; set; } = 0.42f;

	[Property, Group( "Attack / Kick" )] public bool UseKickKnockback { get; set; } = true;
	[Property, Group( "Attack / Kick" )] public float KickKnockbackDistance { get; set; } = 220f;
	[Property, Group( "Attack / Kick" )] public float KickKnockbackDuration { get; set; } = 0.18f;
	[Property, Group( "Attack / Kick" )] public float KickKnockbackLift { get; set; } = 12f;

	[Property, Group( "Attack" )] public float AttackPlaybackRate { get; set; } = 1.0f;
	[Property, Group( "Attack" )] public float DamageRadiusBonus { get; set; } = 10f;

	// =========================================================
	// PROJECTILE / SKULL
	// =========================================================
	[Property, Group( "Projectile / Skull" )] public bool EnableSkullProjectile { get; set; } = true;
	[Property, Group( "Projectile / Skull" )] public GameObject ProjectilePrefab { get; set; }
	[Property, Group( "Projectile / Skull" )] public int ProjectileDamage { get; set; } = 32;
	[Property, Group( "Projectile / Skull" )] public float ProjectileSpeed { get; set; } = 820f;
	[Property, Group( "Projectile / Skull" )] public float ProjectileLifetime { get; set; } = 3.0f;
	[Property, Group( "Projectile / Skull" )] public float ProjectileMinInterval { get; set; } = 15f;
	[Property, Group( "Projectile / Skull" )] public float ProjectileMaxInterval { get; set; } = 60f;
	[Property, Group( "Projectile / Skull" )] public float ProjectileMinDistance { get; set; } = 160f;
	[Property, Group( "Projectile / Skull" )] public float AimZOffset { get; set; } = 42f;
	[Property, Group( "Projectile / Skull" )] public Vector3 ProjectileSpawnOffset { get; set; } = new Vector3( 60f, 0f, 60f );

	// =========================================================
	// RAGE
	// =========================================================
	[Property, Group( "Rage" )] public string RageSequence { get; set; } = "boss_rage";
	[Property, Group( "Rage" )] public float RageDuration { get; set; } = 2.7f;
	[Property, Group( "Rage" )] public float RageMinInterval { get; set; } = 15f;
	[Property, Group( "Rage" )] public float RageMaxInterval { get; set; } = 60f;
	[Property, Group( "Rage" )] public float RagePlaybackRate { get; set; } = 1.0f;

	// =========================================================
	// LOW HP FRENZY
	// =========================================================
	[Property, Group( "Low HP Frenzy" )] public bool EnableLowHpFrenzy { get; set; } = true;
	[Property, Group( "Low HP Frenzy" )] public float FrenzyHealthFraction { get; set; } = 0.35f;
	[Property, Group( "Low HP Frenzy" )] public float FrenzyMoveSpeedMultiplier { get; set; } = 1.15f;
	[Property, Group( "Low HP Frenzy" )] public float FrenzyCooldownMultiplier { get; set; } = 0.80f;
	[Property, Group( "Low HP Frenzy" )] public float FrenzyDamageMultiplier { get; set; } = 1.15f;
	[Property, Group( "Low HP Frenzy" )] public float FrenzyRageIntervalMultiplier { get; set; } = 0.70f;
	[Property, Group( "Low HP Frenzy" )] public bool ForceRageOnFrenzyEnter { get; set; } = true;

	// =========================================================
	// MOVEMENT / TURN
	// =========================================================
	[Property, Group( "Movement" )] public float RepathInterval { get; set; } = 0.20f;
	[Property, Group( "Movement" )] public float StopDistance { get; set; } = 70f;

	[Property, Group( "Turn" )] public float TurnSpeed { get; set; } = 14f;
	[Property, Group( "Turn" )] public float TurnSnapAngle { get; set; } = 70f;
	[Property, Group( "Turn" )] public float TurnVelocityMin { get; set; } = 25f;

	// =========================================================
	// SEARCH / PERF
	// =========================================================
	[Property, Group( "Perf" )] public float PlayerSearchInterval { get; set; } = 0.50f;

	// =========================================================
	// ANTI STUCK
	// =========================================================
	[Property, Group( "AntiStuck" )] public bool AntiStuckEnabled { get; set; } = true;
	[Property, Group( "AntiStuck" )] public float StuckCheckInterval { get; set; } = 0.60f;
	[Property, Group( "AntiStuck" )] public float StuckMoveEpsilon { get; set; } = 12f;
	[Property, Group( "AntiStuck" )] public float StuckNudgeDistance { get; set; } = 180f;

	// =========================================================
	// ANIMATION
	// =========================================================
	[Property, Group( "Animation" )] public string WalkSequence { get; set; } = "boss_walk";
	[Property, Group( "Animation" )] public string RunSequence { get; set; } = "boss_run";
	[Property, Group( "Animation" )] public string DeathSequence { get; set; } = "boss_death";

	[Property, Group( "Animation" )] public bool SequenceBlending { get; set; } = true;
	[Property, Group( "Animation" )] public float WalkPlaybackRate { get; set; } = 1.0f;
	[Property, Group( "Animation" )] public float RunPlaybackRate { get; set; } = 1.0f;
	[Property, Group( "Animation" )] public float DeathPlaybackRate { get; set; } = 1.0f;

	// =========================================================
	// AUDIO - SFX
	// =========================================================
	[Property, Group( "Audio / SFX" )] public SoundEvent SwordHitSound { get; set; }
	[Property, Group( "Audio / SFX" )] public SoundEvent HandHitSound { get; set; }
	[Property, Group( "Audio / SFX" )] public SoundEvent KickHitSound { get; set; }
	[Property, Group( "Audio / SFX" )] public SoundEvent RageSound { get; set; }
	[Property, Group( "Audio / SFX" )] public SoundEvent DeathSound { get; set; }
	[Property, Group( "Audio / SFX" )] public SoundEvent HurtSound { get; set; }

	// =========================================================
	// FX
	// =========================================================
	[Property, Group( "FX" )] public GameObject DeathEffectPrefab { get; set; }
	[Property, Group( "FX" )] public float FadeOutTime { get; set; } = 2f;

	// =========================================================
	// DEBUG
	// =========================================================
	[Property, Group( "Debug" )] public bool DebugMode { get; set; } = false;

	// =========================================================
	// NPC BUFF BRIDGE
	// =========================================================
	public float NpcBuff_MoveSpeedMult { get; set; } = 1f;
	public float NpcBuff_DamageMult { get; set; } = 1f;
	public float NpcBuff_DamageTakenMult { get; set; } = 1f;

	public float NpcBuff_BaseWalkSpeed { get; set; } = 0f;
	public float NpcBuff_BaseRunSpeed { get; set; } = 0f;

	// =========================================================
	// HUD BRIDGE
	// =========================================================
	public bool IsBossFightActive => _state != BossState.Dead && TargetPlayer != null && TargetPlayer.IsValid;

	// =========================================================
	// INTERNAL
	// =========================================================
	private enum BossState
	{
		Idle,
		Chasing,
		Running,
		Attacking,
		Raging,
		Dead
	}

	private enum AttackType
	{
		Sword,
		Hand,
		Kick
	}

	private struct AttackData
	{
		public AttackType Type;
		public string Sequence;
		public int Damage;
		public float Duration;
		public float HitDelay;
	}

	private BossState _state = BossState.Idle;
	private AttackData _currentAttack;

	private float _lastAttack = 999f;
	private float _playerSearchTimer = 999f;
	private float _repathTimer = 999f;
	private float _rageTimer = 0f;
	private float _nextRageDelay = 30f;
	private float _projectileTimer = 0f;
	private float _nextProjectileDelay = 30f;
	private float _stateTime = 0f;

	private bool _hitDone = false;
	private bool _frenzyActive = false;
	private bool _pendingForcedRage = false;
	private bool _kickKnockbackRunning = false;

	private float _stuckTimer = 0f;
	private Vector3 _lastStuckPos;

	private Vector3 _lastPos;
	private Vector3 _smoothedVel;

	private string _currentSequence = "";
	private bool _sequenceLooping = true;
	private float _sequenceRate = 1f;

	private static readonly Random Rng = new Random();

	private float WorldDelta => Time.Delta * TimeSlowSystem.WorldScale;

	private float EffectiveWalkSpeed
	{
		get
		{
			float baseSpeed = (NpcBuff_BaseWalkSpeed > 0f) ? NpcBuff_BaseWalkSpeed : WalkSpeed;
			float frenzyMult = _frenzyActive ? FrenzyMoveSpeedMultiplier : 1f;
			return MathF.Max( 0f, baseSpeed * SafeMult( NpcBuff_MoveSpeedMult ) * frenzyMult );
		}
	}

	private float EffectiveRunSpeed
	{
		get
		{
			float baseSpeed = (NpcBuff_BaseRunSpeed > 0f) ? NpcBuff_BaseRunSpeed : RunSpeed;
			float frenzyMult = _frenzyActive ? FrenzyMoveSpeedMultiplier : 1f;
			return MathF.Max( 0f, baseSpeed * SafeMult( NpcBuff_MoveSpeedMult ) * frenzyMult );
		}
	}

	private float EffectiveAttackCooldown
	{
		get
		{
			float frenzyMult = _frenzyActive ? FrenzyCooldownMultiplier : 1f;
			return MathF.Max( 0.1f, AttackCooldown * frenzyMult );
		}
	}

	protected override void OnStart()
	{
		if ( ModelRenderer == null )
			ModelRenderer = Components.GetInChildren<SkinnedModelRenderer>( true );

		if ( Agent == null )
			Agent = Components.Get<NavMeshAgent>();

		Health = MaxHealth;

		if ( NpcBuff_BaseWalkSpeed <= 0f )
			NpcBuff_BaseWalkSpeed = WalkSpeed;

		if ( NpcBuff_BaseRunSpeed <= 0f )
			NpcBuff_BaseRunSpeed = RunSpeed;

		FindPlayer();
		_playerSearchTimer = PlayerSearchInterval;

		_lastAttack = AttackCooldown;
		_repathTimer = RepathInterval;
		_lastStuckPos = Transform.Position;
		_stuckTimer = 0f;

		_lastPos = Transform.Position;
		_smoothedVel = Vector3.Zero;

		ScheduleNextRage();
		ScheduleNextProjectile();
		PlaySequence( WalkSequence, true, WalkPlaybackRate );
		TryApplyAgentSpeed();

		if ( DebugMode )
			Log.Info( $"[BossNPC] Init. Agent={(Agent != null ? "OK" : "MISSING")} Renderer={(ModelRenderer != null ? "OK" : "MISSING")}" );
	}

	protected override void OnUpdate()
	{
		if ( _state == BossState.Dead )
			return;

		float dt = WorldDelta;

		_lastAttack += dt;
		_playerSearchTimer += dt;
		_repathTimer += dt;
		_rageTimer += dt;
		_projectileTimer += dt;
		_stuckTimer += dt;
		_stateTime += dt;

		UpdateVelocity( dt );
		UpdateFrenzyState();
		TryApplyAgentSpeed();

		if ( Health <= 0f )
		{
			Die();
			return;
		}

		if ( TargetPlayer == null || !TargetPlayer.IsValid )
		{
			if ( _playerSearchTimer >= PlayerSearchInterval )
			{
				_playerSearchTimer = 0f;
				FindPlayer();
			}

			StopAgent();

			if ( _state != BossState.Attacking && _state != BossState.Raging )
				PlaySequence( WalkSequence, true, WalkPlaybackRate );

			return;
		}

		if ( _pendingForcedRage && _state != BossState.Attacking && _state != BossState.Raging )
		{
			_pendingForcedRage = false;
			StartRage();
			return;
		}

		switch ( _state )
		{
			case BossState.Attacking:
				UpdateAttack();
				break;

			case BossState.Raging:
				UpdateRage();
				break;

			default:
				UpdateChase();
				break;
		}

		if ( AntiStuckEnabled )
			UpdateAntiStuck();
	}

	private void UpdateChase()
	{
		if ( TargetPlayer == null || !TargetPlayer.IsValid )
		{
			_state = BossState.Idle;
			_stateTime = 0f;
			StopAgent();
			PlaySequence( WalkSequence, true, WalkPlaybackRate );
			return;
		}

		Vector3 playerPos = TargetPlayer.Transform.Position;
		Vector3 npcPos = Transform.Position;
		float dist = Vector3.DistanceBetween( npcPos, playerPos );

		if ( _rageTimer >= _nextRageDelay )
		{
			StartRage();
			return;
		}

		if ( ShouldThrowProjectile( dist ) )
		{
			StopAgent();
			TurnTowards( (playerPos - npcPos).Normal );
			ThrowProjectileAtPlayer();
			ScheduleNextProjectile();
			PlaySequence( WalkSequence, true, WalkPlaybackRate );
			return;
		}

		if ( dist <= AttackRange )
		{
			_state = BossState.Attacking;
			_stateTime = 0f;
			StopAgent();

			TurnTowards( (playerPos - npcPos).Normal );

			if ( _lastAttack >= EffectiveAttackCooldown )
				StartAttack();
			else
			{
				_hitDone = true;
				PlaySequence( WalkSequence, true, WalkPlaybackRate );
			}

			return;
		}

		bool shouldRun = dist >= RunStartDistance;
		_state = shouldRun ? BossState.Running : BossState.Chasing;

		if ( _repathTimer >= RepathInterval )
		{
			_repathTimer = 0f;
			MoveAgentTo( playerPos, shouldRun ? EffectiveRunSpeed : EffectiveWalkSpeed );
		}

		TurnByVelocityOrDirection( playerPos - npcPos );

		if ( dist <= StopDistance )
			StopAgent();

		if ( shouldRun )
			PlaySequence( RunSequence, true, RunPlaybackRate );
		else
			PlaySequence( WalkSequence, true, WalkPlaybackRate );
	}

	private void StartAttack()
	{
		_currentAttack = PickAttack();

		_lastAttack = 0f;
		_hitDone = false;
		_state = BossState.Attacking;
		_stateTime = 0f;

		StopAgent();
		PlaySequence( _currentAttack.Sequence, false, AttackPlaybackRate );

		if ( DebugMode )
			Log.Info( $"[BossNPC] Attack: {_currentAttack.Type}" );
	}

	private void UpdateAttack()
	{
		if ( TargetPlayer == null || !TargetPlayer.IsValid )
		{
			_state = BossState.Idle;
			_stateTime = 0f;
			return;
		}

		Vector3 toPlayer = (TargetPlayer.Transform.Position - Transform.Position).Normal;
		TurnTowards( toPlayer );

		if ( !_hitDone && _stateTime >= _currentAttack.HitDelay )
		{
			_hitDone = true;
			PlayAttackSound( _currentAttack.Type );
			TryDealDamage( _currentAttack.Damage, _currentAttack.Type );
		}

		if ( _stateTime >= _currentAttack.Duration )
		{
			_state = BossState.Chasing;
			_stateTime = 0f;
			PlaySequence( WalkSequence, true, WalkPlaybackRate );
		}
	}

	private void StartRage()
	{
		_state = BossState.Raging;
		_stateTime = 0f;
		_rageTimer = 0f;

		StopAgent();
		PlaySequence( RageSequence, false, RagePlaybackRate );
		PlayOneShotAttached( RageSound );

		if ( DebugMode )
			Log.Info( "[BossNPC] Rage started" );
	}

	private void UpdateRage()
	{
		if ( TargetPlayer != null && TargetPlayer.IsValid )
		{
			Vector3 toPlayer = (TargetPlayer.Transform.Position - Transform.Position).Normal;
			TurnTowards( toPlayer );
		}

		if ( _stateTime >= RageDuration )
		{
			ScheduleNextRage();
			_state = (TargetPlayer != null && TargetPlayer.IsValid) ? BossState.Chasing : BossState.Idle;
			_stateTime = 0f;
			PlaySequence( WalkSequence, true, WalkPlaybackRate );
		}
	}

	private void TryDealDamage( int damage, AttackType attackType )
	{
		if ( TargetPlayer == null || !TargetPlayer.IsValid )
			return;

		float dist = Vector3.DistanceBetween( Transform.Position, TargetPlayer.Transform.Position );
		if ( dist > AttackRange + DamageRadiusBonus )
			return;

		var playerHealth = TargetPlayer.Components.Get<PlayerHealth>();
		if ( playerHealth != null )
			playerHealth.TakeDamage( damage );

		if ( attackType == AttackType.Kick && UseKickKnockback )
			TryApplyKickKnockback();
	}

	private bool ShouldThrowProjectile( float dist )
	{
		if ( !EnableSkullProjectile )
			return false;

		if ( ProjectilePrefab == null || !ProjectilePrefab.IsValid )
			return false;

		if ( TargetPlayer == null || !TargetPlayer.IsValid )
			return false;

		if ( _state == BossState.Attacking || _state == BossState.Raging || _state == BossState.Dead )
			return false;

		if ( dist <= MathF.Max( AttackRange, ProjectileMinDistance ) )
			return false;

		return _projectileTimer >= _nextProjectileDelay;
	}

	private void ThrowProjectileAtPlayer()
	{
		if ( ProjectilePrefab == null || !ProjectilePrefab.IsValid )
		{
			if ( DebugMode )
				Log.Warning( $"[BossNPC] ProjectilePrefab не назначен на '{GameObject?.Name}'" );
			return;
		}

		if ( TargetPlayer == null || !TargetPlayer.IsValid )
			return;

		Vector3 spawnPos =
			Transform.Position
			+ Transform.Rotation.Forward * ProjectileSpawnOffset.x
			+ Transform.Rotation.Right * ProjectileSpawnOffset.y
			+ Vector3.Up * ProjectileSpawnOffset.z;

		Vector3 aimPos = TargetPlayer.Transform.Position + Vector3.Up * AimZOffset;
		Vector3 dir = aimPos - spawnPos;

		if ( dir.Length < 0.001f )
			dir = Transform.Rotation.Forward;
		else
			dir = dir.Normal;

		var projGO = ProjectilePrefab.Clone( spawnPos, Rotation.LookAt( dir ) );
		projGO.Name = "BossSkullProjectile";

		var proj = projGO.Components.Get<Projectile>( FindMode.EnabledInSelfAndDescendants );
		if ( proj == null )
		{
			if ( DebugMode )
				Log.Warning( $"[BossNPC] В ProjectilePrefab нет компонента Projectile.cs (или он Disabled)." );
			return;
		}

		proj.Owner = GameObject;
		proj.Direction = dir;
		proj.Speed = ProjectileSpeed;
		proj.Damage = GetEffectiveDamage( ProjectileDamage );
		proj.Lifetime = ProjectileLifetime;

		if ( DebugMode )
			Log.Info( $"[BossNPC] Throw skull projectile: dmg={proj.Damage}, speed={proj.Speed}, lifetime={proj.Lifetime}" );
	}

	private void TryApplyKickKnockback()
	{
		if ( TargetPlayer == null || !TargetPlayer.IsValid )
			return;

		if ( _kickKnockbackRunning )
			return;

		Vector3 dir = TargetPlayer.Transform.Position - Transform.Position;
		dir = dir.WithZ( 0f );

		if ( dir.Length < 0.001f )
			dir = Transform.Rotation.Forward;
		else
			dir = dir.Normal;

		_ = KickKnockbackRoutine( dir );
	}

	private async Task KickKnockbackRoutine( Vector3 horizontalDir )
	{
		_kickKnockbackRunning = true;

		float totalDistance = MathF.Max( 0f, KickKnockbackDistance );
		float duration = MathF.Max( 0.05f, KickKnockbackDuration );
		float lift = KickKnockbackLift;

		Vector3 start = TargetPlayer.Transform.Position;
		Vector3 target = start + horizontalDir * totalDistance + Vector3.Up * lift;

		float time = 0f;

		while ( time < duration )
		{
			if ( TargetPlayer == null || !TargetPlayer.IsValid )
				break;

			time += WorldDelta;
			float t = time / duration;
			if ( t > 1f ) t = 1f;

			float arc = 4f * t * (1f - t);
			Vector3 pos = Vector3.Lerp( start, target, t );
			pos.z += arc * lift;

			TargetPlayer.Transform.Position = pos;

			await Task.DelaySeconds( 0.01f );
		}

		_kickKnockbackRunning = false;
	}

	private AttackData PickAttack()
	{
		int swordWeight = Math.Max( 0, SwordChance );
		int handWeight = Math.Max( 0, HandChance );
		int kickWeight = Math.Max( 0, KickChance );

		if ( string.IsNullOrWhiteSpace( SwordSequence ) ) swordWeight = 0;
		if ( string.IsNullOrWhiteSpace( HandSequence ) ) handWeight = 0;
		if ( string.IsNullOrWhiteSpace( KickSequence ) ) kickWeight = 0;

		int total = swordWeight + handWeight + kickWeight;

		if ( total <= 0 )
		{
			return new AttackData
			{
				Type = AttackType.Sword,
				Sequence = SwordSequence,
				Damage = GetEffectiveDamage( SwordDamage ),
				Duration = SwordDuration,
				HitDelay = SwordHitDelay
			};
		}

		int roll = Rng.Next( 0, total );

		if ( roll < swordWeight )
		{
			return new AttackData
			{
				Type = AttackType.Sword,
				Sequence = SwordSequence,
				Damage = GetEffectiveDamage( SwordDamage ),
				Duration = SwordDuration,
				HitDelay = SwordHitDelay
			};
		}

		roll -= swordWeight;
		if ( roll < handWeight )
		{
			return new AttackData
			{
				Type = AttackType.Hand,
				Sequence = HandSequence,
				Damage = GetEffectiveDamage( HandDamage ),
				Duration = HandDuration,
				HitDelay = HandHitDelay
			};
		}

		return new AttackData
		{
			Type = AttackType.Kick,
			Sequence = KickSequence,
			Damage = GetEffectiveDamage( KickDamage ),
			Duration = KickDuration,
			HitDelay = KickHitDelay
		};
	}

	private int GetEffectiveDamage( int baseDamage )
	{
		float dmg = baseDamage;
		dmg *= SafeMult( NpcBuff_DamageMult );

		if ( _frenzyActive )
			dmg *= FrenzyDamageMultiplier;

		return Math.Max( 1, (int)MathF.Round( dmg ) );
	}

	private void UpdateFrenzyState()
	{
		if ( !EnableLowHpFrenzy || _frenzyActive || MaxHealth <= 0f )
			return;

		float hpFraction = Health / MaxHealth;
		if ( hpFraction > FrenzyHealthFraction )
			return;

		_frenzyActive = true;

		if ( ForceRageOnFrenzyEnter )
		{
			if ( _state == BossState.Attacking )
				_pendingForcedRage = true;
			else if ( _state != BossState.Raging )
				StartRage();
		}

		if ( DebugMode )
			Log.Info( "[BossNPC] Frenzy activated" );
	}

	private void FindPlayer()
	{
		var player = Scene.GetAllComponents<Player>().FirstOrDefault();
		if ( player != null )
			TargetPlayer = player.GameObject;
	}

	// =========================================================
	// AUDIO
	// =========================================================
	private void PlayAttackSound( AttackType attackType )
	{
		switch ( attackType )
		{
			case AttackType.Sword:
				PlayOneShotAttached( SwordHitSound );
				break;

			case AttackType.Hand:
				PlayOneShotAttached( HandHitSound );
				break;

			case AttackType.Kick:
				PlayOneShotAttached( KickHitSound );
				break;
		}
	}

	private void PlayOneShotAttached( SoundEvent sound )
	{
		if ( sound == null )
			return;

		GameObject.PlaySound( sound, Vector3.Zero );
	}

	// =========================================================
	// ANIMATION
	// =========================================================
	private void PlaySequence( string name, bool looping, float playbackRate )
	{
		if ( ModelRenderer == null ) return;
		if ( string.IsNullOrWhiteSpace( name ) ) return;

		if ( _currentSequence == name && _sequenceLooping == looping && MathF.Abs( _sequenceRate - playbackRate ) < 0.001f )
			return;

		_currentSequence = name;
		_sequenceLooping = looping;
		_sequenceRate = playbackRate;

		ModelRenderer.Sequence.Name = name;
		ModelRenderer.Sequence.Looping = looping;
		ModelRenderer.Sequence.Blending = SequenceBlending;
		ModelRenderer.Sequence.PlaybackRate = playbackRate;
	}

	// =========================================================
	// TURN
	// =========================================================
	private void UpdateVelocity( float dt )
	{
		Vector3 pos = Transform.Position;
		Vector3 rawVel = (pos - _lastPos) / MathF.Max( 0.0001f, dt );
		_lastPos = pos;

		float lerp = 1f - MathF.Pow( 0.001f, dt );
		_smoothedVel = Vector3.Lerp( _smoothedVel, rawVel, lerp );
	}

	private void TurnByVelocityOrDirection( Vector3 fallbackDir )
	{
		if ( _smoothedVel.Length >= TurnVelocityMin )
		{
			TurnTowards( _smoothedVel.Normal );
			return;
		}

		TurnTowards( fallbackDir.Normal );
	}

	private void TurnTowards( Vector3 dir )
	{
		if ( dir.Length < 0.01f ) return;

		Rotation want = Rotation.LookAt( dir );
		float ang = AngleBetweenDirectionsDegrees( Transform.Rotation.Forward, want.Forward );

		if ( ang >= TurnSnapAngle )
		{
			Transform.Rotation = want;
			return;
		}

		Transform.Rotation = Rotation.Slerp( Transform.Rotation, want, WorldDelta * TurnSpeed );
	}

	private static float AngleBetweenDirectionsDegrees( Vector3 a, Vector3 b )
	{
		a = a.Normal;
		b = b.Normal;

		float dot = a.Dot( b );
		dot = Clamp( dot, -1f, 1f );

		float rad = MathF.Acos( dot );
		return rad * (180f / MathF.PI);
	}

	// =========================================================
	// NAV
	// =========================================================
	private void MoveAgentTo( Vector3 targetPosition, float speed )
	{
		if ( Agent == null || !Agent.IsValid )
		{
			Vector3 dir = (targetPosition - Transform.Position).Normal;
			Transform.Position += dir * speed * WorldDelta;
			return;
		}

		try
		{
			Agent.MaxSpeed = speed * TimeSlowSystem.WorldScale;
		}
		catch
		{
		}

		Agent.MoveTo( targetPosition );
	}

	private void StopAgent()
	{
		if ( Agent == null || !Agent.IsValid ) return;
		Agent.Stop();
	}

	private void TryApplyAgentSpeed()
	{
		if ( Agent == null || !Agent.IsValid ) return;

		float desiredSpeed = (_state == BossState.Running) ? EffectiveRunSpeed : EffectiveWalkSpeed;
		float spd = desiredSpeed * TimeSlowSystem.WorldScale;

		try
		{
			Agent.MaxSpeed = spd;
		}
		catch
		{
		}
	}

	// =========================================================
	// ANTI STUCK
	// =========================================================
	private void UpdateAntiStuck()
	{
		if ( _state != BossState.Chasing && _state != BossState.Running ) return;
		if ( Agent == null || !Agent.IsValid ) return;

		if ( _stuckTimer < StuckCheckInterval ) return;
		_stuckTimer = 0f;

		Vector3 now = Transform.Position;
		float moved = Vector3.DistanceBetween( now, _lastStuckPos );
		_lastStuckPos = now;

		if ( moved >= StuckMoveEpsilon )
			return;

		if ( TargetPlayer == null || !TargetPlayer.IsValid ) return;

		Vector2 n = RandomVector2InCircle( 1f );
		if ( n.Length < 0.001f ) n = new Vector2( 1f, 0f );
		n = n.Normal;

		Vector2 nudge2D = n * StuckNudgeDistance;
		Vector3 nudgeTarget = TargetPlayer.Transform.Position + new Vector3( nudge2D.x, nudge2D.y, 0f );

		Agent.MoveTo( nudgeTarget );
	}

	private static Vector2 RandomVector2InCircle( float radius )
	{
		float angle = (float)(Rng.NextDouble() * Math.PI * 2.0);
		float r = MathF.Sqrt( (float)Rng.NextDouble() ) * radius;

		return new Vector2(
			MathF.Cos( angle ) * r,
			MathF.Sin( angle ) * r
		);
	}

	private void ScheduleNextRage()
	{
		float min = MathF.Min( RageMinInterval, RageMaxInterval );
		float max = MathF.Max( RageMinInterval, RageMaxInterval );

		if ( _frenzyActive )
		{
			min *= FrenzyRageIntervalMultiplier;
			max *= FrenzyRageIntervalMultiplier;
		}

		_nextRageDelay = RandomFloat( min, max );
		_rageTimer = 0f;
	}

	private void ScheduleNextProjectile()
	{
		float min = MathF.Max( 0.1f, MathF.Min( ProjectileMinInterval, ProjectileMaxInterval ) );
		float max = MathF.Max( min, MathF.Max( ProjectileMinInterval, ProjectileMaxInterval ) );

		_nextProjectileDelay = RandomFloat( min, max );
		_projectileTimer = 0f;
	}

	private static float RandomFloat( float min, float max )
	{
		if ( max < min )
		{
			float t = min;
			min = max;
			max = t;
		}

		return min + (float)Rng.NextDouble() * (max - min);
	}

	// =========================================================
	// DAMAGE / DEATH
	// =========================================================
	public void TakeDamage( float damage )
	{
		if ( _state == BossState.Dead ) return;

		if ( damage < 0f ) damage = 0f;
		damage *= SafeMult( NpcBuff_DamageTakenMult );

		Health -= damage;
		PlayOneShotAttached( HurtSound );

		if ( Health <= 0f )
			Die();
	}

	private void Die()
	{
		if ( _state == BossState.Dead ) return;

		_state = BossState.Dead;
		_stateTime = 0f;
		StopAgent();

		Sandbox.Services.Achievements.Unlock( "boss_kill" );

		PlaySequence( DeathSequence, false, DeathPlaybackRate );
		PlayOneShotAttached( DeathSound );

		if ( DeathEffectPrefab != null )
		{
			var effect = DeathEffectPrefab.Clone( Transform.Position );
			effect.Name = "BossDeathEffect";
		}

		var collider = Components.Get<Collider>();
		if ( collider != null )
			collider.Enabled = false;

		_ = FadeOutAndDestroy( FadeOutTime );
	}

	private async Task FadeOutAndDestroy( float duration )
	{
		float time = 0f;

		while ( time < duration )
		{
			time += WorldDelta;
			float t = time / MathF.Max( 0.001f, duration );
			float alpha = 1f - t;

			if ( ModelRenderer != null )
				ModelRenderer.Tint = Color.White.WithAlpha( alpha );

			await Task.DelaySeconds( 0.01f );
		}

		GameObject.StopAllSounds();
		GameObject.Destroy();
	}

	// =========================================================
	// HELPERS
	// =========================================================
	private static float SafeMult( float v )
	{
		if ( float.IsNaN( v ) || float.IsInfinity( v ) ) return 1f;
		if ( v < 0.01f ) return 0.01f;
		if ( v > 1000f ) return 1000f;
		return v;
	}

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}
}
