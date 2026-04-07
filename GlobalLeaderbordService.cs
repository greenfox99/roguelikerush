using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum GlobalLeaderboardSortMode
{
	NpcKills,
	GoldEarned
}

public sealed class GlobalLeaderboardEntry
{
	public long Rank { get; set; }
	public string DisplayName { get; set; } = "";
	public long GoldEarned { get; set; }
	public long NpcKills { get; set; }
}

public static class GlobalLeaderboardService
{
	public const string PackageIdent = "greengames.roguelikerush";
	public const int MaxEntries = 50;
	const int MaxRefreshAttempts = 3;

	static readonly List<GlobalLeaderboardEntry> _entries = new();

	public static IReadOnlyList<GlobalLeaderboardEntry> Entries => _entries;
	public static bool IsLoading { get; private set; }
	public static string ErrorMessage { get; private set; } = "";
	public static int Revision { get; private set; }
	public static GlobalLeaderboardSortMode CurrentMode { get; private set; } = GlobalLeaderboardSortMode.GoldEarned;

	public static void RequestRefresh( bool force = false )
	{
		RequestRefresh( CurrentMode, force );
	}

	public static void RequestRefresh( GlobalLeaderboardSortMode mode, bool force = false )
	{
		if ( IsLoading && !force )
			return;

		_ = LoadAsync( mode );
	}

	static async Task LoadAsync( GlobalLeaderboardSortMode mode )
	{
		IsLoading = true;
		ErrorMessage = "";
		CurrentMode = mode;
		Revision++;

		try
		{
			LifetimeStatsService.FlushPending();
			await Task.Delay( 200 );

			var merged = new Dictionary<string, GlobalLeaderboardEntry>( StringComparer.OrdinalIgnoreCase );

			var goldLoaded = await TryLoadBoardIntoAsync(
				LifetimeStatsService.GoldEarnedTotalStat,
				GlobalLeaderboardSortMode.GoldEarned,
				merged );

			var npcLoaded = await TryLoadBoardIntoAsync(
				LifetimeStatsService.NpcKillsTotalStat,
				GlobalLeaderboardSortMode.NpcKills,
				merged );

			_entries.Clear();

			if ( merged.Count > 0 )
			{
				var sorted = SortEntries( merged.Values.ToList(), mode );

				for ( int i = 0; i < sorted.Count; i++ )
				{
					sorted[i].Rank = i + 1;
					_entries.Add( sorted[i] );
				}
			}

			if ( !goldLoaded && !npcLoaded )
			{
				ErrorMessage = "Не удалось загрузить ни одну таблицу лидеров.";
			}
			else if ( !goldLoaded )
			{
				ErrorMessage = "Таблица по золоту не загрузилась, показаны только данные по NPC.";
			}
			else if ( !npcLoaded )
			{
				ErrorMessage = "Таблица по NPC не загрузилась, показаны только данные по золоту.";
			}
			else if ( _entries.Count == 0 )
			{
				ErrorMessage = "Таблица лидеров пуста.";
			}
		}
		catch ( Exception ex )
		{
			_entries.Clear();
			ErrorMessage = $"Не удалось загрузить объединённую таблицу лидеров: {ex.Message}";
			Log.Error( $"[Leaderboard] Combined load failed: {ex}" );
		}
		finally
		{
			IsLoading = false;
			Revision++;
		}
	}

	static async Task<bool> TryLoadBoardIntoAsync(
		string statName,
		GlobalLeaderboardSortMode boardMode,
		Dictionary<string, GlobalLeaderboardEntry> merged )
	{
		Exception lastException = null;

		for ( int attempt = 1; attempt <= MaxRefreshAttempts; attempt++ )
		{
			try
			{
				Log.Info( $"[Leaderboard] Loading stat='{statName}', attempt {attempt}/{MaxRefreshAttempts}" );

				var board = Sandbox.Services.Leaderboards.GetFromStat( PackageIdent, statName );

				if ( board == null )
					throw new Exception( "Leaderboards.GetFromStat returned null." );

				board.MaxEntries = MaxEntries;
				await board.Refresh();

				if ( board.Entries == null )
					throw new Exception( "board.Entries is null after refresh." );

				foreach ( var entry in board.Entries )
				{
					var displayName = string.IsNullOrWhiteSpace( $"{entry.DisplayName}" )
						? "Unknown"
						: $"{entry.DisplayName}";

					if ( !merged.TryGetValue( displayName, out var row ) )
					{
						row = new GlobalLeaderboardEntry
						{
							DisplayName = displayName,
							GoldEarned = 0,
							NpcKills = 0
						};

						merged[displayName] = row;
					}

					var value = SafeLong( entry.Value );

					if ( boardMode == GlobalLeaderboardSortMode.GoldEarned )
						row.GoldEarned = value;
					else
						row.NpcKills = value;
				}

				return true;
			}
			catch ( Exception ex )
			{
				lastException = ex;
				Log.Warning( $"[Leaderboard] Failed loading stat='{statName}' on attempt {attempt}: {ex.Message}" );

				if ( attempt < MaxRefreshAttempts )
					await Task.Delay( 250 * attempt );
			}
		}

		Log.Warning( $"[Leaderboard] Giving up on stat='{statName}': {lastException?.Message}" );
		return false;
	}

	static List<GlobalLeaderboardEntry> SortEntries( List<GlobalLeaderboardEntry> source, GlobalLeaderboardSortMode mode )
	{
		if ( mode == GlobalLeaderboardSortMode.NpcKills )
		{
			return source
				.OrderByDescending( x => x.NpcKills )
				.ThenByDescending( x => x.GoldEarned )
				.ThenBy( x => x.DisplayName )
				.ToList();
		}

		return source
			.OrderByDescending( x => x.GoldEarned )
			.ThenByDescending( x => x.NpcKills )
			.ThenBy( x => x.DisplayName )
			.ToList();
	}

	static long SafeLong( object value )
	{
		try
		{
			return Convert.ToInt64( value );
		}
		catch
		{
			return 0;
		}
	}
}
