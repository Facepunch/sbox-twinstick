namespace Twinstick;

public interface IProjectileCollisionListener
{
	public int Priority { get; }
	public bool OnProjectileCollision( ProjectileComponent projectile );
}
