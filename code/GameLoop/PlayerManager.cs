using System.Text.Json.Serialization;

namespace Twinstick;

/// <summary>
/// A component that manages players.
/// </summary>
public partial class PlayerManager : Component
{
	/// <summary>
	/// A list of player colors we'll use in multiple systems across the game.
	/// </summary>
	[Property] public Dictionary<int, Color> PlayerColors { get; set; }

	/// <summary>
	/// A list of current players.
	/// </summary>
	public List<Player> Players { get; set; } = new();

	/// <summary>
	/// Adds a player to play
	/// </summary>
	/// <param name="playerId"></param>
	/// <param name="steamId"></param>
	/// <param name="controllable"></param>
	public void AddPlayer( int playerId, ulong steamId = 0, GameObject controllable = null )
	{
		var existing = Players.FirstOrDefault( x => x.Index == playerId );
		if ( existing is not null )
		{
			// Replace the GameObject
			existing.GameObject?.Destroy();
			existing.GameObject = controllable;
		}
		else
		{
			Players.Add( new()
			{
				Index = playerId,
				OwningSteamId = steamId,
				GameObject = controllable
			} );
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

	/// <summary>
	/// Clears out all the players in the game.
	/// </summary>
	public void Clear()
	{
		foreach ( var player in Players )
		{
			player.GameObject?.Destroy();
		}

		Players.Clear();
	}

	/// <summary>
	/// A player. They could be a networked player, or a local one FROM a networked player.
	/// </summary>
	public class Player
	{
		/// <summary>
		/// The owning connection's SteamId
		/// </summary>
		public ulong OwningSteamId { get; set; }

		/// <summary>
		/// The player's index. Unique to an OwningSteamId
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// What GameObject is our player controlling?
		/// </summary>
		public GameObject GameObject { get; set; }
	}
}
