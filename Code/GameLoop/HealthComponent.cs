using System;

namespace Twinstick;

public partial class HealthComponent : Component, IProjectileCollisionListener
{
	[ConVar( "twinstick_debug_health" )]
	private static bool IsDebugging { get; set; } = false;

	/// <summary>
	/// Can this object respawn?
	/// </summary>
	[Property, Group( "Life State" )] public float RespawnTime { get; set; } = 0f;
	[Property] public Action<float, float> OnHealthChanged { get; set; }
	[Property] public Action<LifeState, LifeState> OnLifeStateChanged { get; set; }

	float health = 100f;
	private LifeState state = LifeState.Alive;
	private TimeUntil TimeUntilRespawn;

	[Property] public RangedFloat HealthRange { get; set; } = new( 0, 100 );

	/// <summary>
	/// What's our health?
	/// </summary>
	[Property, ReadOnly] public float Health
	{
		get => health;
		set
		{
			var old = health;
			if ( old == value ) return;

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
			if ( old == value ) return;

			state = value;
			LifeStateChanged( old, state );
		}
	}

	void HealthChanged( float before, float after )
	{
		OnHealthChanged?.Invoke( before, after );

		// On death
		if ( Health <= HealthRange.x )
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
		if ( after == LifeState.Respawning )
		{
			TimeUntilRespawn = RespawnTime;
		}

		if ( after == LifeState.Alive )
		{
			Health = HealthRange.y;
		}

		OnLifeStateChanged?.Invoke( before, after );

		foreach ( var listener in Components.GetAll<ILifeStateListener>( FindMode.EnabledInSelfAndDescendants ) )
		{
			listener.OnLifeStateChanged( before, after );
		}
	}

	// @IProjectileCollisionRecipient
	public void OnProjectileCollision( ProjectileComponent projectile )
	{
		var dmgInfo = projectile.CalculateDamage();

		foreach ( var listener in Components.GetAll<IDamageListener>( FindMode.EnabledInSelfAndDescendants ) )
		{
			listener.OnDamage( ref dmgInfo );
		}

		if ( dmgInfo.Damage > 0f )
		{
			Health -= dmgInfo.Damage;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsDebugging )
		{
			Gizmo.Draw.ScreenText( $"Health: {Health}", Scene.Camera.PointToScreenPixels( Transform.Position ), "Roboto", 12, TextFlag.Center );
		}

		if ( State == LifeState.Respawning && TimeUntilRespawn )
		{
			State = LifeState.Alive;
		}
	}

	protected override void OnStart()
	{
		Health = HealthRange.y;
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
