using System;

using Twinstick;

/// <summary>
/// Camera manager class that tries to keep the players in view.
/// </summary>
public sealed class CameraManager : Component
{
	[Property] public float DampTime { get; set; } = 0.2f;
	[Property] public float ScreenEdge { get; set; } = 4f;
	[Property] public float MinSize { get; set; } = 6.5f;
	[Property] public float BoostScreenScale { get; set; } = 25f;
	[Property] public CameraComponent Camera { get; set; }

	public IEnumerable<GameTransform> Targets
	{
		get
		{
			var players = Scene.GetAllComponents<PlayerComponent>();
			// Only look for alive players
			players = players.Where( x => x.Components.Get<HealthComponent>().State == HealthComponent.LifeState.Alive );
			return players.Select( x => x.Transform );
		}
	}
	public int Boosters
	{
		get
		{
			var players = Scene.GetAllComponents<PlayerComponent>();
			int count = 0;
			foreach ( var player in players )
			{
				if ( player.IsBoosting ) count++;
			}
			return count;
		}
	}

	private float ZoomSpeed;
	private Vector3 MoveVelocity;
	private Vector3 TargetPosition;

	protected override void OnStart()
	{
		ResetUpdate();
	}

	protected override void OnFixedUpdate()
	{
		FindAveragePosition();

		Transform.Position = Vector3.SmoothDamp( Transform.Position, TargetPosition, ref MoveVelocity, DampTime, Time.Delta );
		Camera.OrthographicHeight = MathX.SmoothDamp( Camera.OrthographicHeight, GetOrthoHeight(), ref ZoomSpeed, DampTime, Time.Delta );
	}

	private float GetOrthoHeight()
	{
		var localTransform = Transform.World.ToLocal( new Transform().WithPosition( TargetPosition ) );

		float size = 0f;
		foreach ( var target in Targets )
		{
			var targetLocalTransform = Transform.World.ToLocal( new Transform().WithPosition( target.Position ) );
			var posToTarget = targetLocalTransform.Position - localTransform.Position;

			size = MathF.Max( size, MathF.Abs( posToTarget.z ) );
			size = MathF.Max( size, MathF.Abs( posToTarget.y / Screen.Aspect ) );
		}

		size += ScreenEdge;
		size += Boosters * BoostScreenScale;
		size = MathF.Max( size, MinSize );

		return size;
	}

	/// <summary>
	/// Immediately moves position to desired spot, instead of lerping
	/// Good for when the round changes/etc
	/// </summary>
	public void ResetUpdate()
	{
		FindAveragePosition();

		Transform.Position = TargetPosition;
		Camera.OrthographicHeight = GetOrthoHeight();
	}

	void FindAveragePosition()
	{
		var averagePos = Vector3.Zero;
		int numTargets = 0;
		foreach ( var target in Targets )
		{
			averagePos += target.Position;
			numTargets++;
		}

		if ( numTargets > 0 ) averagePos /= numTargets;

		averagePos.z = Transform.Position.z;
		TargetPosition = averagePos;
	}
}
