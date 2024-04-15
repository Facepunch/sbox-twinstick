namespace Twinstick;

public partial class ShootingComponent : Component
{
	[Property] public GameObject ProjectilePrefab { get; set; }
	[Property] public float FireRate { get; set; } = 0.1f;
	[Property] public TimeSince TimeSinceLastFire { get; set; } = 1f;

	[Property] public SoundEvent ShootSound { get; set; }

	protected bool CanFire()
	{
		return TimeSinceLastFire > FireRate;
	}

	public void Fire( Vector3 direction )
	{
		if ( !CanFire() ) return;

		TimeSinceLastFire = 0;

		var gameObject = ProjectilePrefab.Clone( new CloneConfig()
		{
			 Name = "Projectile",
			 Transform = new Transform( Transform.Position ),
			 StartEnabled = true,
		} );

		var projectile = gameObject.Components.Get<ProjectileComponent>();
		if ( projectile is null )
		{
			Log.Warning( "Tried to create a projectile that doesn't have a ProjectileComponent" );
			gameObject.Destroy();
		}
		else
		{
			projectile.SetOwner( GameObject );
			projectile.SetDirection( direction.Normal );
		}

		var snd = Sound.Play( ShootSound, Transform.Position );
	}
}