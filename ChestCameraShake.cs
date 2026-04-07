using Sandbox;
using System;

public sealed class ChestCameraShake : Component
{
	[Property] public bool AffectPosition { get; set; } = true;
	[Property] public bool AffectRotation { get; set; } = true;

	private bool _playing;
	private float _startReal;
	private float _duration;
	private float _posAmplitude;
	private float _rotAmplitude;
	private float _frequency;

	private Vector3 _lastPosOffset;
	private Rotation _lastRotOffset;
	private bool _hasRotOffset;

	private float _seedX;
	private float _seedY;
	private float _seedZ;
	private float _seedR;

	public bool IsPlaying => _playing;

	public void Play( float duration = 8f, float posAmplitude = 7f, float rotAmplitude = 3.0f, float frequency = 18f )
	{
		ClearOffsets();

		_playing = true;
		_startReal = RealTime.Now;
		_duration = MathF.Max( 0.01f, duration );
		_posAmplitude = posAmplitude;
		_rotAmplitude = rotAmplitude;
		_frequency = MathF.Max( 0.1f, frequency );

		float seedBase = MathF.Abs( GameObject.Id.GetHashCode() ) * 0.01337f + RealTime.Now * 3.17f;
		_seedX = seedBase + 11.3f;
		_seedY = seedBase + 37.7f;
		_seedZ = seedBase + 73.1f;
		_seedR = seedBase + 101.9f;
	}

	public void Stop()
	{
		ClearOffsets();
		_playing = false;
	}

	protected override void OnUpdate()
	{
		// сначала снимаем смещение прошлого кадра
		ClearOffsets();

		if ( !_playing )
			return;

		float elapsed = RealTime.Now - _startReal;
		float t = elapsed / _duration;

		if ( t >= 1f )
		{
			_playing = false;
			return;
		}

		// Сильный старт, плавное затухание
		float envelope = MathF.Pow( 1f - t, 2.35f );
		float time = elapsed * _frequency;

		float nx = CompositeNoise( time, _seedX );
		float ny = CompositeNoise( time, _seedY );
		float nz = CompositeNoise( time, _seedZ );
		float nr = CompositeNoise( time, _seedR );

		Rotation baseRot = Transform.Rotation;

		if ( AffectPosition )
		{
			Vector3 localOffset = new Vector3(
				nx * _posAmplitude * envelope,
				ny * _posAmplitude * envelope,
				nz * _posAmplitude * 0.22f * envelope
			);

			_lastPosOffset =
				baseRot.Right * localOffset.x +
				baseRot.Up * localOffset.y +
				baseRot.Forward * localOffset.z;

			Transform.Position += _lastPosOffset;
		}

		if ( AffectRotation )
		{
			Angles ang = new Angles(
				ny * _rotAmplitude * envelope,
				nr * (_rotAmplitude * 0.65f) * envelope,
				nx * (_rotAmplitude * 1.15f) * envelope
			);

			_lastRotOffset = ang.ToRotation();
			_hasRotOffset = true;
			Transform.Rotation *= _lastRotOffset;
		}
	}

	protected override void OnDisabled()
	{
		Stop();
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		Stop();
		base.OnDestroy();
	}

	private void ClearOffsets()
	{
		if ( _lastPosOffset.Length > 0.0001f )
		{
			Transform.Position -= _lastPosOffset;
			_lastPosOffset = 0;
		}

		if ( _hasRotOffset )
		{
			Transform.Rotation *= _lastRotOffset.Inverse;
			_hasRotOffset = false;
		}
	}

	private static float CompositeNoise( float t, float seed )
	{
		float a = MathF.Sin( t * 1.10f + seed );
		float b = MathF.Sin( t * 2.37f + seed * 1.37f ) * 0.60f;
		float c = MathF.Sin( t * 4.11f + seed * 0.73f ) * 0.25f;
		return (a + b + c) / 1.85f;
	}
}
