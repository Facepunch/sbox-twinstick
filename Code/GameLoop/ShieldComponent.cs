using System;
using Twinstick;

public sealed class ShieldComponent : Component, IDamageListener
{
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

	private const float MinShield = 0f;
	private const float MaxShield = 100f;

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
		if ( ShieldAmount <= MinShield ) return;

		ShieldAmount -= GetDrainAmount();
		ShieldAmount = ShieldAmount.Clamp( MinShield, MaxShield );

		if ( ShieldAmount <= MinShield )
		{
			IsActive = false;
		}
	}

	void Regenerate()
	{
		if ( ShieldAmount >= MaxShield ) return;

		ShieldAmount += GetRegenerateAmount();
		ShieldAmount = ShieldAmount.Clamp( MinShield, MaxShield );
	}

	bool CanToggleActive()
	{
		if ( ShieldAmount <= MinShield ) return false;
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

		// Gizmo.Draw.ScreenText( $"Shield: {isActive}, {ShieldAmount}", Scene.Camera.PointToScreenPixels( Transform.Position ) );
	}

	void DeflectDamage( Twinstick.DamageInfo dmgInfo )
	{
		OnDamageDeflected?.Invoke( dmgInfo );
		// TODO: sound, VFX
	}

	public void OnDamage( ref Twinstick.DamageInfo dmgInfo )
	{
		if ( IsActive )
		{
			DeflectDamage( dmgInfo );
			dmgInfo.Damage = 0;
		}
	}
}
