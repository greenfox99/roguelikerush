using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public sealed class LootChest : Component
{
	[Property] public SoundEvent PickupSound { get; set; }
	[Property] public GameObject PickupEffect { get; set; }
	[Property] public float PickupRange { get; set; } = 150f;
	[Property] public bool DebugMode { get; set; } = true;

	private bool _isCollected = false;

	protected override void OnStart()
	{
		Log.Info( $"📦 Сундук создан на {Transform.Position}" );
	}

	protected override void OnUpdate()
	{
		if ( _isCollected ) return;

		var player = Scene.GetAllComponents<Player>().FirstOrDefault();
		if ( player == null ) return;

		float dist = Vector3.DistanceBetween( Transform.Position, player.Transform.Position );

		if ( DebugMode && Time.Now % 2f < 0.1f )
		{
			string status = dist <= PickupRange ? "🟢 МОЖНО ПОДОБРАТЬ" : "🔴 ДАЛЕКО";
			Log.Info( $"📦 Сундук: дист={dist:F0}, {status}" );
			DebugOverlay.Sphere( new Sphere( Transform.Position, PickupRange ), Color.Yellow.WithAlpha( 0.3f ), 0f );
		}

		if ( dist <= PickupRange && Input.Pressed( "Use" ) )
		{
			Collect();
		}
	}

	private void Collect()
	{
		if ( _isCollected ) return;
		_isCollected = true;

		Log.Info( $"🎁 Сундук подобран!" );

		// Звук подбора
		if ( PickupSound != null )
		{
			Sound.Play( PickupSound, Transform.Position );
		}

		// Эффект подбора
		if ( PickupEffect != null )
		{
			var effect = PickupEffect.Clone( Transform.Position );
			_ = DestroyAfterDelay( effect, 2f );
		}

		// ДАЕМ ОПЫТ ДЛЯ ПОВЫШЕНИЯ УРОВНЯ
		GiveExpToLevelUp();

		// Удаляем сундук
		GameObject.Destroy();
	}

	private void GiveExpToLevelUp()
	{
		var playerLevel = Scene.GetAllComponents<PlayerLevel>().FirstOrDefault();

		if ( playerLevel != null )
		{
			// Получаем сколько опыта нужно до следующего уровня
			int expNeeded = playerLevel.GetExpNeededForNextLevel() - playerLevel.CurrentExp;

			// Если нужно больше 0 - даем ровно столько, чтобы поднялся уровень
			if ( expNeeded > 0 )
			{
				playerLevel.AddExp( expNeeded );
				Log.Info( $"✨ Сундук дал {expNeeded} опыта - повышение уровня!" );
			}
			else
			{
				// Если уже есть следующий уровень - даем 1 опыт
				playerLevel.AddExp( 1 );
				Log.Info( $"✨ Сундук дал 1 опыт" );
			}
		}
		else
		{
			Log.Error( "❌ PlayerLevel не найден!" );
		}
	}

	private async Task DestroyAfterDelay( GameObject obj, float delay )
	{
		await Task.DelaySeconds( delay );
		if ( obj != null && obj.IsValid )
		{
			obj.Destroy();
		}
	}
}
