using System;

namespace Twinstick;

public partial class ProjectileComponent : Component, Component.ITriggerListener
{
	/// <summary>
	/// How fast is this projectile?
	/// </summary>
	[Property, Group( "Setup" )] public float Speed { get; set; }

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

	/// <summary>
	/// The projectile's model renderer.
	/// </summary>
	[Property, Group( "Components" )] public ModelRenderer ModelRenderer { get; set; }

	/// <summary>
	/// The projectile's collider.
	/// </summary>
	[Property, Group( "Components" )] public Collider Collider { get; set; }

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
	protected GameObject Owner { get; set; }

	/// <summary>
	/// Sets the owner of this projectile.
	/// </summary>
	/// <param name="owner"></param>
	public void SetOwner( GameObject owner )
	{
		Owner = owner;
	}

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

	protected override void OnFixedUpdate()
	{
		Transform.Position += Velocity;

		CheckCollisions();
	}

	void CheckCollisions()
	{
		// Shitty expensive collision detection
		var objects = Scene.FindInPhysics( Collider.KeyframeBody.GetBounds() );
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

		Log.Info( $"Collided with {rootObject}" );

		GameObject.Destroy();
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
