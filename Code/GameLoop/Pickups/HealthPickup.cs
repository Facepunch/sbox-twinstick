namespace Twinstick;

public partial class HealthPickup : PickupComponent
{
	[Property] public float Health { get; set; } = 50f;

	protected override void OnPickup( PlayerComponent player )
	{
		player.HealthComponent.Health += Health;
	}

	protected override bool CanPickup( PlayerComponent player )
	{
		return player.HealthComponent.Health < player.HealthComponent.HealthRange.y;
	}
}
