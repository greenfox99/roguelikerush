using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using YourGame.UI;

public enum SkillEffectType
{
	Dash,
	ExplosiveDash,
	BallLightning,
	TimeSlow,
	Armadillo
}

[Serializable]
public class SkillDefinition
{
	[Property] public string Id { get; set; } = "dash";
	[Property] public string Name { get; set; } = "Dash";
	[Property] public string Description { get; set; } = "Рывок вперёд";
	[Property] public string Icon { get; set; } = "⚡";
	[Property] public Texture IconTexture { get; set; }

	[Property] public int Price { get; set; } = 150;
	[Property] public float Cooldown { get; set; } = 6f;

	[Property] public SkillEffectType Effect { get; set; } = SkillEffectType.Dash;

	[Property] public float TeleportDistance { get; set; } = 220f;

	// ExplosiveDash
	[Property] public float ExplosionRadius { get; set; } = 0f;
	[Property] public float ExplosionDamage { get; set; } = 0f;
	[Property] public float KnockbackDistance { get; set; } = 0f;

	// Buff duration / temp modifiers
	[Property, Group( "Buff" )] public float BuffDuration { get; set; } = 7f;
	[Property, Group( "Buff" )] public int TempMaxTargets { get; set; } = 99;
	[Property, Group( "Buff" )] public float TempAttackCooldownMultiplier { get; set; } = 0.5f;

	// Armadillo
	[Property, Group( "Defense Buff" )] public float DamageReductionFraction { get; set; } = 0f;
	[Property, Group( "Defense Buff" )] public float ReflectIncomingDamageMultiplier { get; set; } = 0f;

	// SFX (optional)
	[Property] public SoundEvent StartSound { get; set; }
	[Property] public SoundEvent ExplosionSound { get; set; }
	[Property] public SoundEvent EndSound { get; set; }
}

public sealed class PlayerSkills : Component
{
	// action names
	[Property] public string Skill1Action { get; set; } = "Skill1"; // Q
	[Property] public string Skill2Action { get; set; } = "Skill2"; // R

	[Property] public PlayerStats PlayerStats { get; set; }
	[Property] public PlayerHealth PlayerHealthComponent { get; set; }

	// ссылка на посох
	[Property] public PlayerStaff PlayerStaff { get; set; }

	// FX PREFABS
	[Property, Group( "Explosive Dash FX" )] public GameObject TeleportOutFxPrefab { get; set; }
	[Property, Group( "Explosive Dash FX" )] public GameObject TeleportInFxPrefab { get; set; }
	[Property, Group( "Explosive Dash FX" )] public GameObject ExplosionFxPrefab { get; set; }

	// Screen FX
	[Property, Group( "Explosive Dash FX" )] public TeleportScreenFX ScreenFx { get; set; }

	// Safe teleport settings
	[Property, Group( "Safe Teleport" )] public float TeleportCheckRadius { get; set; } = 28f;
	[Property, Group( "Safe Teleport" )] public float SurfacePadding { get; set; } = 22f;
	[Property, Group( "Safe Teleport" )] public float BackStep { get; set; } = 18f;
	[Property, Group( "Safe Teleport" )] public float UpStep { get; set; } = 18f;
	[Property, Group( "Safe Teleport" )] public int MaxBackSteps { get; set; } = 18;
	[Property, Group( "Safe Teleport" )] public int MaxUpSteps { get; set; } = 10;
	[Property, Group( "Safe Teleport" )] public float ExtraUpOnTeleport { get; set; } = 6f;
	[Property, Group( "Safe Teleport" )] public float SnapDownDistance { get; set; } = 80f;

	// Damage filter
	[Property, Group( "Explosion" )] public string NpcTag { get; set; } = "npc";

	[Property]
	public List<SkillDefinition> Skills { get; set; } = new()
	{
		new SkillDefinition
		{
			Id="explosive_dash",
			Name="Взрывной рывок",
			Description="Взрыв радиусом 1000 → телепорт по направлению камеры на 1000",
			Icon="💣",
			Price=220,
			Cooldown=10f,
			Effect=SkillEffectType.ExplosiveDash,
			TeleportDistance=1000f,
			ExplosionRadius=1000f,
			ExplosionDamage=45f,
			KnockbackDistance=140f
		},

		new SkillDefinition
		{
			Id="dash",
			Name="Рывок",
			Description="Безопасный телепорт по камере",
			Icon="⚡",
			Price=120,
			Cooldown=5f,
			Effect=SkillEffectType.Dash,
			TeleportDistance=220f
		},

		new SkillDefinition
		{
			Id="ball_lightning",
			Name="Шаровая Молния",
			Description="Мощный урон по всем целям",
			Icon="🟠⚡",
			Price=350,
			Cooldown=22f,
			Effect=SkillEffectType.BallLightning,
			BuffDuration=7f,
			TempMaxTargets=99,
			TempAttackCooldownMultiplier=0.5f
		},

		new SkillDefinition
		{
			Id="time_slow",
			Name="Кристалл времени",
			Description="Замедляет время в 2 раза",
			Icon="🕒",
			Price=1,
			Cooldown=28f,
			Effect=SkillEffectType.TimeSlow,
			BuffDuration=6f
		},

		new SkillDefinition
		{
			Id="armadillo",
			Name="Броненосец",
			Description="поглощает 80% входящего урона и возвращает атакующим",
			Icon="🛡️",
			Price=535,
			Cooldown=85f,
			Effect=SkillEffectType.Armadillo,
			BuffDuration=12f,
			DamageReductionFraction=0.80f,
			ReflectIncomingDamageMultiplier=1.0f
		},
	};

	private readonly HashSet<string> _unlocked = new();

	private string _slot1Id = "";
	private string _slot2Id = "";

	private float _cd1 = 0f;
	private float _cd2 = 0f;

	public string Slot1Id => _slot1Id;
	public string Slot2Id => _slot2Id;

	public float Slot1CooldownLeft => _cd1;
	public float Slot2CooldownLeft => _cd2;

	// pending teleport
	private bool _pendingTeleport;
	private float _teleportAtReal;
	private Vector3 _pendingDest;
	private SkillDefinition _pendingDef;

	// Ball Lightning runtime state
	private bool _ballActive;
	private float _ballTimeLeft;
	private SkillDefinition _ballDef;

	private int _savedMaxTargets;
	private float _savedAttackCooldown;

	private int _ballAppliedTargets;
	private float _ballAppliedCooldownMult;

	// Time Slow runtime state
	private bool _timeSlowActive;
	private float _timeSlowTimeLeft;
	private SkillDefinition _timeSlowDef;

	public bool IsUnlocked( string id ) => !string.IsNullOrEmpty( id ) && _unlocked.Contains( id );

	public SkillDefinition GetDef( string id )
	{
		if ( string.IsNullOrEmpty( id ) ) return null;
		return Skills?.FirstOrDefault( s => s.Id == id );
	}

	protected override void OnStart()
	{
		GameLocalization.EnsureLoaded();
		ApplyLocalization();

		PlayerStats ??= Components.Get<PlayerStats>() ?? Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		PlayerHealthComponent ??= Components.Get<PlayerHealth>() ?? Scene.GetAllComponents<PlayerHealth>().FirstOrDefault();
		PlayerStaff ??= Components.Get<PlayerStaff>() ?? Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();
		ScreenFx ??= Scene.GetAllComponents<TeleportScreenFX>().FirstOrDefault();

		GameLocalization.Changed += ApplyLocalization;
	}

	protected override void OnDestroy()
	{
		GameLocalization.Changed -= ApplyLocalization;
		base.OnDestroy();
	}

	protected override void OnUpdate()
	{
		if ( _pendingTeleport && RealTime.Now >= _teleportAtReal )
		{
			_pendingTeleport = false;

			Transform.Position = _pendingDest;

			SpawnFxPrefab( TeleportInFxPrefab, Transform.Position, Rotation.Identity );

			if ( _pendingDef?.EndSound != null )
				Sound.Play( _pendingDef.EndSound, Transform.Position );

			_pendingDef = null;
		}

		if ( _ballActive )
		{
			_ballTimeLeft -= Time.Delta;
			if ( _ballTimeLeft <= 0f )
				EndBallLightning();
		}

		if ( _timeSlowActive )
		{
			_timeSlowTimeLeft -= Time.Delta;
			if ( _timeSlowTimeLeft <= 0f )
				EndTimeSlow();
		}

		if ( _cd1 > 0f ) _cd1 = MathF.Max( 0f, _cd1 - Time.Delta );
		if ( _cd2 > 0f ) _cd2 = MathF.Max( 0f, _cd2 - Time.Delta );

		if ( Scene.TimeScale <= 0.001f ) return;
		if ( _pendingTeleport ) return;

		if ( Input.Pressed( Skill1Action ) ) TryActivateSlot( 1 );
		if ( Input.Pressed( Skill2Action ) ) TryActivateSlot( 2 );
	}

	public bool TryBuySkill( string id )
	{
		var def = GetDef( id );
		if ( def == null ) return false;

		if ( IsUnlocked( id ) ) return true;

		if ( PlayerStats == null ) return false;
		if ( !PlayerStats.TrySpendCoins( def.Price ) ) return false;

		_unlocked.Add( id );

		if ( string.IsNullOrEmpty( _slot1Id ) ) _slot1Id = id;
		else if ( string.IsNullOrEmpty( _slot2Id ) ) _slot2Id = id;

		Log.Info( string.Format( T( "playerskills.log.buy", "✅ Куплен скилл: {0}", "✅ Skill purchased: {0}" ), def.Name ) );
		return true;
	}

	public bool TryEquipSkill( int slot, string id )
	{
		if ( slot != 1 && slot != 2 ) return false;
		if ( string.IsNullOrEmpty( id ) ) return false;
		if ( !IsUnlocked( id ) ) return false;

		if ( slot == 1 && _slot1Id == id ) return true;
		if ( slot == 2 && _slot2Id == id ) return true;

		if ( slot == 1 && _slot2Id == id )
		{
			(_slot1Id, _slot2Id) = (_slot2Id, _slot1Id);
			(_cd1, _cd2) = (_cd2, _cd1);
			return true;
		}

		if ( slot == 2 && _slot1Id == id )
		{
			(_slot1Id, _slot2Id) = (_slot2Id, _slot1Id);
			(_cd1, _cd2) = (_cd2, _cd1);
			return true;
		}

		if ( slot == 1 ) _slot1Id = id;
		else _slot2Id = id;

		return true;
	}

	private void TryActivateSlot( int slot )
	{
		if ( slot == 1 )
		{
			if ( _cd1 > 0f ) return;
			if ( string.IsNullOrEmpty( _slot1Id ) ) return;
			if ( !IsUnlocked( _slot1Id ) ) return;

			var def = GetDef( _slot1Id );
			if ( def == null ) return;

			ExecuteSkill( def );
			_cd1 = def.Cooldown;
		}
		else
		{
			if ( _cd2 > 0f ) return;
			if ( string.IsNullOrEmpty( _slot2Id ) ) return;
			if ( !IsUnlocked( _slot2Id ) ) return;

			var def = GetDef( _slot2Id );
			if ( def == null ) return;

			ExecuteSkill( def );
			_cd2 = def.Cooldown;
		}
	}

	private void ExecuteSkill( SkillDefinition def )
	{
		switch ( def.Effect )
		{
			case SkillEffectType.ExplosiveDash:
				DoExplosiveDash( def );
				break;

			case SkillEffectType.Dash:
				DoSafeTeleportByCameraDirection( def.TeleportDistance, null );
				break;

			case SkillEffectType.BallLightning:
				DoBallLightning( def );
				break;

			case SkillEffectType.TimeSlow:
				DoTimeSlow( def );
				break;

			case SkillEffectType.Armadillo:
				DoArmadillo( def );
				break;
		}
	}

	private void DoBallLightning( SkillDefinition def )
	{
		PlayerStaff ??= Components.Get<PlayerStaff>() ?? Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();
		if ( PlayerStaff == null )
		{
			Log.Warning( T( "playerskills.warn.ball_lightning_missing_staff", "⚠️ Шаровая Молния: PlayerStaff не найден", "⚠️ Ball Lightning: PlayerStaff not found" ) );
			return;
		}

		if ( def.StartSound != null )
			Sound.Play( def.StartSound, Transform.Position );

		if ( _ballActive )
		{
			_ballTimeLeft = MathF.Max( _ballTimeLeft, MathF.Max( 0.05f, def.BuffDuration ) );
			return;
		}

		_ballActive = true;
		_ballDef = def;
		_ballTimeLeft = MathF.Max( 0.05f, def.BuffDuration );

		_savedMaxTargets = PlayerStaff.MaxTargets;
		_savedAttackCooldown = PlayerStaff.AttackCooldown;

		_ballAppliedTargets = Math.Max( _savedMaxTargets, Math.Max( 1, def.TempMaxTargets ) );
		PlayerStaff.MaxTargets = _ballAppliedTargets;

		_ballAppliedCooldownMult = Clamp( def.TempAttackCooldownMultiplier, 0.01f, 10f );
		PlayerStaff.AttackCooldown = MathF.Max( 0.03f, _savedAttackCooldown * _ballAppliedCooldownMult );

		Log.Info( T( "playerskills.log.ball_lightning_on", "🟠⚡ Шаровая Молния активирована!", "🟠⚡ Ball Lightning activated!" ) );
	}

	private void EndBallLightning()
	{
		if ( !_ballActive ) return;

		_ballActive = false;
		_ballTimeLeft = 0f;

		PlayerStaff ??= Components.Get<PlayerStaff>() ?? Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();

		if ( PlayerStaff != null )
		{
			int currentTargets = PlayerStaff.MaxTargets;
			int extraTargets = Math.Max( 0, currentTargets - _ballAppliedTargets );
			PlayerStaff.MaxTargets = Math.Max( 1, _savedMaxTargets + extraTargets );

			float currentCd = PlayerStaff.AttackCooldown;
			float mult = MathF.Max( 0.01f, _ballAppliedCooldownMult );
			PlayerStaff.AttackCooldown = MathF.Max( 0.03f, currentCd / mult );
		}

		if ( _ballDef?.EndSound != null )
			Sound.Play( _ballDef.EndSound, Transform.Position );

		_ballDef = null;
	}

	private void DoTimeSlow( SkillDefinition def )
	{
		if ( def.StartSound != null )
			Sound.Play( def.StartSound, Transform.Position );

		if ( _timeSlowActive )
		{
			_timeSlowTimeLeft = MathF.Max( _timeSlowTimeLeft, MathF.Max( 0.05f, def.BuffDuration ) );
			return;
		}

		_timeSlowActive = true;
		_timeSlowDef = def;
		_timeSlowTimeLeft = MathF.Max( 0.05f, def.BuffDuration );

		TimeSlowSystem.IsActive = true;

		Log.Info( T( "playerskills.log.time_slow_on", "🕒 Замедление времени активировано!", "🕒 Time slow activated!" ) );
	}

	private void EndTimeSlow()
	{
		if ( !_timeSlowActive ) return;

		_timeSlowActive = false;
		_timeSlowTimeLeft = 0f;

		TimeSlowSystem.IsActive = false;

		if ( _timeSlowDef?.EndSound != null )
			Sound.Play( _timeSlowDef.EndSound, Transform.Position );

		_timeSlowDef = null;

		Log.Info( T( "playerskills.log.time_slow_off", "🕒 Замедление времени закончилось", "🕒 Time slow ended" ) );
	}

	private void DoArmadillo( SkillDefinition def )
	{
		PlayerHealthComponent ??= Components.Get<PlayerHealth>() ?? Scene.GetAllComponents<PlayerHealth>().FirstOrDefault();

		if ( PlayerHealthComponent == null )
		{
			Log.Warning( T( "playerskills.warn.armadillo_missing_health", "⚠️ Броненосец: PlayerHealth не найден", "⚠️ Armadillo: PlayerHealth not found" ) );
			return;
		}

		PlayerHealthComponent.ActivateArmadillo(
			def.BuffDuration,
			def.DamageReductionFraction,
			def.ReflectIncomingDamageMultiplier
		);

		Log.Info( T( "playerskills.log.armadillo_on", "🛡️ Броненосец активирован!", "🛡️ Armadillo activated!" ) );
	}

	private void DoExplosiveDash( SkillDefinition def )
	{
		var startPos = Transform.Position;

		ScreenFx?.Play();

		if ( def.StartSound != null )
			Sound.Play( def.StartSound, startPos );

		SpawnFxPrefab( TeleportOutFxPrefab, startPos, Rotation.Identity );
		SpawnFxPrefab( ExplosionFxPrefab, startPos, Rotation.Identity );

		if ( def.ExplosionSound != null )
			Sound.Play( def.ExplosionSound, startPos );

		ExplodeDamageAndKnockback_TagNpcOnly( startPos, def.ExplosionRadius, def.ExplosionDamage, def.KnockbackDistance );

		var dest = ComputeSafeDestinationByCamera( def.TeleportDistance );

		float closeTime = ScreenFx != null ? ScreenFx.CloseDuration : 0.08f;
		_pendingTeleport = true;
		_teleportAtReal = RealTime.Now + closeTime;
		_pendingDest = dest;
		_pendingDef = def;

		Log.Info( T( "playerskills.log.explosive_dash", "💣⚡ Взрывной рывок!", "💣⚡ Explosive Dash!" ) );
	}

	private Vector3 ComputeSafeDestinationByCamera( float maxDistance )
	{
		var dir = GetCameraForward();
		var origin = Transform.Position + Vector3.Up * 48f;

		var desired = origin + dir * maxDistance;

		var tr = Scene.Trace
			.Ray( origin, desired )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		Vector3 target = tr.Hit ? (tr.EndPosition + tr.Normal * SurfacePadding) : desired;

		target += Vector3.Up * ExtraUpOnTeleport;

		var safe = FindFreeSpotNear( target, dir );
		safe = TrySnapDown( safe );

		return safe;
	}

	private void DoSafeTeleportByCameraDirection( float maxDistance, SkillDefinition defForSfx )
	{
		ScreenFx?.Play();

		var dest = ComputeSafeDestinationByCamera( maxDistance );

		float closeTime = ScreenFx != null ? ScreenFx.CloseDuration : 0.08f;
		_pendingTeleport = true;
		_teleportAtReal = RealTime.Now + closeTime;
		_pendingDest = dest;
		_pendingDef = defForSfx;
	}

	private Vector3 GetCameraForward()
	{
		if ( Scene.Camera != null )
			return Scene.Camera.Transform.Rotation.Forward;

		return Transform.Rotation.Forward;
	}

	private Vector3 FindFreeSpotNear( Vector3 target, Vector3 dir )
	{
		for ( int b = 0; b <= MaxBackSteps; b++ )
		{
			float back = b * BackStep;

			for ( int u = 0; u <= MaxUpSteps; u++ )
			{
				float up = u * UpStep;

				var p = target - dir * back + Vector3.Up * up;

				if ( IsSpotFree( p ) )
					return p;
			}
		}

		return target + Vector3.Up * 64f;
	}

	private bool IsSpotFree( Vector3 pos )
	{
		var tr = Scene.Trace
			.Sphere( TeleportCheckRadius, pos, pos )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		return !tr.Hit;
	}

	private Vector3 TrySnapDown( Vector3 pos )
	{
		if ( SnapDownDistance <= 0f ) return pos;

		var from = pos + Vector3.Up * 24f;
		var to = pos - Vector3.Up * SnapDownDistance;

		var tr = Scene.Trace
			.Ray( from, to )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit )
			return tr.EndPosition + Vector3.Up * 2f;

		return pos;
	}

	private void ExplodeDamageAndKnockback_TagNpcOnly( Vector3 center, float radius, float damage, float knockbackDistance )
	{
		if ( radius <= 0f ) return;

		float r2 = radius * radius;
		var hit = new HashSet<GameObject>();

		foreach ( var col in Scene.GetAllComponents<Collider>() )
		{
			if ( col == null || col.GameObject == null ) continue;

			var go = col.GameObject;
			if ( go == GameObject ) continue;
			if ( !hit.Add( go ) ) continue;

			if ( !HasTagUpHierarchy( go, NpcTag ) )
				continue;

			var pos = go.Transform.Position;
			var delta = pos - center;

			if ( delta.LengthSquared > r2 )
				continue;

			var dmg = FindDamageableUpHierarchy( go );
			dmg?.TakeDamage( damage );

			if ( knockbackDistance > 0f && delta.Length > 0.01f )
				go.Transform.Position += delta.Normal * knockbackDistance;
		}
	}

	private bool HasTagUpHierarchy( GameObject go, string tag )
	{
		if ( string.IsNullOrWhiteSpace( tag ) ) return false;

		var cur = go;
		while ( cur != null )
		{
			if ( cur.Tags.Has( tag ) )
				return true;

			cur = cur.Parent;
		}

		return false;
	}

	private IMyDamageable FindDamageableUpHierarchy( GameObject go )
	{
		var cur = go;
		while ( cur != null )
		{
			var d = cur.Components.Get<IMyDamageable>();
			if ( d != null ) return d;
			cur = cur.Parent;
		}
		return null;
	}

	private void SpawnFxPrefab( GameObject prefab, Vector3 pos, Rotation rot )
	{
		if ( prefab == null ) return;

		var fx = prefab.Clone();
		fx.Transform.Position = pos;
		fx.Transform.Rotation = rot;
		fx.Enabled = true;
	}


	private string T( string key, string russianFallback, string englishFallback )
	{
		GameLocalization.EnsureLoaded();
		return GameLocalization.T( key, GameLocalization.IsLanguage( "ru" ) ? russianFallback : englishFallback );
	}

	private string ResolveSkillLocalizationKey( SkillDefinition skill )
	{
		if ( skill == null )
			return string.Empty;

		string id = (skill.Id ?? string.Empty).Trim().ToLowerInvariant();
		if ( id == "explosive_dash" || id == "dash" || id == "ball_lightning" || id == "time_slow" || id == "armadillo" )
			return id;

		switch ( skill.Effect )
		{
			case SkillEffectType.ExplosiveDash: return "explosive_dash";
			case SkillEffectType.Dash: return "dash";
			case SkillEffectType.BallLightning: return "ball_lightning";
			case SkillEffectType.TimeSlow: return "time_slow";
			case SkillEffectType.Armadillo: return "armadillo";
		}

		string name = (skill.Name ?? string.Empty).Trim().ToLowerInvariant();

		if ( name.Contains( "взрывной рывок" ) || name == "explosive dash" ) return "explosive_dash";
		if ( name == "рывок" || name == "dash" ) return "dash";
		if ( name.Contains( "шаровая молния" ) || name == "ball lightning" ) return "ball_lightning";
		if ( name.Contains( "кристалл времени" ) || name == "time crystal" ) return "time_slow";
		if ( name.Contains( "броненосец" ) || name == "armadillo" ) return "armadillo";

		return string.Empty;
	}

	private string GetLocalizedSkillName( SkillDefinition skill )
	{
		return ResolveSkillLocalizationKey( skill ) switch
		{
			"explosive_dash" => T( "skill.explosive_dash.name", "Взрывной рывок", "Explosive Dash" ),
			"dash" => T( "skill.dash.name", "Рывок", "Dash" ),
			"ball_lightning" => T( "skill.ball_lightning.name", "Шаровая Молния", "Ball Lightning" ),
			"time_slow" => T( "skill.time_slow.name", "Кристалл времени", "Time Crystal" ),
			"armadillo" => T( "skill.armadillo.name", "Броненосец", "Armadillo" ),
			_ => skill?.Name ?? string.Empty
		};
	}

	private string GetLocalizedSkillDescription( SkillDefinition skill )
	{
		return ResolveSkillLocalizationKey( skill ) switch
		{
			"explosive_dash" => T( "skill.explosive_dash.desc", "Взрыв радиусом 1000 -> телепорт по направлению камеры на 1000", "Explosion radius 1000 -> teleport 1000 in camera direction" ),
			"dash" => T( "skill.dash.desc", "Безопасный телепорт по камере", "Safe camera-direction teleport" ),
			"ball_lightning" => T( "skill.ball_lightning.desc", "Мощный урон по всем целям", "Heavy damage to all targets" ),
			"time_slow" => T( "skill.time_slow.desc", "Замедляет время в 2 раза", "Slows time by 2x" ),
			"armadillo" => T( "skill.armadillo.desc", "Поглощает 80% входящего урона и возвращает атакующим", "Absorbs 80% incoming damage and reflects it back to attackers" ),
			_ => skill?.Description ?? string.Empty
		};
	}

	private void ApplyLocalization()
	{
		if ( Skills == null || Skills.Count == 0 )
			return;

		foreach ( var skill in Skills )
		{
			if ( skill == null )
				continue;

			// Не трогаем настройки эффекта, цены, кулдауны и прочие параметры компонента.
			// Обновляем только отображаемые Name/Description во время игры.
			skill.Name = GetLocalizedSkillName( skill );
			skill.Description = GetLocalizedSkillDescription( skill );
		}
	}

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}
}
