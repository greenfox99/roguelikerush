using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class Car : Component
{
	// *** НАСТРОЙКИ ДВИЖЕНИЯ ***
	[Property] public float MotorForce { get; set; } = 8000f;
	[Property] public float ReverseForce { get; set; } = 6000f;
	[Property] public float MaxSpeed { get; set; } = 2000f;

	// *** НАСТРОЙКИ ПОВОРОТА ***
	[Property] public float TurnSpeedMin { get; set; } = 1.0f;
	[Property] public float TurnSpeedMax { get; set; } = 2.2f;
	[Property] public float OptimalSpeed { get; set; } = 500f;
	[Property] public float CurveSharpness { get; set; } = 2.2f;

	// *** ПЛАВНОСТЬ РУЛЯ И ИНЕРЦИЯ ***
	[Property] public float SteeringSmoothness { get; set; } = 15f;
	[Property] public float SteeringReturnSmoothness { get; set; } = 8f;
	[Property] public float TurnChangeSpeed { get; set; } = 50f;

	// *** НАСТРОЙКИ ФИЗИКИ ***
	[Property] public float Downforce { get; set; } = 5000f;
	[Property] public float Traction { get; set; } = 0.8f;

	// *** ЗВУКИ МАШИНЫ ***
	[Property] public SoundPointComponent WindSound { get; set; }
	[Property] public SoundPointComponent EngineIdleSound { get; set; }
	[Property] public SoundPointComponent EngineAccelSound { get; set; }
	[Property] public SoundPointComponent EngineDecelSound { get; set; }
	[Property] public SoundPointComponent TireSquealSound { get; set; }
	[Property] public SoundPointComponent TurboSound { get; set; }
	[Property] public SoundPointComponent CrashSound { get; set; }
	[Property] public SoundPointComponent SkidSound { get; set; }

	// *** НАСТРОЙКИ ЗВУКОВ ***
	[Property] public float WindThreshold { get; set; } = 200f;
	[Property] public float WindMinVolume { get; set; } = 0.2f;
	[Property] public float WindMaxVolume { get; set; } = 0.8f;
	[Property] public float EnginePitchMin { get; set; } = 0.8f;
	[Property] public float EnginePitchMax { get; set; } = 2.0f;
	[Property] public float TurboThreshold { get; set; } = 400f;
	[Property] public bool DebugSounds { get; set; } = false;

	public bool HasDriver { get; set; } = false;

	private float _throttle;
	private float _steerInput;
	private float _currentSteer;
	private float _currentTurnSpeed;
	private float _lastThrottle;
	private float _lastSpeed;
	private Rigidbody _rb;

	protected override void OnStart()
	{
		_rb = Components.Get<Rigidbody>();
		if ( _rb == null )
		{
			Log.Error( "Нет Rigidbody на машине!" );
			return;
		}

		_rb.LinearDamping = 0.2f;
		_rb.AngularDamping = 0.1f;
		_currentTurnSpeed = TurnSpeedMin;

		InitializeSounds();
	}

	private void InitializeSounds()
	{
		SoundPointComponent[] allSounds = { WindSound, EngineIdleSound, EngineAccelSound,
										   EngineDecelSound, TireSquealSound, TurboSound,
										   CrashSound, SkidSound };

		foreach ( var sound in allSounds )
		{
			if ( sound != null )
			{
				sound.Enabled = false;
				sound.Volume = 0f;
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( _rb == null ) return;

		float speedKmh = _rb.Velocity.Length * 0.1f;

		UpdateWindSound( speedKmh );
		UpdateEngineSounds( speedKmh );
		UpdateTireSounds( speedKmh );
		UpdateTurboSound( speedKmh );

		_lastThrottle = _throttle;
		_lastSpeed = speedKmh;
	}

	protected override void OnFixedUpdate()
	{
		if ( !HasDriver || _rb == null ) return;

		if ( _throttle != 0 )
		{
			float currentForce = _throttle > 0 ? MotorForce : ReverseForce;
			Vector3 force = Transform.World.Forward * _throttle * currentForce;
			_rb.ApplyForce( force );
		}

		UpdateSteering();
		ApplyPhysicsEffects();

		if ( _rb.Velocity.Length > MaxSpeed )
		{
			_rb.Velocity = _rb.Velocity.Normal * MaxSpeed;
		}
	}

	private void UpdateWindSound( float speedKmh )
	{
		if ( WindSound == null ) return;

		if ( HasDriver && speedKmh > WindThreshold )
		{
			float t = MathX.Clamp( (speedKmh - WindThreshold) / 600f, 0f, 1f );
			float targetVolume = MathX.Lerp( WindMinVolume, WindMaxVolume, t );

			WindSound.Enabled = true;
			WindSound.Volume = MathX.Lerp( WindSound.Volume, targetVolume, Time.Delta * 3f );
		}
		else
		{
			WindSound.Volume = MathX.Lerp( WindSound.Volume, 0f, Time.Delta * 2f );
			if ( WindSound.Volume < 0.01f )
				WindSound.Enabled = false;
		}
	}

	private void UpdateEngineSounds( float speedKmh )
	{
		if ( !HasDriver ) return;

		if ( EngineIdleSound != null )
		{
			if ( _throttle < 0.1f && speedKmh < 10f )
			{
				EngineIdleSound.Enabled = true;
				EngineIdleSound.Volume = MathX.Lerp( EngineIdleSound.Volume, 0.5f, Time.Delta * 2f );
			}
			else
			{
				EngineIdleSound.Volume = MathX.Lerp( EngineIdleSound.Volume, 0f, Time.Delta * 2f );
				if ( EngineIdleSound.Volume < 0.01f )
					EngineIdleSound.Enabled = false;
			}
		}

		if ( EngineAccelSound != null && _throttle > 0.1f )
		{
			float pitch = MathX.Lerp( EnginePitchMin, EnginePitchMax, speedKmh / MaxSpeed );
			float volume = MathX.Lerp( 0.3f, 1f, _throttle );

			EngineAccelSound.Enabled = true;
			EngineAccelSound.Volume = MathX.Lerp( EngineAccelSound.Volume, volume, Time.Delta * 3f );
			EngineAccelSound.Pitch = MathX.Lerp( EngineAccelSound.Pitch, pitch, Time.Delta * 2f );
		}
		else if ( EngineAccelSound != null )
		{
			EngineAccelSound.Volume = MathX.Lerp( EngineAccelSound.Volume, 0f, Time.Delta * 3f );
			if ( EngineAccelSound.Volume < 0.01f )
				EngineAccelSound.Enabled = false;
		}

		if ( EngineDecelSound != null )
		{
			if ( _lastThrottle > 0.5f && _throttle < 0.1f && speedKmh > 50f )
			{
				EngineDecelSound.Enabled = true;
				EngineDecelSound.Volume = MathX.Lerp( EngineDecelSound.Volume, 0.7f, Time.Delta * 5f );

				_ = WaitAndDisableSound( EngineDecelSound, 0.8f );
			}
		}
	}

	private void UpdateTireSounds( float speedKmh )
	{
		if ( !HasDriver ) return;

		if ( TireSquealSound != null )
		{
			float turnIntensity = Math.Abs( _steerInput ) * (speedKmh / 300f);
			if ( turnIntensity > 0.3f && speedKmh > 30f && _throttle > 0.1f )
			{
				TireSquealSound.Enabled = true;
				TireSquealSound.Volume = MathX.Lerp( TireSquealSound.Volume,
					MathX.Clamp( turnIntensity, 0.3f, 1f ), Time.Delta * 5f );
			}
			else
			{
				TireSquealSound.Volume = MathX.Lerp( TireSquealSound.Volume, 0f, Time.Delta * 3f );
				if ( TireSquealSound.Volume < 0.01f )
					TireSquealSound.Enabled = false;
			}
		}

		if ( SkidSound != null )
		{
			float driftIntensity = CalculateDriftIntensity();
			if ( driftIntensity > 0.2f && speedKmh > 50f )
			{
				SkidSound.Enabled = true;
				SkidSound.Volume = MathX.Lerp( SkidSound.Volume, driftIntensity, Time.Delta * 4f );
			}
			else
			{
				SkidSound.Volume = MathX.Lerp( SkidSound.Volume, 0f, Time.Delta * 3f );
				if ( SkidSound.Volume < 0.01f )
					SkidSound.Enabled = false;
			}
		}
	}

	private void UpdateTurboSound( float speedKmh )
	{
		if ( TurboSound == null || !HasDriver ) return;

		// Добавим отладку
		if ( DebugSounds && Time.Now % 1f < 0.1f )
		{
			Log.Info( $"Turbo check: speed={speedKmh:F0}, threshold={TurboThreshold}" );
		}

		// Убрал проверку на газ
		if ( speedKmh > TurboThreshold )
		{
			// Исправлено: делим на 400 (как в ветре), а не на 120
			float t = MathX.Clamp( (speedKmh - TurboThreshold) / 400f, 0f, 1f );
			float targetVolume = MathX.Lerp( 0.3f, 1f, t );
			float targetPitch = MathX.Lerp( 1f, 1.5f, t );

			TurboSound.Enabled = true;
			TurboSound.Volume = MathX.Lerp( TurboSound.Volume, targetVolume, Time.Delta * 3f );
			TurboSound.Pitch = MathX.Lerp( TurboSound.Pitch, targetPitch, Time.Delta * 2f );

			if ( DebugSounds && Time.Now % 1f < 0.1f )
			{
				Log.Info( $"🔊 Turbo ON: vol={TurboSound.Volume:F2}, speed={speedKmh:F0}" );
			}
		}
		else
		{
			TurboSound.Volume = MathX.Lerp( TurboSound.Volume, 0f, Time.Delta * 2f );
			if ( TurboSound.Volume < 0.01f )
			{
				TurboSound.Enabled = false;
			}
		}
	}

	private float CalculateDriftIntensity()
	{
		if ( _rb == null || _rb.Velocity.Length < 50f ) return 0;

		Vector3 velocityDir = _rb.Velocity.Normal;
		Vector3 carDir = Transform.World.Forward;

		float dot = Vector3.Dot( velocityDir, carDir );
		float angle = MathF.Acos( MathX.Clamp( dot, -1f, 1f ) );

		return MathX.Clamp( angle / 1.5f, 0f, 1f );
	}

	private async Task WaitAndDisableSound( SoundPointComponent sound, float delay )
	{
		await Task.DelaySeconds( delay );
		if ( sound != null && sound.Enabled )
		{
			sound.Enabled = false;
			sound.Volume = 0f;
		}
	}

	private void UpdateSteering()
	{
		float currentSpeed = _rb.Velocity.Length;
		float targetSteer = _steerInput;

		if ( Math.Abs( targetSteer ) < 0.01f && Math.Abs( _currentSteer ) > 0.01f )
		{
			_currentSteer = MathX.Lerp( _currentSteer, targetSteer, Time.Delta * SteeringReturnSmoothness );
		}
		else
		{
			_currentSteer = MathX.Lerp( _currentSteer, targetSteer, Time.Delta * SteeringSmoothness );
		}

		if ( Math.Abs( _currentSteer ) < 0.01f ) _currentSteer = 0;

		float targetTurnSpeed;

		if ( currentSpeed <= OptimalSpeed )
		{
			float t = currentSpeed / OptimalSpeed;
			t = (float)Math.Pow( t, 1.0f / CurveSharpness );
			targetTurnSpeed = MathX.Lerp( TurnSpeedMin, TurnSpeedMax, t );
		}
		else
		{
			float t = (currentSpeed - OptimalSpeed) / (MaxSpeed - OptimalSpeed);
			t = MathX.Clamp( t, 0, 1 );
			t = (float)Math.Pow( t, CurveSharpness );
			targetTurnSpeed = MathX.Lerp( TurnSpeedMax, TurnSpeedMin, t );
		}

		_currentTurnSpeed = MathX.Lerp( _currentTurnSpeed, targetTurnSpeed, Time.Delta * TurnChangeSpeed );

		if ( _currentSteer != 0 || Math.Abs( _currentSteer ) > 0.01f )
		{
			float turnAmount = -_currentSteer * _currentTurnSpeed;
			turnAmount = MathX.Clamp( turnAmount, -4f, 4f );

			// ===== ИСПРАВЛЕНО: Простой и надежный способ =====
			// Поворачиваем саму машину
			Transform.Rotation = Transform.Rotation * Rotation.FromYaw( turnAmount );

			// Синхронизируем физику
			if ( _rb != null )
			{
				_rb.Transform.Rotation = Transform.Rotation;
			}
		}
	}

	private void ApplyPhysicsEffects()
	{
		float currentSpeed = _rb.Velocity.Length;

		if ( currentSpeed > 100 )
		{
			float downforceMultiplier = MathX.Clamp( currentSpeed / MaxSpeed, 0, 1 );
			Vector3 downForce = -Transform.World.Up * Downforce * downforceMultiplier;
			_rb.ApplyForce( downForce );
		}

		if ( currentSpeed > 50 )
		{
			Vector3 rightVelocity = _rb.Velocity.Dot( Transform.World.Right ) * Transform.World.Right;
			_rb.Velocity -= rightVelocity * Traction * Time.Delta;
		}
	}

	public void SetInput( float throttle, float steer )
	{
		_throttle = throttle;
		_steerInput = steer;
	}

	public float GetSpeed()
	{
		if ( _rb == null ) return 0f;
		return _rb.Velocity.Length;
	}

}
