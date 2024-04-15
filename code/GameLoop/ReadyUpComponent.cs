using System;

namespace Twinstick;

/// <summary>
/// Responsible for seeing if players are ready, and designating them in player slots.
/// </summary>
public sealed class ReadyUpComponent : Component
{
	/// <summary>
	/// Are we waiting for input?
	/// </summary>
	[Property] public bool WaitingForInput { get; set; } = true;

	/// <summary>
	/// How many players can play?
	/// </summary>
	[Property, Group( "Setup" )] public int MaxPlayers { get; set; } = 4;

	/// <summary>
	/// How many players minimum until we start ticking away that we can play?
	/// </summary>
	[Property, Group( "Setup" )] public int MinPlayers { get; set; } = 1;

	/// <summary>
	/// A list of players who are ready
	/// </summary>
	public Dictionary<int, bool> ReadyPlayers { get; set; } = new();

	/// <summary>
	/// Called when the min players threshold is reached
	/// </summary>
	[Property] public Action<int[]> OnMinPlayersReached { get; set; }

	/// <summary>
	/// Called when the max players threshold is reached
	/// </summary>
	[Property] public Action<int[]> OnMaxPlayersReached { get; set; }

	/// <summary>
	/// Called when a player readys up
	/// </summary>
	[Property] public Action<int, bool> OnPlayerReady { get; set; }

	/// <summary>
	/// Which key should we listen to?
	/// </summary>
	[Property, InputAction, Group( "Setup" )] public string InputAction { get; set; } = "Run";

	public float ControllerCount => MathF.Min( Input.ControllerCount, MaxPlayers );

	public int ReadyCount => ReadyPlayers.Count;

	public void ToggleReady( int playerId )
	{
		if ( !ReadyPlayers.TryGetValue( playerId, out var _ ) )
		{
			ReadyPlayers.Add( playerId, true );
			OnToggleReady( playerId, true );
		}
		else
		{
			ReadyPlayers.Remove( playerId );
			OnToggleReady( playerId, false );
		}
	}

	public bool IsReady( int playerId )
	{
		if ( ReadyPlayers.TryGetValue( playerId, out var isReady ) )
		{
			return isReady;
		}

		return false;
	}

	private void OnToggleReady( int playerId, bool isReady )
	{
		OnPlayerReady?.Invoke( playerId, isReady );

		var readyPlayers = ReadyPlayers.Keys.ToArray();

		if ( ReadyPlayers.Count >= MinPlayers )
		{
			OnMinPlayersReached?.Invoke( readyPlayers );
		}

		if ( ReadyPlayers.Count >= MaxPlayers )
		{
			OnMaxPlayersReached?.Invoke( readyPlayers );
		}
	}

	protected override void OnUpdate()
	{
		// Go through, set the input scope, listen for input
		for ( int i = 0; i < ControllerCount; i++ )
		{
			using ( Input.PlayerScope( i ) )
			{
				if ( Input.Pressed( InputAction ) )
				{
					ToggleReady( i );
				}
			}
		}
	}
}
