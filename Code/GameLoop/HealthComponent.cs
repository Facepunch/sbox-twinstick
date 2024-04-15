using System;

namespace Twinstick;

public partial class HealthComponent : Component, IProjectileCollisionRecipient
{
	[Property] public Action<float, float> OnHealthChanged { get; set; }

	float health = 100f;
	[Property, ReadOnly] public float Health
	{
		get => health;
		set
		{
			var old = health;
			health = value;
			HealthChanged( old, health );
		}
	}

	void HealthChanged( float oldHealth, float newHealth )
	{
		OnHealthChanged?.Invoke( oldHealth, newHealth );
	}

	public void OnProjectileCollision( ProjectileComponent projectile )
	{
		var damage = projectile.CalculateDamage();
		Health -= damage;
	}
}
