using Sandbox;

public static class LifetimeStatsService
{
	public const string NpcKillsTotalStat = "npc_kills_total";
	public const string GoldEarnedTotalStat = "gold_earned_total";

	public static void RecordNpcKill( int amount = 1 )
	{
		if ( amount <= 0 )
			return;

		try
		{
			Sandbox.Services.Stats.Increment( NpcKillsTotalStat, amount );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"Failed to record lifetime NPC kills stat: {ex.Message}" );
		}
	}

	public static void RecordGoldEarned( int amount )
	{
		if ( amount <= 0 )
			return;

		try
		{
			Sandbox.Services.Stats.Increment( GoldEarnedTotalStat, amount );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"Failed to record lifetime gold stat: {ex.Message}" );
		}
	}

	public static long GetLocalNpcKillsTotal()
	{
		return ReadLocalSum( NpcKillsTotalStat );
	}

	public static long GetLocalGoldEarnedTotal()
	{
		return ReadLocalSum( GoldEarnedTotalStat );
	}

	public static void FlushPending()
	{
		try
		{
			Sandbox.Services.Stats.Flush();
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"Failed to flush pending stats: {ex.Message}" );
		}
	}

	static long ReadLocalSum( string statName )
	{
		try
		{
			var stat = Sandbox.Services.Stats.LocalPlayer.Get( statName );
			return (long)System.Math.Round( (double)stat.Sum );
		}
		catch
		{
			return 0;
		}
	}
}
