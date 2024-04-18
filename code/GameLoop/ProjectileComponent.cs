using System;

namespace Twinstick;

public partial class ProjectileComponent : Component, Component.ITriggerListener, Component.ITintable
{
	/// <summary>
	/// How fast is this projectile?
	/// </summary>
	[Property, Group( "Setup" )] public float Speed { get; set; }

	/// <summary>
	/// How big is the projectile?
	/// </summary>
	[Property, Group( "Setup" )] public float Radius { get; set; }

	/// <summary>
	/// How long until this projectile is deleted?
	/// </summary>
	[Property, Group( "Setup" )] public float Lifetime { get; set; }

	/// <summary>
	/// Is this a homing projectile?
	/// </summary>
	[Property, Group( "Homing" )] public bool IsHoming { get; set; } = false;

	/// <summary>
	/// How strong is the force that pulls projectiles towards its target?
	/// </summary>
	[Property, Group( "Homing" )] public float HomingPower { get; set; } = 10f;

	[Property, Group( "Homing" )] public float HomingRadius { get; set; } = 512f;

	/// <summary>
	/// The projectile's model renderer.
	/// </summary>
	[Property, Group( "Components" )] public ModelRenderer ModelRenderer { get; set; }

	/// <summary>
	/// How much damage?
	/// </summary>
	[Property, Group( "Damage" )] public float Damage { get; set; } = 25f;

	/// <summary>
	/// The particle
	/// </summary>
	[Property] public ParticleEffect Particles { get; set; } = null;

	/// <summary>
	/// Called when the projectile hits something.
	/// </summary>
	public Action<GameObject> OnProjectileHit { get; set; }

	/// <summary>
	/// How fast are we going?
	/// </summary>
	public Vector3 Velocity { get; set; }

	/// <summary>
	/// Who fired this? We don't want to hit them.
	/// </summary>
	public GameObject Owner { get; private set; }

	/// <summary>
	/// Sets the owner of this projectile.
	/// </summary>
	/// <param name="owner"></param>
	public void SetOwner( GameObject owner )
	{
		Owner = owner;
		Target = null;

		if ( owner.Root.Components.Get<PlayerComponent>( FindMode.EnabledInSelfAndDescendants ) is { } player )
		{
			Color col = player.Body.Color;

			Color = col;

			if ( Particles.IsValid() )
			{
				Particles.Tint = col * 5f;
				Particles.Gradient = col * 5f;
			}
		}
	}

	public DamageInfo CalculateDamage() => DamageInfo.FromProjectile( this );

	protected override void OnStart()
	{
		if ( Lifetime > 0f )
		{
			GameObject.DestroyAsync( Lifetime );
		}
	}

	protected override void OnDestroy()
	{
		// TODO: Explosions
	}

	protected override void OnUpdate()
	{
		UpdateHeading();
	}

	GameObject Target { get; set; }
	TimeSince TimeSinceTargeted { get; set; } = 1f;

	public Color Color
	{
		get => ModelRenderer.Tint;
		set
		{
			ModelRenderer.Tint = value;
		}
	}

	void TryGetTarget()
	{
		if ( TimeSinceTargeted < 0.2f ) return;

		TimeSinceTargeted = 0;

		var targets = Scene.FindInPhysics( BBox.FromPositionAndSize( Transform.Position, HomingRadius ) ).Where( IsSuitableCollision );
		var closestTarget = targets.OrderBy( x => x.Transform.Position.DistanceSquared( Transform.Position ) ).FirstOrDefault();

		Target = closestTarget;
	}

	void Home()
	{
		TryGetTarget();

		if ( IsHoming )
		{
			if ( Target.IsValid() )
			{
				var direction = ( Target.Transform.Position - Transform.Position ).Normal;
				Velocity += direction * ( HomingPower * Time.Delta );
				// TODO: clean this up
				Velocity = Velocity.Normal * Speed * Time.Delta;
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		Home();

		// Move the projectile
		Transform.Position += Velocity;

		CheckCollisions();
	}

	void CheckCollisions()
	{
		// Shitty expensive collision detection
		var objects = Scene.FindInPhysics( new Sphere( Transform.Position, Radius ) );
		foreach ( var obj in objects )
		{
			if ( IsSuitableCollision( obj ) )
			{
				Collide( obj );
			}
		}
	}

	bool IsSuitableCollision( GameObject obj )
	{
		// Don't home to projectiles 
		if ( obj.Root.Components.Get<ProjectileComponent>( FindMode.EnabledInSelfAndDescendants ) is { } projectile /* && projectile.Owner == Owner */ )
		{
			return false;
		}

		var colliders = obj.Root.Components.GetAll<Collider>( FindMode.EverythingInSelfAndDescendants );
		if ( colliders.Count( x => x.Enabled ) == 0 )
		{
			return false;
		}

		// Self 
		if ( obj == GameObject ) return false;
		if ( obj.Root == GameObject ) return false;
		if ( GameObject.Root == obj.Root ) return false;

		// Owner
		if ( obj == Owner ) return false;
		if ( obj.Root == Owner ) return false;
		if ( obj.Root == Owner.Root ) return false;

		return true;
	}

	void Collide( GameObject obj )
	{
		var rootObject = obj.Root;

		// All recipients need this event
		foreach ( var recipient in rootObject.Components.GetAll<IProjectileCollisionListener>( FindMode.EnabledInSelfAndDescendants ).OrderByDescending( x => x.Priority ) )
		{
			var shouldDestroy = recipient.OnProjectileCollision( this, obj );
			if ( !shouldDestroy ) return;

			GameObject.Destroy();
		}
	}

	void UpdateHeading()
	{
		Transform.Rotation = Rotation.LookAt( Velocity.Normal );
	}

	internal void SetDirection( Vector3 direction )
	{
		Velocity = direction * ( Speed * Time.Delta );

		UpdateHeading();
	}
}
