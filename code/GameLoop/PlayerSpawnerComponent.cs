using System;

namespace Twinstick;

public sealed class PlayerSpawnerComponent : Component
{
	[Property] public GameStateManager GameManager { get; set; }
	[Property] public GameObject PlayerPrefab { get; set; }

	public SpawnPoint FindSpawnPoint( int playerId )
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>()
			.ToList();

		return spawnPoints[playerId];
	}

	public void CreatePlayer( int playerId )
	{
		var spawnPoint = FindSpawnPoint( playerId );
		var gameObject = PlayerPrefab.Clone( new CloneConfig()
		{
			Name = $"Player {playerId}",
			Transform = new Transform().WithPosition( spawnPoint.Transform.Position ),
			StartEnabled = true,
		} );

		var player = gameObject.Components.Get<PlayerComponent>();
		player.SetPlayer( playerId );

		// Register the player
		GameManager.PlayerManager.AddPlayer( playerId, gameObject );
	}

	/// <summary>
	/// Moves a player to a spawnpoint
	/// </summary>
	/// <param name="player"></param>
	internal void MoveToSpawnPoint( PlayerComponent player )
	{
		var spawnPoint = FindSpawnPoint( player.PlayerId );
		if ( spawnPoint is not null )
		{
			player.Transform.Position = spawnPoint.Transform.Position;
		}
	}
}
