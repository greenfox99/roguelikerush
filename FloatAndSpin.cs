using Sandbox;
using System;

public sealed class FloatAndSpin : Component
{
	[Property] public float MinZ { get; set; } = 7.2f;
	[Property] public float FloatAmplitude { get; set; } = 10f;
	[Property] public float FloatSpeed { get; set; } = 1.5f;
	[Property] public float SpinSpeedDeg { get; set; } = 90f;

	// Включить/выключить случайную фазу
	[Property] public bool RandomPhase { get; set; } = true;

	private float _t;
	private float _phase;

	protected override void OnStart()
	{
		// Фаза в радианах: 0..2π
		_phase = RandomPhase ? Game.Random.Float( 0f, MathF.Tau ) : 0f;
	}

	protected override void OnUpdate()
	{
		_t += Time.Delta;

		// sin [-1..1] -> [0..1]
		float k = (MathF.Sin( (_t * FloatSpeed) + _phase ) + 1f) * 0.5f;
		float zOffset = k * FloatAmplitude;

		var pos = Transform.Position;
		pos.z = MinZ + zOffset;
		Transform.Position = pos;

		var yaw = Rotation.FromYaw( SpinSpeedDeg * Time.Delta );
		Transform.Rotation = yaw * Transform.Rotation;
	}
}
