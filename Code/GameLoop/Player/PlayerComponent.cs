using System;

namespace Twinstick;

public sealed class PlayerComponent : Component, ILifeStateListener, IDamageListener
{
	/// <summary>
	/// What's this player's info? Used for input, networking, identification.
	/// </summary>
	[Property, Group( "Input" )] public PlayerManager.Player Info { get; private set; } = new();

	/// <summary>
	/// The turn speed (right stick)
	/// </summary>
	[Property, Group( "Movement" )] public float TurnSpeed { get; set; } = 20f;

	[Property, Group( "Movement" )] public float BoostTurnSpeed { get; set; } = 5f;

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
	/// The player's main collider
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

	void Turn()
	{
		var normalized = Input.AnalogMove.Normal;
		if ( MathF.Abs( normalized.x ) > 0.1f || MathF.Abs( normalized.y ) > 0.1f )
		{
			LookDirection = Input.AnalogMove.Normal;
		}

		TargetRotation = Rotation.Lerp( TargetRotation, Rotation.LookAt( LookDirection ), Time.Delta * GetTurnSpeed() );
		GameObject.Transform.Rotation = Rotation.From( 0, TargetRotation.Yaw(), 0 );
	}

	/// <summary>
	/// Add acceleration to the current velocity. 
	/// No need to scale by time delta - it will be done inside.
	/// </summary>
	public void Accelerate()
	{
		Velocity = Velocity.WithAcceleration( ( TargetRotation.Forward * GetMoveSpeed() ) * WishVelocity.Length, GetAcceleration() * Time.Delta );
		Velocity = Velocity.WithZ( 0f );
	}

	public float GetAcceleration()
	{
		return Acceleration;
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


	int _stuckTries;

	bool TryUnstuck()
	{
		var result = BuildTrace( Transform.Position, Transform.Position ).Run();

		// Not stuck, we cool
		if ( !result.StartedSolid )
		{
			_stuckTries = 0;
			return false;
		}

		int AttemptsPerTick = 20;
		for ( int i = 0; i < AttemptsPerTick; i++ )
		{
			var pos = Transform.Position + Vector3.Random.Normal * (((float)_stuckTries) / 2.0f);
			result = BuildTrace( pos, pos ).Run();

			if ( !result.StartedSolid )
			{
				//Log.Info( $"unstuck after {_stuckTries} tries ({_stuckTries * AttemptsPerTick} tests)" );
				Transform.Position = pos.WithZ( Transform.Position.y );
				return false;
			}
		}

		_stuckTries++;

		return true;
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

	float GetTurnSpeed()
	{
		if ( IsBoosting ) return BoostTurnSpeed;
		return TurnSpeed;
	}

	Vector3 WishVelocity;
	void MoveInput()
	{
		WishVelocity = EnableInput ? Input.AnalogMove : 0;

		SetBoosting( Input.Down( "Boost" ) && WishVelocity.Length > 0f );

		if ( !WishVelocity.IsNearlyZero() )
		{
			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.ClampLength( 1 );
		}

		ApplyFriction( BaseFriction );

		Accelerate();
	}

	private void ShootInput()
	{
		if ( !EnableInput ) return;

		if ( Input.Down( "Attack1" ) )
		{
			if ( Components.Get<ShootingComponent>( FindMode.EnabledInSelfAndDescendants )?.Fire( TargetRotation.Forward ) ?? false )
			{
				Input.TriggerHaptics( 0f, 1f, 0.0f, 0.0f, 100 );
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		using var _ = ScopeInput();

		MoveInput();
		TryUnstuck();
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

	void IDamageListener.OnDamage( ref DamageInfo dmgInfo )
	{
		if ( dmgInfo.Damage > 0f )
		{
			using ( ScopeInput() )
			{
				Input.TriggerHaptics( 1.0f, 0.0f, 0f, 0f, 500 );
			}
		}
	}
}
