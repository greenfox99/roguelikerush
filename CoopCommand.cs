using Sandbox;
using Sandbox.Network;
using System;

public static class CoopCommands
{
	private const string CoopScenePath = "scenes/mulitiplayer.scene";
	// если файл у тебя реально называется multiplayer.scene
	// то замени на "scenes/multiplayer.scene"

	[ConCmd( "host_coop" )]
	public static void HostCoop()
	{
		var path = NormalizeScenePath( CoopScenePath );

		if ( string.IsNullOrWhiteSpace( path ) )
		{
			Log.Warning( "host_coop: empty scene path" );
			return;
		}

		Networking.CreateLobby( new LobbyConfig
		{
			MaxPlayers = 4,
			Privacy = LobbyPrivacy.Public,
			Name = "RLR_COOP"
		} );

		Log.Info( $"host_coop: lobby created, loading '{path}'" );

		if ( Game.ActiveScene is null )
		{
			Log.Warning( "host_coop: no active scene" );
			return;
		}

		Game.ActiveScene.LoadFromFile( path );
	}

	private static string NormalizeScenePath( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
			return null;

		path = path.Trim().Replace( "\\", "/" );

		int assetsIndex = path.IndexOf( "/Assets/", StringComparison.OrdinalIgnoreCase );
		if ( assetsIndex >= 0 )
			path = path.Substring( assetsIndex + "/Assets/".Length );

		if ( path.StartsWith( "Assets/", StringComparison.OrdinalIgnoreCase ) )
			path = path.Substring( "Assets/".Length );

		if ( path.EndsWith( ".scene_c", StringComparison.OrdinalIgnoreCase ) ||
			 path.EndsWith( ".scene_d", StringComparison.OrdinalIgnoreCase ) )
		{
			int dotIndex = path.LastIndexOf( '.' );
			if ( dotIndex > 0 )
				path = path.Substring( 0, dotIndex );
		}

		if ( !path.EndsWith( ".scene", StringComparison.OrdinalIgnoreCase ) )
			path += ".scene";

		return path.ToLowerInvariant();
	}
}
