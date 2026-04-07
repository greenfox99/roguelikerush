using Sandbox;
using System;

public sealed class StatueDebris : Component
{
	[Property] public Vector3 Velocity { get; set; }
	[Property] public Vector3 AngularVelocity { get; set; }

	[Property] public float Gravity { get; set; } = 1200f;
	[Property] public float Lifetime { get; set; } = 4.0f;

	[Property] public float AirDamping { get; set; } = 0.995f;
	[Property] public float GroundDamping { get; set; } = 0.90f;
	[Property] public float AngularDamping { get; set; } = 0.985f;

	[Property] public float BounceFactor { get; set; } = 0.38f;
	[Property] public int MaxBounces { get; set; } = 2;

	[Property] public float GroundCheckPadding { get; set; } = 2f;
	[Property] public float MinBounceSpeed { get; set; } = 80f;

	private float _life;
	private int _bounceCount;
	private Rotation _rotation;
	private bool _grounded;

	protected override void OnStart()
	{
		_life = 0f;
		_bounceCount = 0;
		_rotation = Transform.Rotation;
		_grounded = false;
	}

	protected override void OnUpdate()
	{
		float dt = Time.Delta;
		_life += dt;

		Vector3 startPos = Transform.Position;

		// гравитация
		if ( !_grounded )
			Velocity += Vector3.Down * Gravity * dt;

		Vector3 endPos = startPos + Velocity * dt;

		// trace по траектории движения
		var tr = Scene.Trace
			.Ray( startPos, endPos )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit )
		{
			Transform.Position = tr.EndPosition + tr.Normal * GroundCheckPadding;

			// отражаем скорость от поверхности
			Vector3 normal = tr.Normal;
			float vn = Velocity.Dot( normal );
			Vector3 reflected = Velocity - (2f * vn * normal);

			// ослабляем отскок
			Velocity = reflected * BounceFactor;

			_bounceCount++;

			// если отскок слабый или лимит отскоков вышел — считаем, что камень лёг
			if ( _bounceCount >= MaxBounces || Velocity.Length < MinBounceSpeed )
			{
				_grounded = true;
				Velocity = new Vector3( Velocity.x * 0.35f, Velocity.y * 0.35f, 0f );
				AngularVelocity *= 0.65f;
			}
		}
		else
		{
			Transform.Position = endPos;
		}

		// скольжение по земле после приземления
		if ( _grounded )
		{
			Velocity = new Vector3( Velocity.x * GroundDamping, Velocity.y * GroundDamping, 0f );

			if ( Velocity.Length < 8f )
				Velocity = Vector3.Zero;
		}
		else
		{
			Velocity *= AirDamping;
		}

		// вращение
		var pitch = Rotation.FromPitch( AngularVelocity.x * dt );
		var yaw = Rotation.FromYaw( AngularVelocity.y * dt );
		var roll = Rotation.FromRoll( AngularVelocity.z * dt );

		_rotation *= pitch * yaw * roll;
		Transform.Rotation = _rotation;

		AngularVelocity *= AngularDamping;

		if ( _life >= Lifetime )
			GameObject.Destroy();
	}
}
