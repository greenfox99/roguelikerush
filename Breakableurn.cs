using Sandbox;
using System;
using System.Linq;

public sealed class BreakableUrn : Component
{
	[Property, Group( "Use" )] public float UseDistance { get; set; } = 120f;

	[Property, Group( "Drops" )] public GameObject CoinPickupPrefab { get; set; }
	[Property, Group( "Drops" )] public GameObject ExpPickupPrefab { get; set; }

	[Property, Group( "Drops/Coins" )] public int CoinMin { get; set; } = 2;
	[Property, Group( "Drops/Coins" )] public int CoinMax { get; set; } = 6;
	[Property, Group( "Drops/Coins" )] public float CoinMaxLuckMultiplier { get; set; } = 2.0f;

	[Property, Group( "Drops/Exp" )] public int ExpMin { get; set; } = 1;
	[Property, Group( "Drops/Exp" )] public int ExpMax { get; set; } = 3;
	[Property, Group( "Drops/Exp" )] public float ExpMaxLuckMultiplier { get; set; } = 1.75f;

	[Property, Group( "Drops" )] public float DropScatterRadius { get; set; } = 28f;
	[Property, Group( "Drops" )] public float DropSpawnHeight { get; set; } = 8f;

	[Property, Group( "FX" )] public GameObject BreakEffectPrefab { get; set; }
	[Property, Group( "FX" )] public SoundEvent BreakSound { get; set; }

	public bool IsBroken => _broken;

	private bool _broken;

	public bool CanUse( Player player )
	{
		if ( _broken || player == null || !player.IsValid )
			return false;

		float dist = Vector3.DistanceBetween( player.Transform.Position, Transform.Position );
		return dist <= UseDistance;
	}

	public void Break( Player player )
	{
		if ( _broken )
			return;

		_broken = true;

		float luck01 = GetLuck01( player );

		int coins = RollAmount( CoinMin, CoinMax, CoinMaxLuckMultiplier, luck01 );
		int exp = RollAmount( ExpMin, ExpMax, ExpMaxLuckMultiplier, luck01 );

		SpawnLoot( CoinPickupPrefab, LootType.Coins, coins );
		SpawnLoot( ExpPickupPrefab, LootType.Exp, exp );

		if ( BreakSound != null )
			Sound.Play( BreakSound, Transform.Position );

		if ( BreakEffectPrefab != null )
		{
			var fx = BreakEffectPrefab.Clone( Transform.Position );
			fx.Name = "UrnBreakFX";
		}

		GameObject.Destroy();
	}

	private float GetLuck01( Player player )
	{
		PlayerLuck luck = null;

		if ( player != null && player.IsValid && player.GameObject != null )
			luck = player.GameObject.GetComponent<PlayerLuck>();

		luck ??= Scene.GetAllComponents<PlayerLuck>().FirstOrDefault();

		if ( luck == null || !luck.IsValid || luck.MaxLuckPercent <= 0f )
			return 0f;

		float value = luck.LuckPercent / luck.MaxLuckPercent;

		if ( value < 0f ) value = 0f;
		if ( value > 1f ) value = 1f;

		return value;
	}

	private int RollAmount( int min, int max, float maxLuckMultiplier, float luck01 )
	{
		if ( max < min )
			max = min;

		int baseAmount = Random.Shared.Next( min, max + 1 );

		float mult = 1f + (MathF.Max( 1f, maxLuckMultiplier ) - 1f) * luck01;
		int result = (int)MathF.Round( baseAmount * mult );

		return Math.Max( 1, result );
	}

	private void SpawnLoot( GameObject prefab, LootType type, int amount )
	{
		if ( prefab == null || amount <= 0 )
			return;

		Vector3 spawnPos = Transform.Position + RandomHorizontalOffset() + Vector3.Up * DropSpawnHeight;
		var go = prefab.Clone( spawnPos );

		go.Name = $"{type}Pickup";

		var loot = go.GetComponent<LootPickup>() ?? go.GetComponentInChildren<LootPickup>();
		if ( loot != null && loot.IsValid )
		{
			loot.Setup( type, amount );
		}
	}

	private Vector3 RandomHorizontalOffset()
	{
		float angle = (float)(Random.Shared.NextDouble() * MathF.PI * 2f);
		float radius = (float)(Random.Shared.NextDouble() * DropScatterRadius);

		return new Vector3(
			MathF.Cos( angle ) * radius,
			MathF.Sin( angle ) * radius,
			0f
		);
	}
}
