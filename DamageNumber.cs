using Sandbox;
using System;
using System.Linq;

public sealed class DamageNumber : Component
{
	[Property] public Color TextColor { get; set; } = Color.Red;
	[Property] public float TextSize { get; set; } = 24f;
	[Property] public float FloatSpeed { get; set; } = 50f;
	[Property] public float Lifetime { get; set; } = 1.5f;

	private float _timeAlive = 0;
	private string _damageText;
	private Vector3 _worldPos;

	protected override void OnStart()
	{
		_worldPos = Transform.Position;
	}

	public void SetDamage( int damage )
	{
		_damageText = $"-{damage}";
	}

	protected override void OnUpdate()
	{
		_timeAlive += Time.Delta;

		// Поднимаем вверх
		_worldPos += Vector3.Up * FloatSpeed * Time.Delta;

		// Просто логируем для проверки
		if ( _timeAlive % 0.5f < 0.1f )
		{
			Log.Info( $"💬 {_damageText} at {_worldPos}" );
		}

		// Уничтожаем после времени жизни
		if ( _timeAlive >= Lifetime )
		{
			GameObject.Destroy();
		}
	}
}
