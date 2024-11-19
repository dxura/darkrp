namespace Dxura.Darkrp;

public sealed partial class Player : Component, IDescription, IAreaDamageReceiver, IRespawnable
{
	/// <summary>
	/// A reference to the player's head (the GameObject)
	/// </summary>
	[Property]
	public GameObject? Head { get; set; }

	/// <summary>
	/// A reference to the animation helper (normally on the Body GameObject)
	/// </summary>
	[Property]
	public AnimationHelper? AnimationHelper { get; set; }

	/// <summary>
	/// The current character controller for this player.
	/// </summary>
	[RequireComponent]
	public CharacterController CharacterController { get; set; } = null!;

	/// <summary>
	/// The current camera controller for this player.
	/// </summary>
	[RequireComponent]
	public CameraController CameraController { get; set; } = null!;

	/// <summary>
	/// The outline effect for this player.
	/// </summary>
	[RequireComponent]
	public HighlightOutline Outline { get; set; } = null!;

	/// <summary>
	/// Get a quick reference to the real Camera GameObject.
	/// </summary>
	public GameObject? CameraGameObject => CameraController?.Camera?.GameObject;

	// IDescription
	string IDescription.DisplayName => DisplayName;
	Color IDescription.Color => Job.Color;

	// IAreaDamageReceiver
	void IAreaDamageReceiver.ApplyAreaDamage( AreaDamage component )
	{
		var dmg = new DamageInfo( component.Attacker, component.Damage, component.Inflictor,
			component.WorldPosition,
			Flags: component.DamageFlags );

		HealthComponent.TakeDamage( dmg );
	}
	
	protected override void OnStart()
	{
		// TODO: expose these parameters please
		TagBinder.BindTag( "no_shooting",
			() => IsSprinting || TimeSinceSprintChanged < 0.25f || TimeSinceWeaponDeployed < 0.66f );
		TagBinder.BindTag( "no_aiming",
			() => IsSprinting || TimeSinceSprintChanged < 0.25f || TimeSinceGroundedChanged < 0.25f );

		GameObject.Name = $"Player ({DisplayName})";
	}

	public SceneTraceResult CachedEyeTrace { get; private set; }

	protected override void OnUpdate()
	{
		OnUpdatePawn();
		
		if ( HealthComponent.State == LifeState.Dead )
		{
			UpdateDeathCam();
		}

		OnUpdateMovement();

		CrouchAmount = CrouchAmount.LerpTo( IsCrouching ? 1 : 0, Time.Delta * CrouchLerpSpeed() );
		_smoothEyeHeight =
			_smoothEyeHeight.LerpTo( EyeHeightOffset * (IsCrouching ? CrouchAmount : 1), Time.Delta * 10f );
		CharacterController.Height = Height + _smoothEyeHeight;
	}
	
	protected override void OnFixedUpdate()
	{
		OnFixedUpdatePresence();
		
		var cc = CharacterController;
		if ( !cc.IsValid() )
		{
			return;
		}

		var wasGrounded = IsGrounded;
		IsGrounded = cc.IsOnGround;

		if ( IsGrounded != wasGrounded )
		{
			GroundedChanged( wasGrounded, IsGrounded );
		}

		UpdateEyes();
		UpdateOutline();

		if ( !IsProxy )
		{
			CachedEyeTrace = Scene.Trace.Ray( AimRay, 100000f )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "ragdoll", "movement" )
				.UseHitboxes()
				.Run();
		}

		if ( HealthComponent.State != LifeState.Alive )
		{
			return;
		}

		if ( IsProxy )
		{
			return;
		}

		_previousVelocity = cc.Velocity;

		UpdateUse();
		BuildWishInput();
		BuildWishVelocity();
		BuildInput();

		UpdateRecoilAndSpread();
		ApplyAcceleration();

		if ( IsInVehicle )
		{
			ApplyVehicle();
		}
		else
		{
			ApplyMovement();
		}
	}
	
	public void OnGameEvent( JobChangedEvent eventArgs )
	{
		Sound.Play( JobChangedSound, WorldPosition);
		UpdateBodyFromJob(eventArgs.After);
	}
}
