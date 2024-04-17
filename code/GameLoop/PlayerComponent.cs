using System;

namespace Twinstick;

public sealed class PlayerComponent : Component, ILifeStateListener
{
	/// <summary>
	/// What's this player's info? Used for input, networking, identification.
	/// </summary>
	[Property, Group( "Input" )] public PlayerManager.Player Info { get; private set; } = new();

	/// <summary>
	/// The turn speed (right stick)
	/// </summary>
	[Property, Group( "Movement" )] public float TurnSpeed { get; set; } = 20f;

	/// <summary>
	/// What's our base move speed (left stick)?
	/// </summary>
	[Property, Group( "Movement" )] public float MoveSpeed { get; set; } = 500f;

	/// <summary>
	/// What's our move speed when boosting?
	/// </summary>
	[Property, Group( "Movement" )] public float BoostMoveSpeed { get; set; } = 1000f;

	/// <summary>
	/// How quickly can we get moving?
	/// </summary>
	[Property, Group( "Movement" )] public float Acceleration { get; set; } = 10f;

	/// <summary>
	/// For movement, how wide are we?
	/// </summary>
	[Range( 0, 200 ), Property, Group( "Movement" )] public float Radius { get; set; } = 16.0f;

	/// <summary>
	/// How slippery is our movement?
	/// </summary>
	[Property, Group( "Movement" )] public float BaseFriction { get; set; } = 10f;

	/// <summary>
	/// The body component, which handles visuals for the player.
	/// </summary>
	[Property, Group( "Components" )] public PlayerBodyComponent Body { get; set; }

	/// <summary>
	/// The player's health component.
	/// </summary>
	[Property, Group( "Components" )] public HealthComponent HealthComponent { get; set; }

	/// <summary>
	/// The main collider for the player.
	/// </summary>
	[Property, Group( "Components" )] public Collider MainCollider { get; set; }

	/// <summary>
	/// Is input enabled right now for this player?
	/// </summary>
	private bool EnableInput { get; set; } = true;

	/// <summary>
	/// The bounds of the player.
	/// </summary>
	public BBox BoundingBox => new BBox( new Vector3( -Radius, -Radius, -Radius ), new Vector3( Radius, Radius, Radius ) );

	/// <summary>
	/// A reference to the scene's camera. There's only one in this game.
	/// </summary>
	public CameraComponent Camera => Scene.Camera;

	/// <summary>
	/// How fast are we going?
	/// </summary>
	public Vector3 Velocity { get; set; }

	/// <summary>
	/// A shorthand way to scope input.
	/// </summary>
	/// <returns></returns>
	internal IDisposable ScopeInput() => Input.PlayerScope( Info.Index );

	public void SetPlayer( int playerId, ulong steamId = 0 )
	{
		if ( steamId == 0 ) steamId = Connection.Local.SteamId;

		Info = new()
		{
			Index = playerId,
			OwningSteamId = steamId,
			GameObject = GameObject
		};

		Body.SetPlayerId( playerId );
	}

	protected override void OnUpdate()
	{
		using var _ = ScopeInput();
		Turn();
	}

	Rotation TargetRotation;
	internal Vector3 LookDirection;

	[Property] public bool IsTwinStick { get; set; } = false;

	void Turn()
	{
		if ( IsTwinStick )
		{
			var analogLook = EnableInput ? Input.AnalogLook.AsVector3() : 0;
			analogLook = analogLook.Normal;

			var direction = new Vector3( -analogLook.x, analogLook.y, 0 );

			// Don't bother if we're not looking at anything
			if ( direction.Length <= 0 )
				return;

			LookDirection = direction;
		}
		else
		{
			if ( Velocity.Length < 0.1f ) 
				return;

			LookDirection = Velocity.Normal;
		}

		const float fwd = 100f;
		var lookAt = Rotation.LookAt( LookDirection * fwd );

		TargetRotation = Rotation.Lerp( TargetRotation, lookAt, Time.Delta * TurnSpeed );
		GameObject.Transform.Rotation = Rotation.From( 0, TargetRotation.Yaw(), 0 );
	}

	/// <summary>
	/// Add acceleration to the current velocity. 
	/// No need to scale by time delta - it will be done inside.
	/// </summary>
	public void Accelerate( Vector3 vector )
	{
		Velocity = Velocity.WithAcceleration( vector, Acceleration * Time.Delta );
	}

	/// <summary>
	/// Apply an amount of friction to the current velocity.
	/// No need to scale by time delta - it will be done inside.
	/// </summary>
	public void ApplyFriction( float frictionAmount, float stopSpeed = 140.0f )
	{
		var speed = Velocity.Length;
		if ( speed < 0.01f ) return;

		// Bleed off some speed, but if we have less than the bleed
		//  threshold, bleed the threshold amount.
		float control = (speed < stopSpeed) ? stopSpeed : speed;

		// Add the amount to the drop amount.
		var drop = control * Time.Delta * frictionAmount;

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;
		if ( newspeed == speed ) return;

		newspeed /= speed;
		Velocity *= newspeed;
	}

	SceneTrace BuildTrace( Vector3 from, Vector3 to ) => BuildTrace( Scene.Trace.Ray( from, to ) );

	SceneTrace BuildTrace( SceneTrace source ) => source.Size( BoundingBox ).WithoutTags( "player" ).IgnoreGameObjectHierarchy( GameObject );

	/// <summary>
	/// Trace the controller's current position to the specified delta
	/// </summary>
	public SceneTraceResult TraceDirection( Vector3 direction )
	{
		return BuildTrace( GameObject.Transform.Position, GameObject.Transform.Position + direction ).Run();
	}

	void Move()
	{
		var pos = GameObject.Transform.Position;

		var mover = new CharacterControllerHelper( BuildTrace( pos, pos ), pos, Velocity );
		mover.Bounce = 0.3f;
		mover.MaxStandableAngle = 90f;

		mover.TryMove( Time.Delta );

		Transform.Position = mover.Position;
		Velocity = mover.Velocity;
	}

	public bool IsBoosting { get; private set; } = false;
	void SetBoosting( bool boosting )
	{
		if ( boosting == IsBoosting ) return;

		IsBoosting = boosting;
		Body.SetBoosting( boosting );
	}

	float GetMoveSpeed()
	{
		if ( IsBoosting ) return BoostMoveSpeed;
		return MoveSpeed;
	}

	Vector3 WishVelocity;
	void MoveInput()
	{
		WishVelocity = EnableInput ? Input.AnalogMove : 0;

		SetBoosting( Input.Down( "Run" ) && WishVelocity.Length > 0f );

		if ( !WishVelocity.IsNearlyZero() )
		{
			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.ClampLength( 1 );
			WishVelocity *= GetMoveSpeed();
		}

		ApplyFriction( BaseFriction );

		Accelerate( WishVelocity );
	}

	private void ShootInput()
	{
		if ( !EnableInput ) return;

		if ( Input.Down( "Attack1" ) )
		{
			Components.Get<ShootingComponent>( FindMode.EnabledInSelfAndDescendants )?.Fire( LookDirection );
		}
	}

	protected override void OnFixedUpdate()
	{
		using var _ = ScopeInput();

		MoveInput();
		Move();
		ShootInput();
	}

	/// <summary>
	/// Called when a health component's life state has changed, on this GameObject, or a child of the HealthComponent
	/// </summary>
	/// <param name="before"></param>
	/// <param name="after"></param>
	void ILifeStateListener.OnLifeStateChanged( HealthComponent.LifeState before, HealthComponent.LifeState after )
	{
		if ( after == HealthComponent.LifeState.Dead || after == HealthComponent.LifeState.Respawning )
		{
			EnableInput = false;
			Body?.SetShouldRender( false );
			MainCollider.Enabled = false;
		}
		if ( after == HealthComponent.LifeState.Alive )
		{
			EnableInput = true;
			Body?.SetShouldRender( true );
			MainCollider.Enabled = true;

			GameStateManager.Instance.PlayerSpawner?.MoveToSpawnPoint( this );
		}
	}
}
