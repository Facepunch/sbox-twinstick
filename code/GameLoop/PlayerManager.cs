namespace Twinstick;

/// <summary>
/// A component that manages players.
/// </summary>
public partial class PlayerManager : Component
{
	public List<Player> Players { get; set; } = new();

	/// <summary>
	/// Adds a player to play
	/// </summary>
	/// <param name="playerId"></param>
	/// <param name="controllable"></param>
	public void AddPlayer( int playerId, GameObject controllable )
	{
		var existing = Players.FirstOrDefault( x => x.Index == playerId );
		if ( existing is not null )
		{
			// Replace the GameObject
			existing.GameObject?.Destroy();
			existing.GameObject = controllable;
		}
	}

	/// <summary>
	/// Removes a player from play 
	/// </summary>
	/// <param name="playerId"></param>
	public void RemovePlayer( int playerId )
	{
		var player = Players.FirstOrDefault( x => x.Index == playerId );
		if ( player is null )
		{
			return;
		}

		player.GameObject?.Destroy();
		Players.Remove( player );
	}

	public void Clear()
	{
		foreach ( var player in Players )
		{
			player.GameObject?.Destroy();
		}

		Players.Clear();
	}

	public class Player
	{
		public int Index { get; set; }
		public GameObject GameObject { get; set; }
	}
}
