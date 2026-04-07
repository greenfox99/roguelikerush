using Sandbox;
using System;
using System.Linq;

public sealed class StatueRewardCoin : Component
{
	[Property] public int CoinValue { get; set; } = 1;
	[Property] public Vector3 Velocity { get; set; }
	[Property] public Vector3 AngularVelocity { get; set; }

	[Property] public float Gravity { get; set; } = 1100f;
	[Property] public float Lifetime { get; set; } = 10f;
	[Property] public float PickupDelay { get; set; } = 0.35f;
	[Property] public float PickupRadius { get; set; } = 70f;

	[Property] public float AirDamping { get; set; } = 0.995f;
	[Property] public float GroundDamping { get; set; } = 0.88f;
	[Property] public float AngularDamping { get; set; } = 0.985f;

	[Property] public float BounceFactor { get; set; } = 0.30f;
	[Property] public int MaxBounces { get; set; } = 2;
	[Property] public float MinBounceSpeed { get; set; } = 70f;
	[Property] public float GroundOffset { get; set; } = 2f;

	private float _life;
	private int _bounceCount;
	private bool _grounded;
	private Rotation _rotation;

	private Player _player;
	private PlayerStats _playerStats;

	protected override void OnStart()
	{
		_life = 0f;
		_bounceCount = 0;
		_grounded = false;
		_rotation = Transform.Rotation;

		_player = Scene.GetAllComponents<Player>().FirstOrDefault();
		_playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		float dt = Time.Delta;
		_life += dt;

		if ( _player == null || !_player.IsValid )
			_player = Scene.GetAllComponents<Player>().FirstOrDefault();

		if ( _playerStats == null || !_playerStats.IsValid )
			_playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();

		UpdateMovement( dt );
		UpdatePickup();

		if ( _life >= Lifetime )
			GameObject.Destroy();
	}

	private void UpdateMovement( float dt )
	{
		Vector3 startPos = Transform.Position;

		if ( !_grounded )
			Velocity += Vector3.Down * Gravity * dt;

		Vector3 endPos = startPos + Velocity * dt;

		var tr = Scene.Trace
			.Ray( startPos, endPos )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit )
		{
			Transform.Position = tr.EndPosition + tr.Normal * GroundOffset;

			Vector3 normal = tr.Normal;
			float vn = Velocity.Dot( normal );
			Vector3 reflected = Velocity - (2f * vn * normal);

			Velocity = reflected * BounceFactor;
			_bounceCount++;

			if ( _bounceCount >= MaxBounces || Velocity.Length < MinBounceSpeed )
			{
				_grounded = true;
				Velocity = new Vector3( Velocity.x * 0.35f, Velocity.y * 0.35f, 0f );
				AngularVelocity *= 0.60f;
			}
		}
		else
		{
			Transform.Position = endPos;
		}

		if ( _grounded )
		{
			Velocity = new Vector3( Velocity.x * GroundDamping, Velocity.y * GroundDamping, 0f );
			if ( Velocity.Length < 6f )
				Velocity = Vector3.Zero;
		}
		else
		{
			Velocity *= AirDamping;
		}

		var pitch = Rotation.FromPitch( AngularVelocity.x * dt );
		var yaw = Rotation.FromYaw( AngularVelocity.y * dt );
		var roll = Rotation.FromRoll( AngularVelocity.z * dt );

		_rotation *= pitch * yaw * roll;
		Transform.Rotation = _rotation;

		AngularVelocity *= AngularDamping;
	}

	private void UpdatePickup()
	{
		if ( _life < PickupDelay ) return;
		if ( _player == null || !_player.IsValid ) return;
		if ( _playerStats == null || !_playerStats.IsValid ) return;

		float dist = Vector3.DistanceBetween( Transform.Position, _player.Transform.Position );
		if ( dist > PickupRadius ) return;

		_playerStats.AddCoins( CoinValue );
		GameObject.Destroy();
	}
}
