using Sandbox;
using System;

public sealed class LootDropper : Component
{
	// prefabs (твои модельки exp/coin)
	[Property, Group( "Prefabs" )] public GameObject ExpPickupPrefab { get; set; }
	[Property, Group( "Prefabs" )] public GameObject CoinPickupPrefab { get; set; }

	// сколько выпадет (рандом)
	[Property, Group( "Amount" )] public int ExpMin { get; set; } = 1;
	[Property, Group( "Amount" )] public int ExpMax { get; set; } = 3;
	[Property, Group( "Amount" )] public int CoinsMin { get; set; } = 0;
	[Property, Group( "Amount" )] public int CoinsMax { get; set; } = 1;

	// как “дробить” на отдельные модельки (1 = каждая монетка отдельной моделью)
	[Property, Group( "Amount" )] public int ExpPerPickup { get; set; } = 1;
	[Property, Group( "Amount" )] public int CoinsPerPickup { get; set; } = 1;

	// где спавнить
	[Property, Group( "Spawn" )] public float ScatterRadius { get; set; } = 80f;
	[Property, Group( "Spawn" )] public float SpawnHeight { get; set; } = 18f;
	[Property, Group( "Spawn" )] public float GroundTraceUp { get; set; } = 120f;
	[Property, Group( "Spawn" )] public float GroundTraceDown { get; set; } = 220f;

	private static readonly Random Rng = new Random();

	public void Drop( Vector3 origin )
	{
		int exp = Roll( ExpMin, ExpMax );
		int coins = Roll( CoinsMin, CoinsMax );

		SpawnMany( ExpPickupPrefab, LootType.Exp, exp, ExpPerPickup, origin );
		SpawnMany( CoinPickupPrefab, LootType.Coins, coins, CoinsPerPickup, origin );
	}

	private int Roll( int min, int max )
	{
		// нормализуем диапазон
		if ( max < min ) (min, max) = (max, min);
		min = Math.Max( 0, min );
		max = Math.Max( 0, max );

		if ( max == min ) return min;
		return Rng.Next( min, max + 1 ); // inclusive
	}

	private void SpawnMany( GameObject prefab, LootType type, int total, int perPickup, Vector3 origin )
	{
		if ( prefab == null ) return;
		if ( total <= 0 ) return;

		perPickup = Math.Max( 1, perPickup );

		int left = total;

		while ( left > 0 )
		{
			int amt = Math.Min( perPickup, left );
			left -= amt;

			Vector3 pos = GetScatterPosOnGround( origin );

			var go = prefab.Clone( pos );
			// ✅ монету ставим "на ребро" + случайный разворот вокруг вертикали
			if ( type == LootType.Coins )
			{
				float yaw = (float)(Rng.NextDouble() * 360.0);
				go.Transform.Rotation = Rotation.FromPitch( 90f ) * Rotation.FromYaw( yaw );
			}
			else
			{
				// EXP пусть просто крутится по yaw (по желанию)
				float yaw = (float)(Rng.NextDouble() * 360.0);
				go.Transform.Rotation = Rotation.FromYaw( yaw );
			}
			go.Name = $"{type}Pickup";

			var pickup = go.Components.Get<LootPickup>();
			if ( pickup == null )
				pickup = go.Components.Create<LootPickup>();

			pickup.Setup( type, amt );
		}
	}

	private Vector3 GetScatterPosOnGround( Vector3 origin )
	{
		// рандом по кругу (XY)
		float r = (float)Rng.NextDouble() * ScatterRadius;
		float a = (float)Rng.NextDouble() * MathF.PI * 2f;

		Vector3 pos = origin + new Vector3(
			MathF.Cos( a ) * r,
			MathF.Sin( a ) * r,
			0f
		);

		// ищем землю
		var tr = Scene.Trace
			.Ray( pos + Vector3.Up * GroundTraceUp, pos - Vector3.Up * GroundTraceDown )
			.Run();

		if ( tr.Hit )
			pos = tr.HitPosition;

		return pos + Vector3.Up * SpawnHeight;
	}
}
