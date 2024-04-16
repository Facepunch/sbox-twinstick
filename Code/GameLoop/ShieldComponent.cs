using System;
using Twinstick;

public sealed class ShieldComponent : Component, IDamageListener, IProjectileCollisionListener
{
	[ConVar( "twinstick_debug_shields" )]
	private static bool IsDebugging { get; set; } = false;

	/// <summary>
	/// The collider
	/// </summary>
	[Property, Group( "Components" )] public Collider Collider { get; set; }

	/// <summary>
	/// The player
	/// </summary>
	[Property, Group( "Components" )] public PlayerComponent Player { get; set; }

	/// <summary>
	/// How much shields we can have
	/// </summary>
	[Property, Group( "Configuration" )] public float ShieldAmount { get; set; } = 100f;

	/// <summary>
	/// How quickly do the shields drain
	/// </summary>
	[Property, Group( "Configuration" )] public float DrainPower { get; set; } = 10f;

	/// <summary>
	/// How quickly do the shields regenerate
	/// </summary>
	[Property, Group( "Configuration" )] public float RegeneratePower { get; set; } = 5f;

	[Property] public Action<Twinstick.DamageInfo> OnDamageDeflected { get; set; }
	[Property] public Action<bool> OnActiveChanged { get; set; }
	[Property] public RangedFloat ShieldRange { get; set; } = new( 0, 100f );

	public int Priority { get; set; } = 100;

	private bool isActive = false;
	public bool IsActive
	{ 
		get => isActive;
		set
		{
			var old = IsActive;
			isActive = value;
			ActiveChanged( old, isActive );
		}
	}

	void ActiveChanged( bool prevActive, bool active )
	{
		Collider.Enabled = active;
		OnActiveChanged?.Invoke( active );
	}

	float GetDrainAmount()
	{
		return DrainPower * Time.Delta;
	}

	float GetRegenerateAmount()
	{
		return RegeneratePower * Time.Delta;
	}

	void Drain()
	{
		if ( ShieldAmount <= ShieldRange.x ) return;

		ShieldAmount -= GetDrainAmount();
		ShieldAmount = ShieldAmount.Clamp( ShieldRange.x, ShieldRange.y );

		if ( ShieldAmount <= ShieldRange.x )
		{
			IsActive = false;
		}
	}

	void Regenerate()
	{
		if ( ShieldAmount >= ShieldRange.y ) return;

		ShieldAmount += GetRegenerateAmount();
		ShieldAmount = ShieldAmount.Clamp( ShieldRange.x, ShieldRange.y );
	}

	bool CanToggleActive()
	{
		if ( ShieldAmount <= ShieldRange.x ) return false;
		return true;
	}

	void DoInput()
	{
		using ( Player.ScopeInput() )
		{
			if ( Input.Pressed( "Attack2" ) )
			{
				if ( CanToggleActive() )
				{
					// Flip the thing off
					IsActive ^= true;
				}
			}
		}

	}

	protected override void OnUpdate()
	{
		DoInput();

		if ( IsActive )
		{
			Drain();
		}
		else
		{
			Regenerate();
		}

		if ( IsDebugging )
		{
			Gizmo.Draw.ScreenText( $"Shield: {isActive}, {ShieldAmount}", Scene.Camera.PointToScreenPixels( Transform.Position ), "Roboto", 12, TextFlag.Center );
		}
	}

	void DeflectDamage( Twinstick.DamageInfo dmgInfo )
	{
		OnDamageDeflected?.Invoke( dmgInfo );
		// TODO: sound, VFX
	}

	// @IDamageListener
	void IDamageListener.OnDamage( ref Twinstick.DamageInfo dmgInfo )
	{
		if ( IsActive )
		{
			DeflectDamage( dmgInfo );
			dmgInfo.Damage = 0;
		}
	}

	// @IProjectileCollisionListener
	bool IProjectileCollisionListener.OnProjectileCollision( ProjectileComponent projectile )
	{
		// Don't delete the projectile
		if ( IsActive )
		{
			projectile.SetOwner( Player.GameObject );
			projectile.SetDirection( Player.LookDirection );

			return false;
		}

		return true;
	}
}
