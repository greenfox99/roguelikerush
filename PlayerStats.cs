using Sandbox;
using System;

public sealed class PlayerStats : Component
{
	public static PlayerStats Instance { get; private set; }

	[Property] public int TotalKills { get; private set; } = 0;
	[Property] public int TotalExpGained { get; private set; } = 0;
	[Property] public int Coins { get; private set; } = 0;

	public event Action OnStatsChanged;

	protected override void OnStart()
	{
		Instance = this;
	}

	protected override void OnDisabled()
	{
		if ( Instance == this )
			Instance = null;

		LifetimeStatsService.FlushPending();
	}

	[ConCmd( "greenfox_givegold_10000", ConVarFlags.Server )]
	public static void GiveGold10000()
	{
		if ( Instance == null )
		{
			Log.Warning( "PlayerStats instance not found for command greenfox_givegold_10000" );
			return;
		}

		Instance.AddCoins( 10000 );
		Log.Info( "🪙 Команда greenfox_givegold_10000 выполнена: +10000 монет" );
	}

	public void AddKill()
	{
		TotalKills++;
		LifetimeStatsService.RecordNpcKill();
		Log.Info( $"👻 Убийств: {TotalKills}" );
		OnStatsChanged?.Invoke();
	}

	public void AddExp( int amount )
	{
		if ( amount <= 0 )
			return;

		TotalExpGained += amount;
		Log.Info( $"✨ Всего опыта: {TotalExpGained}" );
		OnStatsChanged?.Invoke();
	}

	public void AddCoins( int amount )
	{
		if ( amount <= 0 )
			return;

		Coins += amount;
		LifetimeStatsService.RecordGoldEarned( amount );
		Log.Info( $"🪙 Монет: {Coins}" );
		OnStatsChanged?.Invoke();
	}

	public void ResetStats()
	{
		TotalKills = 0;
		TotalExpGained = 0;
		Coins = 0;

		OnStatsChanged?.Invoke();
	}

	public bool TrySpendCoins( int amount )
	{
		if ( amount <= 0 )
			return true;

		if ( Coins < amount )
			return false;

		Coins -= amount;
		Log.Info( $"🪙 Монет: {Coins}" );
		OnStatsChanged?.Invoke();
		return true;
	}
}
