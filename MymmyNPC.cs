using Sandbox;
using Sandbox.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;

public sealed class MummyNPC : Component, IMyDamageable
{
	// =========================
	// BASIC
	// =========================
	[Property] public GameObject TargetPlayer { get; set; }

	[Property] public float Health { get; set; } = 150f;
	[Property] public float MaxHealth { get; set; } = 150f;

	[Property] public float AttackRange { get; set; } = 100f;
	[Property] public float AttackCooldown { get; set; } = 2.5f;
	[Property] public int AttackDamage { get; set; } = 20;

	[Property] public float MoveSpeed { get; set; } = 150f;

	// =========================
	// NPC BUFF BRIDGE (совместимость с NpcBuffReceiver)
	// =========================
	public float NpcBuff_MoveSpeedMult { get; set; } = 1f;
	public float NpcBuff_DamageMult { get; set; } = 1f;
	public float NpcBuff_DamageTakenMult { get; set; } = 1f;

	public float NpcBuff_BaseMoveSpeed { get; set; } = 0f;
	public int NpcBuff_BaseAttackDamage { get; set; } = 0;

	private float EffectiveMoveSpeed
		=> MathF.Max( 0f, (NpcBuff_BaseMoveSpeed > 0f ? NpcBuff_BaseMoveSpeed : MoveSpeed) * SafeMult( NpcBuff_MoveSpeedMult ) );

	private int EffectiveAttackDamage
	{
		get
		{
			int baseDmg = (NpcBuff_BaseAttackDamage > 0) ? NpcBuff_BaseAttackDamage : AttackDamage;
			float mult = SafeMult( NpcBuff_DamageMult );
			return Math.Max( 1, (int)MathF.Round( baseDmg * mult ) );
		}
	}

	// =========================
	// ATTACK TIMING
	// =========================
	[Property, Group( "Attack Timing" )] public float AttackDuration { get; set; } = 1.5f;
	[Property, Group( "Attack Timing" )] public float HitDelay { get; set; } = 0.45f;

	// =========================
	// TURN (FIX "ICE")
	// =========================
	[Property, Group( "Turn" )] public float TurnSpeed { get; set; } = 18f;
	[Property, Group( "Turn" )] public float TurnSnapAngle { get; set; } = 65f;
	[Property, Group( "Turn" )] public float TurnVelocityMin { get; set; } = 25f;

	// =========================
	// PERF
	// =========================
	[Property, Group( "Perf" )] public float PlayerSearchInterval { get; set; } = 0.50f;

	// =========================
	// NAV
	// =========================
	[Property] public NavMeshAgent Agent { get; set; }

	[Property, Group( "Nav" )] public float RepathInterval { get; set; } = 0.20f;
	[Property, Group( "Nav" )] public float StopDistance { get; set; } = 75f;

	// Разброс цели (чтобы не все бежали в одну точку)
	[Property, Group( "Nav" )] public float TargetOffsetRadius { get; set; } = 70f;
	[Property, Group( "Nav" )] public float TargetOffsetRefresh { get; set; } = 0.65f;

	// Separation (мягкое расталкивание)
	[Property, Group( "Nav" )] public bool SeparationEnabled { get; set; } = true;
	[Property, Group( "Nav" )] public float SeparationRadius { get; set; } = 90f;
	[Property, Group( "Nav" )] public float SeparationStrength { get; set; } = 70f;
	[Property, Group( "Nav" )] public float SeparationMaxStep { get; set; } = 0.75f;

	// Anti-stuck
	[Property, Group( "Nav" )] public bool AntiStuckEnabled { get; set; } = true;
	[Property, Group( "Nav" )] public float StuckCheckInterval { get; set; } = 0.60f;
	[Property, Group( "Nav" )] public float StuckMoveEpsilon { get; set; } = 12f;
	[Property, Group( "Nav" )] public float StuckNudgeDistance { get; set; } = 180f;

	// =========================
	// VISUAL / ANIM / FX
	// =========================
	[Property] public bool DebugMode { get; set; } = true;

	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }

	// === Sequence-анимации (без animgraph) ===
	[Property, Group( "Animation (Sequence)" )] public string RunSequence { get; set; } = "Sprint_N";

	[Property, Group( "Animation (Sequence)" )]
	public string[] AttackSequences { get; set; } =
	{
		"Zombie_Attack", "Zombie_Attack2", "Zombie_Attack3", "Zombie_Attack4"
	};

	[Property, Group( "Animation (Sequence)" )]
	public string[] DeathSequences { get; set; } =
	{
		"Death_Headshot", "Death_Random", "Death_Random2"
	};

	[Property, Group( "Animation (Sequence)" )] public bool SequenceBlending { get; set; } = true;
	[Property, Group( "Animation (Sequence)" )] public float RunPlaybackRate { get; set; } = 0.7f;
	[Property, Group( "Animation (Sequence)" )] public float AttackPlaybackRate { get; set; } = 1.0f;
	[Property, Group( "Animation (Sequence)" )] public float DeathPlaybackRate { get; set; } = 1.0f;

	[Property] public GameObject DeathEffectPrefab { get; set; }
	[Property] public SoundEvent HitSound { get; set; }
	[Property] public SoundEvent DeathSound { get; set; }

	// Outline
	[Property] public bool EnableOutline { get; set; } = true;
	[Property] public Color OutlineHighHP { get; set; } = Color.Green;
	[Property] public Color OutlineMediumHP { get; set; } = Color.Yellow;
	[Property] public Color OutlineLowHP { get; set; } = Color.Red;
	[Property] public float OutlineWidth { get; set; } = 2.0f;

	[Property] public float FadeOutTime { get; set; } = 2f;

	// Ground
	[Property, Group( "Ground" )] public bool LockZ { get; set; } = false;

	// =========================
	// INTERNAL
	// =========================
	private enum NPCState { Idle, Chasing, Attacking, Dead }
	private NPCState _state = NPCState.Idle;

	private HighlightOutline _outline;
	private float _fixedZ;

	private float _lastAttack = 999f;
	private float _playerSearch = 999f;
	private float _repathTimer = 999f;
	private float _offsetTimer = 999f;

	private float _attackTime = 0f;
	private bool _hitDone = false;

	private PlayerLevel _playerLevel;
	private PlayerStats _playerStats;

	private int _outlineBucket = -1;
	private Vector2 _targetOffset2D = Vector2.Zero;

	private float _stuckTimer = 0f;
	private Vector3 _lastStuckPos;

	// velocity for turn
	private Vector3 _lastPos;
	private Vector3 _smoothedVel;

	private static readonly Random Rng = new Random();

	// cache: чтобы не дергать лишний раз
	private string _currentSequence = "";
	private bool _sequenceLooping = true;
	private float _sequenceRate = 1f;

	private float WorldDelta => Time.Delta * TimeSlowSystem.WorldScale;

	protected override void OnStart()
	{
		if ( ModelRenderer == null )
			ModelRenderer = Components.GetInChildren<SkinnedModelRenderer>( true );

		if ( Agent == null )
			Agent = Components.Get<NavMeshAgent>();

		_outline = Components.Get<HighlightOutline>();

		Health = MaxHealth;
		_fixedZ = Transform.Position.z;

		_playerLevel = Scene.GetAllComponents<PlayerLevel>().FirstOrDefault();
		_playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();

		// Базы для бафов, если Receiver ещё не успел проставить
		if ( NpcBuff_BaseMoveSpeed <= 0f )
			NpcBuff_BaseMoveSpeed = MoveSpeed;

		if ( NpcBuff_BaseAttackDamage <= 0 )
			NpcBuff_BaseAttackDamage = AttackDamage;

		if ( _outline != null && EnableOutline )
		{
			_outline.Width = OutlineWidth;
			UpdateOutlineColor( force: true );
		}

		FindPlayer();
		_playerSearch = PlayerSearchInterval;

		_lastStuckPos = Transform.Position;
		_stuckTimer = 0f;

		_lastAttack = AttackCooldown;
		_repathTimer = RepathInterval;
		_offsetTimer = TargetOffsetRefresh;

		_lastPos = Transform.Position;
		_smoothedVel = Vector3.Zero;

		RefreshTargetOffset();

		// стартуем с бега
		PlaySequence( RunSequence, looping: true, playbackRate: RunPlaybackRate );

		// скорость агента под бафами
		TryApplyAgentSpeed();

		if ( DebugMode )
			Log.Info( $"🧟 Mummy Nav init. Agent={(Agent != null ? "OK" : "MISSING")} Renderer={(ModelRenderer != null ? "OK" : "MISSING")}" );
	}

	protected override void OnUpdate()
	{
		if ( _state == NPCState.Dead ) return;

		float dt = WorldDelta;

		_lastAttack += dt;
		_playerSearch += dt;
		_repathTimer += dt;
		_offsetTimer += dt;
		_stuckTimer += dt;

		UpdateVelocity( dt );

		// бафы могут обновляться глобально — поддержим скорость агента актуальной
		TryApplyAgentSpeed();

		if ( Health <= 0f )
		{
			Die();
			return;
		}

		if ( _outline != null && EnableOutline )
			UpdateOutlineColor( force: false );

		// player search (not every frame)
		if ( TargetPlayer == null || !TargetPlayer.IsValid )
		{
			if ( _playerSearch > PlayerSearchInterval )
			{
				_playerSearch = 0f;
				FindPlayer();
			}

			StopAgent();

			if ( _state != NPCState.Attacking )
				PlaySequence( RunSequence, looping: true, playbackRate: RunPlaybackRate );

			FixHeight();
			return;
		}

		// refresh offset
		if ( _offsetTimer > TargetOffsetRefresh )
		{
			_offsetTimer = 0f;
			RefreshTargetOffset();
		}

		UpdateStateMachine();

		if ( SeparationEnabled )
			ApplySeparation();

		if ( AntiStuckEnabled )
			UpdateAntiStuck();

		FixHeight();
	}

	private void UpdateVelocity( float dt )
	{
		var pos = Transform.Position;
		var rawVel = (pos - _lastPos) / MathF.Max( 0.0001f, dt );
		_lastPos = pos;

		float lerp = 1f - MathF.Pow( 0.001f, dt );
		_smoothedVel = Vector3.Lerp( _smoothedVel, rawVel, lerp );
	}

	private void UpdateStateMachine()
	{
		switch ( _state )
		{
			case NPCState.Idle:
			case NPCState.Chasing:
				UpdateChaseOrAttack();
				break;

			case NPCState.Attacking:
				UpdateAttack();
				break;
		}
	}

	private void UpdateChaseOrAttack()
	{
		Vector3 playerPos = TargetPlayer.Transform.Position;
		Vector3 npcPos = Transform.Position;

		float dist = Vector3.DistanceBetween( npcPos, playerPos );

		// attack
		if ( dist <= AttackRange )
		{
			_state = NPCState.Attacking;
			StopAgent();

			TurnTowards( (playerPos - npcPos).Normal );

			if ( _lastAttack > AttackCooldown )
				StartAttack();
			else
			{
				_attackTime = 0f;
				_hitDone = true;
				PlaySequence( RunSequence, looping: true, playbackRate: RunPlaybackRate );
			}

			return;
		}

		// chase
		_state = NPCState.Chasing;

		Vector3 desired = playerPos + new Vector3( _targetOffset2D.x, _targetOffset2D.y, 0f );

		if ( _repathTimer > RepathInterval )
		{
			_repathTimer = 0f;
			MoveAgentTo( desired );
		}

		TurnByVelocityOrToPlayer( playerPos );

		if ( dist <= StopDistance )
			StopAgent();

		PlaySequence( RunSequence, looping: true, playbackRate: RunPlaybackRate );
	}

	private void StartAttack()
	{
		_lastAttack = 0f;
		_attackTime = 0f;
		_hitDone = false;

		string seq = PickRandom( AttackSequences, fallback: "Zombie_Attack4" );
		PlaySequence( seq, looping: false, playbackRate: AttackPlaybackRate );
	}

	private void UpdateAttack()
	{
		if ( TargetPlayer == null || !TargetPlayer.IsValid )
		{
			_state = NPCState.Idle;
			return;
		}

		_attackTime += WorldDelta;

		Vector3 toPlayer = (TargetPlayer.Transform.Position - Transform.Position).Normal;
		TurnTowards( toPlayer );

		if ( !_hitDone && _attackTime >= HitDelay )
		{
			_hitDone = true;
			TryDealDamage();
		}

		if ( _attackTime >= AttackDuration )
		{
			_state = NPCState.Chasing;
			PlaySequence( RunSequence, looping: true, playbackRate: RunPlaybackRate );
		}
	}

	private void TryDealDamage()
	{
		if ( TargetPlayer == null || !TargetPlayer.IsValid ) return;

		float dist = Vector3.DistanceBetween( Transform.Position, TargetPlayer.Transform.Position );
		if ( dist > AttackRange ) return;

		var playerHealth = TargetPlayer.Components.Get<PlayerHealth>();
		if ( playerHealth != null )
			playerHealth.TakeDamage( EffectiveAttackDamage, GameObject );
	}

	// =========================
	// SEQUENCE ANIMATION (NO ANIMGRAPH)
	// =========================
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

	private static string PickRandom( string[] arr, string fallback )
	{
		if ( arr == null || arr.Length <= 0 ) return fallback;
		int i = Rng.Next( 0, arr.Length );
		return string.IsNullOrWhiteSpace( arr[i] ) ? fallback : arr[i];
	}

	// =========================
	// TURN FIXES
	// =========================
	private void TurnByVelocityOrToPlayer( Vector3 playerPos )
	{
		Vector3 npcPos = Transform.Position;

		if ( _smoothedVel.Length >= TurnVelocityMin )
		{
			var dir = _smoothedVel.Normal;
			TurnTowards( dir );
			return;
		}

		TurnTowards( (playerPos - npcPos).Normal );
	}

	private void TurnTowards( Vector3 dir )
	{
		if ( dir.Length < 0.01f ) return;

		var want = Rotation.LookAt( dir );

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

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}

	// =========================
	// NAV
	// =========================
	private void MoveAgentTo( Vector3 targetPosition )
	{
		if ( Agent == null || !Agent.IsValid )
		{
			Vector3 dir = (targetPosition - Transform.Position).Normal;
			Transform.Position += dir * EffectiveMoveSpeed * WorldDelta;
			return;
		}

		Agent.MoveTo( targetPosition );
	}

	private void StopAgent()
	{
		if ( Agent == null || !Agent.IsValid ) return;
		Agent.Stop();
	}

	private void RefreshTargetOffset()
	{
		_targetOffset2D = RandomVector2InCircle( TargetOffsetRadius );
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

	private void ApplySeparation()
	{
		if ( _state == NPCState.Attacking || _state == NPCState.Dead ) return;

		var all = Scene.GetAllComponents<MummyNPC>();
		if ( all == null ) return;

		Vector3 myPos = Transform.Position;
		Vector3 push = Vector3.Zero;
		int count = 0;

		foreach ( var other in all )
		{
			if ( other == null || !other.IsValid ) continue;
			if ( other == this ) continue;
			if ( other._state == NPCState.Dead ) continue;

			Vector3 d = myPos - other.Transform.Position;
			float dist = d.Length;

			if ( dist <= 0.001f || dist > SeparationRadius ) continue;

			float t = 1f - (dist / SeparationRadius);
			push += d.Normal * t;
			count++;
		}

		if ( count <= 0 ) return;

		push /= count;

		Vector3 step = push * SeparationStrength * WorldDelta * 0.02f;

		float len = step.Length;
		if ( len > SeparationMaxStep )
			step = step.Normal * SeparationMaxStep;

		Transform.Position += step;
	}

	private void UpdateAntiStuck()
	{
		if ( _state != NPCState.Chasing ) return;
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

	// =========================
	// OUTLINE
	// =========================
	private void UpdateOutlineColor( bool force )
	{
		if ( _outline == null ) return;

		float hpPct = (MaxHealth <= 0f) ? 0f : (Health / MaxHealth);

		int bucket;
		if ( hpPct > 0.75f ) bucket = 2;
		else if ( hpPct > 0.25f ) bucket = 1;
		else bucket = 0;

		if ( !force && bucket == _outlineBucket ) return;
		_outlineBucket = bucket;

		_outline.Color = bucket switch
		{
			2 => OutlineHighHP,
			1 => OutlineMediumHP,
			_ => OutlineLowHP
		};
	}

	// =========================
	// DAMAGE / DEATH
	// =========================
	public void TakeDamage( float damage )
	{
		if ( _state == NPCState.Dead ) return;

		if ( damage < 0f ) damage = 0f;
		damage *= SafeMult( NpcBuff_DamageTakenMult );

		Health -= damage;

		if ( HitSound != null )
			Sound.Play( HitSound, Transform.Position );

		if ( Health <= 0f )
			Die();
	}

	private void Die()
	{
		if ( _state == NPCState.Dead ) return;

		_state = NPCState.Dead;
		StopAgent();

		string deathSeq = PickRandom( DeathSequences, fallback: "Death_Headshot" );
		PlaySequence( deathSeq, looping: false, playbackRate: DeathPlaybackRate );

		if ( _playerLevel == null ) _playerLevel = Scene.GetAllComponents<PlayerLevel>().FirstOrDefault();
		if ( _playerStats == null ) _playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();

		if ( DeathSound != null )
			Sound.Play( DeathSound, Transform.Position );

		if ( DeathEffectPrefab != null )
		{
			var effect = DeathEffectPrefab.Clone( Transform.Position );
			effect.Name = "DeathEffect";
		}

		if ( _outline != null )
			_outline.Enabled = false;

		int expAmount = Rng.Next( 2, 6 ); // 2..5
		if ( _playerLevel != null )
			_playerLevel.AddExp( expAmount );

		if ( _playerStats != null )
		{
			_playerStats.AddKill();
			_playerStats.AddExp( expAmount );

			// 🪙 Мумия даёт 2 монеты
			_playerStats.AddCoins( 2 );
		}

		var collider = Components.Get<Collider>();
		if ( collider != null ) collider.Enabled = false;

		Enabled = false;
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

		GameObject.Destroy();
	}

	private void FixHeight()
	{
		if ( !LockZ ) return;

		if ( Math.Abs( Transform.Position.z - _fixedZ ) > 0.1f )
			Transform.Position = new Vector3( Transform.Position.x, Transform.Position.y, _fixedZ );
	}

	private void FindPlayer()
	{
		var player = Scene.GetAllComponents<Player>().FirstOrDefault();
		if ( player != null )
			TargetPlayer = player.GameObject;
	}

	// =========================
	// Helpers
	// =========================
	private void TryApplyAgentSpeed()
	{
		if ( Agent == null || !Agent.IsValid ) return;

		float spd = EffectiveMoveSpeed * TimeSlowSystem.WorldScale;

		try
		{
			Agent.MaxSpeed = spd;
		}
		catch
		{
			// если в твоём билде нет MaxSpeed — игнор
		}
	}

	private static float SafeMult( float v )
	{
		if ( float.IsNaN( v ) || float.IsInfinity( v ) ) return 1f;
		if ( v < 0.01f ) return 0.01f;
		if ( v > 1000f ) return 1000f;
		return v;
	}
}
