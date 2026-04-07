using Sandbox;
using System;
using System.Linq;
using YourGame.UI;

public sealed class PlayerHealth : Component
{
	public static PlayerHealth Instance { get; private set; }

	[Property] public float MaxHealth { get; set; } = 100f;
	[Property] public float MaxStamina { get; set; } = 100f;
	[Property] public float StaminaRegenRate { get; set; } = 20f;

	[Property] public float HealthRegenAmount { get; set; } = 0f;
	[Property] public float HealthRegenInterval { get; set; } = 4f;

	[Property] public float StaminaDrainRate { get; set; } = 30f;
	[Property] public float RunSpeedMultiplier { get; set; } = 1.5f;

	// =========================
	// 🛡️ SHIELD UPGRADE
	// =========================
	[Property] public float ShieldPerLevel { get; set; } = 5f;
	[Property] public float ShieldRegenDelay { get; set; } = 5f;
	[Property] public float ShieldRegenPerSecond { get; set; } = 1f;
	[Property] public float ShieldRegenDelayReducePerLevel { get; set; } = 0.3f;
	[Property] public float ShieldRegenRateAddPerLevel { get; set; } = 0.3f;

	public int ShieldLevel { get; private set; } = 0;
	public float MaxShield => ShieldLevel * ShieldPerLevel;
	public float CurrentShield { get; private set; } = 0f;

	public float ShieldRegenCooldownLeft => MathF.Max( 0f, _shieldRegenDelayTimer );
	public float ShieldRegenRateNow => GetShieldRegenRate();
	public float ShieldRegenDelayNow => GetShieldRegenDelay();

	[Property] public SoundEvent PlayerHitSound { get; set; }
	[Property] public SoundEvent PlayerDeathSound { get; set; }

	public float CurrentHealth { get; private set; }
	public float CurrentStamina { get; private set; }
	public bool IsRunning { get; private set; }
	public bool IsDead { get; private set; }
	public bool IsGodMode { get; private set; }
	public bool IsTemporarilyInvulnerable => !IsDead && RealTime.Now < _temporaryInvulnerableUntilReal;
	public float TemporaryInvulnerabilitySecondsLeft => MathF.Max( 0f, _temporaryInvulnerableUntilReal - RealTime.Now );

	public bool IsArmadilloActive => _armadillo != null && _armadillo.IsValid && _armadillo.IsActive;

	public event Action<float> OnHealthDamageTaken;

	private float _originalSpeed;
	private TimeSince _lastRegen;
	private float _shieldRegenDelayTimer = 0f;
	private float _temporaryInvulnerableUntilReal = 0f;
	private ArmadilloSkillController _armadillo;

	protected override void OnStart()
	{
		Instance = this;
		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;
		_originalSpeed = 200f;

		_lastRegen = 0;
		HealthRegenAmount = 0f;

		CurrentShield = 0f;
		_shieldRegenDelayTimer = 0f;
		IsDead = false;
		IsGodMode = false;

		_armadillo = FindArmadilloController();
	}

	protected override void OnDisabled()
	{
		if ( Instance == this )
			Instance = null;
	}

	[ConCmd( "greenfox_godmode_on", ConVarFlags.Server )]
	public static void GodModeOnCommand()
	{
		if ( Instance == null )
		{
			Log.Warning( "PlayerHealth instance not found for command greenfox_godmode_on" );
			return;
		}

		Instance.SetGodMode( true );
	}

	[ConCmd( "greenfox_godmode_off", ConVarFlags.Server )]
	public static void GodModeOffCommand()
	{
		if ( Instance == null )
		{
			Log.Warning( "PlayerHealth instance not found for command greenfox_godmode_off" );
			return;
		}

		Instance.SetGodMode( false );
	}

	public void GrantTemporaryInvulnerability( float durationSeconds )
	{
		if ( durationSeconds <= 0f )
			return;

		_temporaryInvulnerableUntilReal = MathF.Max( _temporaryInvulnerableUntilReal, RealTime.Now + durationSeconds );
	}

	public void ClearTemporaryInvulnerability()
	{
		_temporaryInvulnerableUntilReal = 0f;
	}

	public void SetGodMode( bool enabled )
	{
		IsGodMode = enabled;

		if ( enabled )
		{
			IsDead = false;
			CurrentHealth = MaxHealth;
			CurrentShield = MaxShield;
			Log.Info( "🛡️ God Mode включён" );
		}
		else
		{
			Log.Info( "🛡️ God Mode выключен" );
		}
	}

	protected override void OnUpdate()
	{
		if ( IsDead )
			return;

		IsRunning = Input.Down( "Run" ) && CurrentStamina > 0;

		if ( IsRunning )
			CurrentStamina = Math.Max( 0, CurrentStamina - StaminaDrainRate * Time.Delta );
		else
			CurrentStamina = Math.Min( MaxStamina, CurrentStamina + StaminaRegenRate * Time.Delta );

		if ( HealthRegenAmount > 0 && CurrentHealth < MaxHealth && _lastRegen > HealthRegenInterval )
		{
			CurrentHealth = Math.Min( MaxHealth, CurrentHealth + HealthRegenAmount );
			_lastRegen = 0;
			Log.Info( $"❤️ Регенерация +{HealthRegenAmount} HP" );
		}

		UpdateShieldRegen();
	}

	private float GetShieldRegenDelay()
	{
		float d = ShieldRegenDelay - ShieldLevel * ShieldRegenDelayReducePerLevel;
		return MathF.Max( 0.25f, d );
	}

	private float GetShieldRegenRate()
	{
		float r = ShieldRegenPerSecond + ShieldLevel * ShieldRegenRateAddPerLevel;
		return MathF.Max( 0f, r );
	}

	private void ResetShieldRegenCooldown()
	{
		if ( ShieldLevel <= 0 ) return;
		_shieldRegenDelayTimer = GetShieldRegenDelay();
	}

	private void UpdateShieldRegen()
	{
		if ( ShieldLevel <= 0 ) return;
		if ( MaxShield <= 0f ) return;

		if ( CurrentShield >= MaxShield )
			return;

		if ( _shieldRegenDelayTimer > 0f )
		{
			_shieldRegenDelayTimer -= Time.Delta;
			return;
		}

		float rate = GetShieldRegenRate();
		if ( rate <= 0f ) return;

		CurrentShield = Math.Min( MaxShield, CurrentShield + rate * Time.Delta );
	}

	public void AddShieldLevel()
	{
		ShieldLevel++;
		CurrentShield = MaxShield;
		_shieldRegenDelayTimer = 0f;

		Log.Info( $"🛡️ Щит улучшен: lvl {ShieldLevel}, {CurrentShield}/{MaxShield} | delay={GetShieldRegenDelay():F2}s | regen={GetShieldRegenRate():F2}/s" );
	}

	public void RemoveShieldLevel()
	{
		ShieldLevel = Math.Max( 0, ShieldLevel - 1 );

		if ( ShieldLevel <= 0 )
		{
			CurrentShield = 0f;
			_shieldRegenDelayTimer = 0f;
		}
		else
		{
			CurrentShield = Math.Min( CurrentShield, MaxShield );
			ResetShieldRegenCooldown();
		}

		Log.Info( $"🛡️ Щит уменьшен: lvl {ShieldLevel}, {CurrentShield}/{MaxShield}" );
	}

	private ArmadilloSkillController FindArmadilloController()
	{
		return Components.Get<ArmadilloSkillController>()
			?? Components.GetInParent<ArmadilloSkillController>()
			?? Components.GetInChildren<ArmadilloSkillController>( true )
			?? Scene.GetAllComponents<ArmadilloSkillController>().FirstOrDefault();
	}

	public void ActivateArmadillo( float duration, float damageReductionFraction = 0.80f, float reflectIncomingDamageMultiplier = 1.0f )
	{
		_armadillo ??= FindArmadilloController();

		if ( _armadillo == null || !_armadillo.IsValid )
		{
			Log.Warning( "ArmadilloSkillController not found on player." );
			return;
		}

		_armadillo.Activate( duration, damageReductionFraction, reflectIncomingDamageMultiplier );
	}

	public void TakeDamage( float damage, GameObject attacker = null )
	{
		if ( IsDead )
			return;

		if ( IsGodMode )
		{
			Log.Info( $"🛡️ Урон {damage} заблокирован God Mode" );
			return;
		}

		if ( IsTemporarilyInvulnerable )
		{
			Log.Info( $"🛡️ Урон {damage} заблокирован временной неуязвимостью" );
			return;
		}

		if ( damage <= 0 )
			return;

		float incoming = damage;

		_armadillo ??= FindArmadilloController();

		if ( _armadillo != null && _armadillo.IsValid && _armadillo.IsActive )
		{
			float originalIncoming = incoming;
			incoming = _armadillo.ModifyIncomingDamage( incoming, attacker );

			Log.Info( $"🛡️ Броненосец: входящий урон {originalIncoming} -> {incoming}" );
		}

		if ( ShieldLevel > 0 && CurrentShield > 0f )
		{
			float absorbed = Math.Min( CurrentShield, incoming );
			CurrentShield -= absorbed;
			incoming -= absorbed;

			ResetShieldRegenCooldown();

			Log.Info( $"🛡️ Щит принял урон: {absorbed}, Shield: {CurrentShield}/{MaxShield}" );
		}

		if ( incoming > 0f )
		{
			CurrentHealth -= incoming;
			CurrentHealth = Math.Max( 0, CurrentHealth );

			ResetShieldRegenCooldown();
			OnHealthDamageTaken?.Invoke( incoming );

			Log.Info( $"💔 Игрок получил урон: {incoming}, HP: {CurrentHealth}/{MaxHealth}" );
		}

		if ( PlayerHitSound != null )
			Sound.Play( PlayerHitSound, Transform.Position );

		if ( CurrentHealth <= 0 )
			Die();
	}

	private void Die()
	{
		if ( IsDead )
			return;

		IsDead = true;
		CurrentHealth = 0f;
		IsRunning = false;

		Log.Info( "💀 Игрок умер" );
		Sound.StopAll( 0f );

		if ( PlayerDeathSound != null )
			Sound.Play( PlayerDeathSound, Transform.Position );

		var deathScreen = Scene.GetAllComponents<DeathScreenMenu>().FirstOrDefault();
		if ( deathScreen != null )
		{
			deathScreen.OpenDeathScreen();
		}
		else
		{
			Log.Warning( "DeathScreenMenu not found in scene." );
		}
	}
}
