namespace Dxura.Darkrp;

public sealed partial class Player : Component, IDescription, IAreaDamageReceiver, IRespawnable
{
	/// <summary>
	/// The player's body
	/// </summary>
	[Property]
	public PlayerBody? Body { get; set; }

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

	/// <summary>
	/// Finds the first <see cref="SkinnedModelRenderer"/> on <see cref="Body"/>
	/// </summary>
	public SkinnedModelRenderer? BodyRenderer => Body?.Components?.Get<SkinnedModelRenderer>();

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

	private float DeathcamSkipTime => 5f;
	private float DeathcamIgnoreInputTime => 1f;

	// deathcam
	private void UpdateDeathCam()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( LastDamageInfo is null )
		{
			return;
		}

		var killer = GetLastKiller();

		if ( killer.IsValid() )
		{
			EyeAngles = Rotation.LookAt( killer.Transform.Position - Transform.Position, Vector3.Up );
		}
	}

	protected override void OnFixedUpdate()
	{
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
}
