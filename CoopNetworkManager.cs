using Sandbox;

public sealed class CoopNetworkManager : Component, Component.INetworkListener
{
	[Property] public GameObject HostPlayerInScene { get; set; }
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject[] SpawnPoints { get; set; }

	private int _spawnIndex = 0;

	public void OnActive( Connection connection )
	{
		// Хост уже использует игрока, который стоит в сцене
		if ( connection == Connection.Local )
		{
			Log.Info( $"Host joined: using existing scene player for {connection.DisplayName}" );
			return;
		}

		if ( PlayerPrefab is null )
		{
			Log.Warning( "PlayerPrefab is null" );
			return;
		}

		var spawn = SpawnPoints != null && SpawnPoints.Length > 0
			? SpawnPoints[_spawnIndex % SpawnPoints.Length].Transform.World
			: Transform.World;

		_spawnIndex++;

		var player = PlayerPrefab.Clone( spawn );
		player.Name = $"Player_{connection.DisplayName}";
		player.NetworkSpawn( connection );

		Log.Info( $"Spawned remote player for {connection.DisplayName}" );
	}
}
