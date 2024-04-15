namespace Twinstick;

/// <summary>
/// A class that handles the game state.
/// </summary>
public sealed class GameStateManager : Component
{
	/// <summary>
	/// The ready up system
	/// </summary>
	[Property, Group( "Setup" )] public ReadyUpComponent ReadyUpSystem { get; set; }

	/// <summary>
	/// The player spawner system
	/// </summary>
	[Property, Group( "Setup" )] public PlayerSpawnerComponent PlayerSpawner { get; set; }

	/// <summary>
	/// The player manager
	/// </summary>
	[Property, Group( "Setup" )] public PlayerManager PlayerManager { get; set; }

	[Property, Group( "Development" )] public bool IsDevelopment { get; set; } = true;

	/// <summary>
	/// What's our current state?
	/// </summary>
	[Property, Title( "Game State" ), Group( "Data" )] public GameState CurrentState { get; private set; }

	/// <summary>
	/// Does the current game state have a countdown?
	/// </summary>
	public bool HasCountdown
	{
		get => CurrentState switch
		{
			GameState.ReadyingPlayers => true,
			GameState.Countdown => true,
			GameState.Play => true,
			GameState.GameOver => true,
			_ => false
		};
	}

	/// <summary>
	/// What's the current countdown?
	/// </summary>
	public TimeUntil TimeUntilCountdown { get; private set; }

	/// <summary>
	/// The singleton instance of GameStateManager
	/// </summary>
	public static GameStateManager Instance { get; private set; }

	protected override void OnEnabled()
	{
		ReadyUpSystem.OnMinPlayersReached	+= OnMinPlayersReached;
		ReadyUpSystem.OnPlayerReady			+= OnPlayerReady;
		ReadyUpSystem.OnMinPlayersReached	+= OnMaxPlayersReached;

		if ( IsDevelopment )
		{
			SetGameState( GameState.Play );
			CreatePlayers( 0, 1 );
		}

		Instance = this;
	}

	protected override void OnDisabled()
	{
		ReadyUpSystem.OnMinPlayersReached -= OnMinPlayersReached;
		ReadyUpSystem.OnPlayerReady		  -= OnPlayerReady;
		ReadyUpSystem.OnMinPlayersReached -= OnMaxPlayersReached;

		if ( Instance == this )
			Instance = null;
	}

	private void OnPlayerReady( int playerId, bool isReady )
	{
		Log.Info( $"Player {playerId} is {(isReady ? "ready" : "not ready")}" );
	}

	private void CreatePlayers( params int[] players )
	{
		// Create the players
		foreach ( var playerId in players )
		{
			PlayerSpawner.CreatePlayer( playerId );
		}
	}

	private void OnMinPlayersReached( int[] players )
	{
		// Don't care
		if ( CurrentState != GameState.ReadyingPlayers ) return;

		SetGameState( GameState.Countdown );
	}

	private void OnMaxPlayersReached( int[] players )
	{
		// Don't care
		if ( CurrentState != GameState.ReadyingPlayers ) return;
	}

	/// <summary>
	/// Safely sets the game's state.
	/// </summary>
	/// <param name="state"></param>
	public void SetGameState( GameState state )
	{
		var old = CurrentState;
		CurrentState = state;

		OnGameStateChanged( old, state );
	}
	
	/// <summary>
	/// Called when the game state changes
	/// </summary>
	/// <param name="old"></param>
	/// <param name="new"></param>
	void OnGameStateChanged( GameState old, GameState @new )
	{
		if ( @new == GameState.Countdown )
		{
			CreatePlayers( ReadyUpSystem.ReadyPlayers.Keys.ToArray() );
			TimeUntilCountdown = 10;
		}
		if ( @new == GameState.Play )
		{
			TimeUntilCountdown = 120;
		}
		if ( @new == GameState.GameOver )
		{
			TimeUntilCountdown = 10;
		}

		if ( @new == GameState.ReadyingPlayers )
		{
			// Clear the players if we had some already
			PlayerManager.Clear();
		}
	}

	private void OnCountdownEnded()
	{
		// The game countdown ended, and now the game is over!
		if ( CurrentState == GameState.Play )
		{
			SetGameState( GameState.GameOver );
		}

		// The starting countdown ended, and now we're ready to play.
		if ( CurrentState == GameState.Countdown )
		{
			SetGameState( GameState.Play );
		}

		// The game ended, and the countdown for that just ended too
		if ( CurrentState == GameState.GameOver )
		{
			SetGameState( GameState.ReadyingPlayers );
		}
	}

	protected override void OnUpdate()
	{
		if ( HasCountdown && TimeUntilCountdown )
		{
			OnCountdownEnded();
		}
	}

	/// <summary>
	/// A list of game states that do things in the game.
	/// </summary>
	public enum GameState
	{
		[Title( "Waiting For Players" ), Description( "We're waiting for players to hit buttons on their gamepad." )]
		ReadyingPlayers,

		[Title( "Game Countdown" ), Description( "The game is starting!" )]
		Countdown,

		[Title( "Playing" ), Description( "The game is currently in the middle of play." )]
		Play,

		[Title( "Game Over" ), Description( "Someone won!" )]
		GameOver
	}
}
