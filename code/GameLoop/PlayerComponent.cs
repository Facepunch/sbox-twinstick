using System;

namespace Twinstick;

public sealed class PlayerComponent : Component
{
	/// <summary>
	/// What's this player's ID? Used for input.
	/// </summary>
	[Property, Group( "Input" )] public int PlayerId { get; private set; }

	/// <summary>
	/// The turn speed (right stick)
	/// </summary>
	[Property, Group( "Movement" )] public float TurnSpeed { get; set; } = 20f;

	[Property, Group( "Movement" )] public float MoveSpeed { get; set; } = 10f;

	[Property, Group( "Movement" )] public float Acceleration { get; set; } = 10f;

	[Range( 0, 200 ), Property, Group( "Movement" )] public float Radius { get; set; } = 16.0f;

	[Property, Group( "Movement" )] public float BaseFriction { get; set; } = 10f;

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
	private IDisposable ScopeInput() => Input.PlayerScope( PlayerId );

	public void SetPlayer( int playerId )
	{
		PlayerId = playerId;
	}

	protected override void OnUpdate()
	{
		using var _ = ScopeInput();

		Turn();
	}

	Rotation TargetRotation;
	Vector3 LookDirection;
	void Turn()
	{
		var analogLook = Input.AnalogLook.AsVector3();
		analogLook = analogLook.Normal;

		var direction = new Vector3( -analogLook.x, analogLook.y, 0 );

		// Don't bother if we're not looking at anything
		if ( direction.Length <= 0 )
			return;

		LookDirection = direction;

		const float fwd = 100f;
		var lookAt = Rotation.LookAt( direction * fwd );

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

	Vector3 WishVelocity;
	void MoveInput()
	{
		WishVelocity = Input.AnalogMove;

		if ( !WishVelocity.IsNearlyZero() )
		{
			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.ClampLength( 1 );
			WishVelocity *= MoveSpeed;
		}

		ApplyFriction( BaseFriction );

		Accelerate( WishVelocity );
	}

	private void ShootInput()
	{
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
}
