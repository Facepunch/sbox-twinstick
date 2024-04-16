namespace Twinstick;

public partial class DamageInfo
{
	public float Damage { get; set; }
	public GameObject Attacker { get; set; }
	public GameObject Inflictor { get; set; }

	public static DamageInfo FromProjectile( ProjectileComponent projectile )
	{
		var dmg = new DamageInfo()
		{
			Damage = projectile.Damage,
			Attacker = projectile.Owner,
			Inflictor = projectile.GameObject
		};

		return dmg;
	}
}
