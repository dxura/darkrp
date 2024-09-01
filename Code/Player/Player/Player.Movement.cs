namespace Dxura.Darkrp;

public partial class Player
{
	/// <summary>
	/// Called when the player jumps.
	/// </summary>
	[Property]
	public Action? OnJump { get; set; }

	/// <summary>
	/// The player's box collider, so people can jump on other people.
	/// </summary>
	[Property]
	public BoxCollider PlayerBoxCollider { get; set; } = null!;
	
	/// <summary>
	/// Should camera be locked
	/// </summary>
	[Property] public bool LockCamera { get; set; }

	[RequireComponent] public TagBinder TagBinder { get; set; } = null!;

	/// <summary>
	/// How tall are we?
	/// </summary>
	[Property]
	[Group( "Config" )]
	public float Height { get; set; } = 64f;

	[Property] [Group( "Fall Damage" )] public float MinimumFallVelocity { get; set; } = 500f;
	[Property] [Group( "Fall Damage" )] public float MinimumFallSoundVelocity { get; set; } = 300f;
	[Property] [Group( "Fall Damage" )] public float FallDamageScale { get; set; } = 0.2f;

	[Property] [Group( "Sprint" )] public float SprintMovementDampening { get; set; } = 0.35f;

	/// <summary>
	/// Noclip movement speed
	/// </summary>
	[Property]
	public float NoclipSpeed { get; set; } = 1000f;

	public PlayerGlobals Global => GlobalGameNamespace.GetGlobal<PlayerGlobals>();

	/// <summary>
	/// Look direction of this player. Smoothly interpolated for networked players.
	/// </summary>
	public Angles EyeAngles
	{
		get => _smoothEyeAngles;
		set
		{
			if ( !IsProxy )
			{
				_smoothEyeAngles = value;
			}

			RawEyeAngles = value;
		}
	}

	[Sync] private Angles RawEyeAngles { get; set; }
	private Angles _smoothEyeAngles;

	/// <summary>
	/// Is the player crouching?
	/// </summary>
	[Sync]
	public bool IsCrouching { get; set; }

	public float CrouchAmount { get; set; }

	/// <summary>
	/// Is the player slow walking?
	/// </summary>
	[Sync]
	public bool IsSlowWalking { get; set; }

	/// <summary>
	/// Are we sprinting?
	/// </summary>
	[Sync]
	public bool IsSprinting { get; set; }

	/// <summary>
	/// Is the player noclipping?
	/// </summary>
	[Sync]
	public bool IsNoclipping { get; set; }

	/// <summary>
	/// If true, we're not allowed to move.
	/// </summary>
	[HostSync]
	public bool IsFrozen { get; set; }

	/// <summary>
	/// Last time this player moved or attacked.
	/// </summary>
	[Sync]
	public TimeSince TimeSinceLastInput { get; private set; }

	/// <summary>
	/// What's our holdtype?
	/// </summary>
	[Sync]
	private AnimationHelper.HoldTypes CurrentHoldType { get; set; } = AnimationHelper.HoldTypes.None;

	/// <summary>
	/// How quick do we wish to go?
	/// </summary>
	private Vector3 WishVelocity { get; set; }

	/// <summary>
	/// Are we on the ground?
	/// </summary>
	public bool IsGrounded { get; set; }

	/// <summary>
	/// How quick do we wish to go (normalized)
	/// </summary>
	public Vector3 WishMove { get; private set; }

	/// <summary>
	/// How much friction to apply to the aim eg if zooming
	/// </summary>
	public float AimDampening { get; set; } = 1.0f;

	/// <summary>
	/// An accessor to get the camera controller's aim ray.
	/// </summary>
	public Ray AimRay => CameraController.AimRay;

	private float _smoothEyeHeight;
	private Vector3 _previousVelocity;
	private Vector3 _jumpPosition;
	private bool _isTouchingLadder;
	private Vector3 _ladderNormal;

	[Sync] private float EyeHeightOffset { get; set; }

	private void UpdateEyes()
	{
		if ( !IsProxy )
		{
			var eyeHeightOffset = GetEyeHeightOffset();

			var target = eyeHeightOffset;
			var trace = TraceBBox( Transform.Position, Transform.Position, 0, 10f );
			if ( trace.Hit && target > _smoothEyeHeight )
			{
				// We hit something, that means we can't increase our eye height because something's in the way.
				eyeHeightOffset = _smoothEyeHeight;
				IsCrouching = true;
			}
			else
			{
				eyeHeightOffset = target;
			}

			EyeHeightOffset = eyeHeightOffset;
		}

		if ( PlayerBoxCollider.IsValid() )
		{
			// Bit shit, but it works
			PlayerBoxCollider.Center = new Vector3( 0, 0, 32 + _smoothEyeHeight );
			PlayerBoxCollider.Scale = new Vector3( 32, 32, 64 + _smoothEyeHeight );
		}
	}

	private TimeUntil _timeUntilAccelerationRecovered = 0;
	private float _accelerationAddedScale = 0;

	private void ApplyAcceleration()
	{
		var relative = _timeUntilAccelerationRecovered.Fraction.Clamp( 0, 1 );
		var acceleration = GetAcceleration();

		acceleration *= (relative + _accelerationAddedScale).Clamp( 0, 1 );

		CharacterController.Acceleration = acceleration;
	}

	private void OnUpdateMovement()
	{
		var cc = CharacterController;
		CurrentHoldType = CurrentEquipment.IsValid() ? CurrentEquipment.GetHoldType() : AnimationHelper.HoldTypes.None;

		if ( IsProxy )
		{
			_smoothEyeAngles = Angles.Lerp( _smoothEyeAngles, RawEyeAngles, Time.Delta / Scene.NetworkRate );
		}

		// Eye input
		if ( !IsProxy && cc.IsValid() )
		{
			if ( !IsProxy && HealthComponent.State == LifeState.Alive && !LockCamera )
			{
				EyeAngles += Input.AnalogLook * AimDampening;
				EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -90, 90 ) );
			}

			CameraController.UpdateFromEyes( _smoothEyeHeight );
		}

		if ( IsInVehicle )
		{
			if ( Body.IsValid() )
			{
				Body.Transform.Local = new Transform();
			}

			if ( AnimationHelper.IsValid() )
			{
				AnimationHelper.WithVelocity( 0 );
				AnimationHelper.WithWishVelocity( 0 );
				AnimationHelper.IsGrounded = true;
				AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
				AnimationHelper.MoveStyle = AnimationHelper.MoveStyles.Run;
				AnimationHelper.DuckLevel = 1f;
				AnimationHelper.HoldType = AnimationHelper.HoldTypes.HoldItem;
				AnimationHelper.AimBodyWeight = 0.1f;
			}
		}
		else
		{
			if ( Body.IsValid() )
			{
				Body.Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );
			}

			if ( AnimationHelper.IsValid() )
			{
				AnimationHelper.WithVelocity( cc.Velocity );
				AnimationHelper.WithWishVelocity( WishVelocity );
				AnimationHelper.IsGrounded = IsGrounded;
				AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
				AnimationHelper.MoveStyle = AnimationHelper.MoveStyles.Run;
				AnimationHelper.DuckLevel = MathF.Abs( _smoothEyeHeight ) / 32.0f;
				AnimationHelper.HoldType = CurrentHoldType;
				AnimationHelper.Handedness =
					CurrentEquipment.IsValid() ? CurrentEquipment.Handedness : AnimationHelper.Hand.Both;
				AnimationHelper.AimBodyWeight = 0.1f;
			}
		}

		AimDampening = 1.0f;
	}

	private float GetMaxAcceleration()
	{
		if ( !CharacterController.IsOnGround )
		{
			return Global.AirMaxAcceleration;
		}

		return Global.MaxAcceleration;
	}

	private void ApplyMovement()
	{
		var cc = CharacterController;

		CheckLadder();

		var gravity = Global.Gravity;

		if ( _isTouchingLadder )
		{
			LadderMove();
			return;
		}

		if ( cc.IsOnGround )
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
			cc.Accelerate( WishVelocity );
		}
		else
		{
			if ( !IsNoclipping )
			{
				cc.Velocity -= gravity * Time.Delta * 0.5f;
			}

			cc.Accelerate( WishVelocity.ClampLength( GetMaxAcceleration() ) );
		}

		if ( !cc.IsOnGround )
		{
			if ( !IsNoclipping )
			{
				cc.Velocity -= gravity * Time.Delta * 0.5f;
			}
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
		}

		if ( IsNoclipping )
		{
			var vertical = 0f;
			if ( Input.Down( "Jump" ) )
			{
				vertical = 1f;
			}

			if ( Input.Down( "Duck" ) )
			{
				vertical = -1f;
			}

			cc.IsOnGround = false;
			cc.Velocity = WishMove.Normal * EyeAngles.ToRotation() * NoclipSpeed;
			cc.Velocity += Vector3.Up * vertical * NoclipSpeed;
		}

		cc.ApplyFriction( GetFriction() );
		cc.Move();
	}

	private TimeSince _timeSinceCrouchPressed = 10f;
	private TimeSince _timeSinceCrouchReleased = 10f;

	private float CrouchLerpSpeed()
	{
		if ( _timeSinceCrouchPressed < 1f && _timeSinceCrouchReleased < 1f )
		{
			return Global.SlowCrouchLerpSpeed;
		}

		return Global.CrouchLerpSpeed;
	}

	private bool WantsToSprint =>
		Input.Down( "Run" ) && !IsSlowWalking && !HasEquipmentTag( "no_sprint" ) && WishMove.x > 0.2f;

	private TimeSince TimeSinceSprintChanged { get; set; } = 100;

	private void OnSprintChanged( bool before, bool after )
	{
		TimeSinceSprintChanged = 0;
	}

	private bool HasEquipmentTag( string tag )
	{
		return CurrentEquipment.IsValid() && CurrentEquipment.Tags.Has( tag );
	}

	private void BuildInput()
	{
		var wasSprinting = IsSprinting;

		IsSlowWalking = Input.Down( "Walk" ) || HasEquipmentTag( "aiming" );
		IsSprinting = WantsToSprint;

		if ( wasSprinting != IsSprinting )
		{
			OnSprintChanged( wasSprinting, IsSprinting );
		}

		IsCrouching = Input.Down( "Duck" ) && !IsNoclipping;

		IsUsing = Input.Down( "Use" );

		if ( Input.Pressed( "Duck" ) )
		{
			_timeSinceCrouchPressed = 0;
		}

		if ( Input.Released( "Duck" ) )
		{
			_timeSinceCrouchReleased = 0;
		}

		// Check if our current weapon has the planting tag and if so force us to crouch.
		var currentWeapon = CurrentEquipment;
		if ( currentWeapon.IsValid() && currentWeapon.Tags.Has( "planting" ) )
		{
			IsCrouching = true;
		}

		if ( Input.Pressed( "Noclip" ) && Game.IsEditor )
		{
			IsNoclipping = !IsNoclipping;
		}

		if ( WishMove.LengthSquared > 0.01f || Input.Down( "Attack1" ) )
		{
			TimeSinceLastInput = 0f;
		}

		if ( CharacterController.IsOnGround && !IsFrozen )
		{
			var bhop = Global.BunnyHopping;
			if ( bhop ? Input.Down( "Jump" ) : Input.Pressed( "Jump" ) )
			{
				CharacterController.Punch( Vector3.Up * Global.JumpPower * 1f );
				BroadcastPlayerJumped();
			}
		}
	}

	public SceneTraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f, float liftHead = 0.0f )
	{
		var bbox = CharacterController.BoundingBox;
		var mins = bbox.Mins;
		var maxs = bbox.Maxs;

		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		if ( liftHead > 0 )
		{
			end += Vector3.Up * liftHead;
		}

		var tr = Scene.Trace.Ray( start, end )
			.Size( mins, maxs )
			.WithoutTags( CharacterController.IgnoreLayers )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();
		return tr;
	}

	/// <summary>
	/// A network message that lets other users that we've triggered a jump.
	/// </summary>
	[Broadcast]
	public void BroadcastPlayerJumped()
	{
		AnimationHelper?.TriggerJump();
		OnJump?.Invoke();
	}

	public TimeSince TimeSinceGroundedChanged { get; private set; }

	private void GroundedChanged( bool wasOnGround, bool isOnGround )
	{
		if ( IsProxy )
		{
			return;
		}

		TimeSinceGroundedChanged = 0;

		if ( wasOnGround && !isOnGround )
		{
			_jumpPosition = Transform.Position;
		}

		if ( !wasOnGround && isOnGround && Global.EnableFallDamage && !IsNoclipping )
		{
			var minimumVelocity = MinimumFallVelocity;
			var vel = MathF.Abs( _previousVelocity.z );

			if ( vel > MinimumFallSoundVelocity )
			{
				PlayFallSound();
			}

			if ( vel > minimumVelocity )
			{
				var velPastAmount = vel - minimumVelocity;

				_timeUntilAccelerationRecovered = 1f;
				_accelerationAddedScale = 0f;

				using ( Rpc.FilterInclude( Connection.Host ) )
				{
					TakeFallDamage( velPastAmount * FallDamageScale );
				}
			}
		}
	}

	[Property] [Group( "Effects" )] public SoundEvent? LandSound { get; set; }

	[Broadcast]
	private void PlayFallSound()
	{
		var handle = Sound.Play( LandSound, Transform.Position );
		if ( handle.IsValid )
		{
			handle.ListenLocal = !IsProxy;
		}
	}

	[Broadcast]
	private void TakeFallDamage( float damage )
	{
		GameObject.TakeDamage( new DamageInfo( this, damage, null, Transform.Position,
			Flags: DamageFlags.FallDamage ) );
	}

	private void CheckLadder()
	{
		var cc = CharacterController;
		var wishvel = new Vector3( WishMove.x.Clamp( -1f, 1f ), WishMove.y.Clamp( -1f, 1f ), 0 );
		wishvel *= EyeAngles.WithPitch( 0 ).ToRotation();
		wishvel = wishvel.Normal;

		if ( _isTouchingLadder )
		{
			if ( Input.Pressed( "jump" ) )
			{
				cc.Velocity = _ladderNormal * 100.0f;
				_isTouchingLadder = false;
				return;
			}
			else if ( cc.GroundObject != null && _ladderNormal.Dot( wishvel ) > 0 )
			{
				_isTouchingLadder = false;
				return;
			}
		}

		const float ladderDistance = 1.0f;
		var start = Transform.Position;
		var end = start + (_isTouchingLadder ? _ladderNormal * -1.0f : wishvel) * ladderDistance;

		var pm = Scene.Trace.Ray( start, end )
			.Size( cc.BoundingBox.Mins, cc.BoundingBox.Maxs )
			.WithTag( "ladder" )
			.HitTriggers()
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		_isTouchingLadder = false;

		if ( pm.Hit )
		{
			_isTouchingLadder = true;
			_ladderNormal = pm.Normal;
		}
	}

	private void LadderMove()
	{
		var cc = CharacterController;
		cc.IsOnGround = false;

		var velocity = WishVelocity;
		var normalDot = velocity.Dot( _ladderNormal );
		var cross = _ladderNormal * normalDot;
		cc.Velocity = velocity - cross + -normalDot * _ladderNormal.Cross( Vector3.Up.Cross( _ladderNormal ).Normal );
		cc.Move();
	}

	private void BuildWishInput()
	{
		WishMove = 0f;

		if ( IsFrozen )
		{
			return;
		}

		WishMove += Input.AnalogMove;
	}

	private void BuildWishVelocity()
	{
		WishVelocity = 0f;

		var rot = EyeAngles.WithPitch( 0f ).ToRotation();

		if ( WishMove.Length > 1f )
		{
			WishMove = WishMove.Normal;
		}

		var wishDirection = WishMove * rot;
		wishDirection = wishDirection.WithZ( 0 );
		WishVelocity = wishDirection * GetWishSpeed();
	}

	/// <summary>
	/// Get the current friction.
	/// </summary>
	/// <returns></returns>
	// TODO: expose to global
	private float GetFriction()
	{
		if ( !CharacterController.IsOnGround )
		{
			return 0.1f;
		}

		if ( IsSlowWalking )
		{
			return Global.SlowWalkFriction;
		}

		if ( IsCrouching )
		{
			return Global.CrouchingFriction;
		}

		if ( IsSprinting )
		{
			return Global.SprintingFriction;
		}

		return Global.WalkFriction;
	}

	private float GetAcceleration()
	{
		if ( !CharacterController.IsOnGround )
		{
			return Global.AirAcceleration;
		}
		else if ( IsSlowWalking )
		{
			return Global.SlowWalkAcceleration;
		}
		else if ( IsCrouching )
		{
			return Global.CrouchingAcceleration;
		}
		else if ( IsSprinting )
		{
			return Global.SprintingAcceleration;
		}

		return Global.BaseAcceleration;
	}

	// TODO: expose to global
	private float GetEyeHeightOffset()
	{
		if ( IsInVehicle )
		{
			return -24f;
		}

		if ( IsCrouching )
		{
			return -16f;
		}

		if ( HealthComponent.State == LifeState.Dead )
		{
			return -48f;
		}

		return 0f;
	}

	private float GetSpeedPenalty()
	{
		var wpn = CurrentEquipment;
		if ( !wpn.IsValid() )
		{
			return 0;
		}

		return wpn.SpeedPenalty;
	}

	private float GetWishSpeed()
	{
		if ( IsSlowWalking )
		{
			return Global.SlowWalkSpeed;
		}

		if ( IsCrouching )
		{
			return Global.CrouchingSpeed;
		}

		if ( IsSprinting )
		{
			return Global.SprintingSpeed - GetSpeedPenalty() * 0.5f;
		}

		return Global.WalkSpeed - GetSpeedPenalty();
	}

	private void DebugUpdate()
	{
		DebugText.Update();
		DebugText.Write( $"Player", Color.White, 20 );
		DebugText.Write( $"Velocity: {CharacterController.Velocity}" );
		DebugText.Write( $"Speed: {CharacterController.Velocity.Length}" );
		DebugText.Spacer();
		DebugText.Write( $"Weapon Info", Color.White, 20 );
		DebugText.Write( $"Spread: {Spread}" );
	}
}
