using System;

namespace Twinstick;

public partial class HealthComponent : Component, IProjectileCollisionListener
{
	/// <summary>
	/// Can this object respawn?
	/// </summary>
	[Property, Group( "Life State" )] public float RespawnTime { get; set; } = 0f;
	[Property] public Action<float, float> OnHealthChanged { get; set; }
	[Property] public Action<LifeState, LifeState> OnLifeStateChanged { get; set; }

	float health = 100f;
	private LifeState state = LifeState.Alive;

	/// <summary>
	/// What's our health?
	/// </summary>
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

	/// <summary>
	/// What's our life state?
	/// </summary>
	[Property, ReadOnly, Group( "Life State" )]
	public LifeState State
	{
		get => state;
		set
		{
			var old = state;
			state = value;
			LifeStateChanged( old, state );
		}
	}

	void HealthChanged( float before, float after )
	{
		OnHealthChanged?.Invoke( before, after );

		// On death
		if ( Health <= 0f )
		{
			if ( RespawnTime > 0f ) State = LifeState.Respawning;
			else State = LifeState.Dead;
		}
		else
		{
			State = LifeState.Alive;
		}
	}

	void LifeStateChanged( LifeState before, LifeState after )
	{
		OnLifeStateChanged?.Invoke( before, after );

		foreach ( var listener in Components.GetAll<ILifeStateListener>( FindMode.EnabledInSelfAndDescendants ) )
		{
			listener.OnLifeStateChanged( before, after );
		}
	}

	// @IProjectileCollisionRecipient
	public void OnProjectileCollision( ProjectileComponent projectile )
	{
		var damage = projectile.CalculateDamage();
		Health -= damage;
	}

	/// <summary>
	/// The component's life state.
	/// </summary>
	public enum LifeState
	{
		Alive,
		Respawning,
		Dead
	}
}
