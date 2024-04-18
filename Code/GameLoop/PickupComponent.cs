namespace Twinstick;

public abstract class PickupComponent : Component, Component.ITriggerListener
{
	protected void InternalPickup( PlayerComponent player )
	{
		OnPickup( player );
		GameObject?.Destroy();
	}

	protected virtual void OnPickup( PlayerComponent player )
	{
		//
	}

	protected virtual bool CanPickup( PlayerComponent player )
	{
		return true;
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Root.Components.Get<PlayerComponent>( FindMode.EnabledInSelfAndDescendants ) is { } player && CanPickup( player ) )
		{
			InternalPickup( player );
		}
	}
}
