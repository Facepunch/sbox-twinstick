using System;

namespace Twinstick;

public sealed class PlayerComponent : Component
{
	/// <summary>
	/// What's this player's ID? Used for input.
	/// </summary>
	[Property] public int PlayerId { get; private set; }

	/// <summary>
	/// A shorthand way to scope input.
	/// </summary>
	/// <returns></returns>
	public IDisposable ScopeInput() => Input.PlayerScope( PlayerId );

	public void SetPlayer( int playerId )
	{
		PlayerId = playerId;
	}

	protected override void OnUpdate()
	{
		using var _ = ScopeInput();
	}

	protected override void OnFixedUpdate()
	{
		using var _ = ScopeInput();
	}
}
