namespace Dxura.Darkrp;

public partial class Player
{
	/// <summary>
	/// Called when the player jumps.
	/// </summary>
	[Property, Feature("Controller")]
	public Action? OnJump { get; set; }

	/// <summary>
	/// The player's box collider, so people can jump on other people.
	/// </summary>
	[Property, Feature("Controller")]
	public BoxCollider PlayerBoxCollider { get; set; } = null!;

	[RequireComponent] public TagBinder TagBinder { get; set; } = null!;

	[Property, Feature("Controller"), Group( "Config" )] public float MinimumFallVelocity { get; set; } = 500f;
	[Property, Feature("Controller"), Group( "Config" )] public float MinimumFallSoundVelocity { get; set; } = 300f;
	[Property, Feature("Controller"), Group( "Config" )] public float FallDamageScale { get; set; } = 0.2f;

	[Property, Feature("Controller"), Group( "Config" )] public float SprintMovementDampening { get; set; } = 0.35f;

	/// <summary>
	/// Noclip movement speed
	/// </summary>
	[Property, Feature("Controller"), Group( "Config" )]
	public float NoclipSpeed { get; set; } = 1000f;
	
	[Range( 0, 200 )] [Property, Feature("Controller"), Group( "Config" )] public float Radius { get; set; } = 16.0f;

	[Range( 0, 200 )] [Property, Feature("Controller"), Group( "Config" )] public float Height { get; set; } = 64.0f;
	[Range( 0, 50 )] [Property, Feature("Controller"), Group( "Config" )] public float StepHeight { get; set; } = 18.0f;

	[Range( 0, 90 )] [Property, Feature("Controller"), Group( "Config" )] public float GroundAngle { get; set; } = 45.0f;

	[Range( 0, 64 )] [Property, Feature("Controller"), Group( "Config" )] public float Acceleration { get; set; } = 10.0f;

	/// <summary>
	/// When jumping into walls, should we bounce off or just stop dead?
	/// </summary>
	[Range( 0, 1 )]
	[Property, Feature("Controller"), Group( "Config" )]
	public float Bounciness { get; set; } = 0.3f;

	/// <summary>
	/// If enabled, determine what to collide with using current project's collision rules for the <see cref="GameObject.Tags"/>
	/// of the containing <see cref="GameObject"/>.
	/// </summary>
	[Property, Feature("Controller")]
	[Group( "Collision" )]
	[Title( "Use Project Collision Rules" )]
	public bool UseCollisionRules { get; set; } = false;

	[Property, Feature("Controller")]
	[Group( "Collision" )]
	[HideIf( nameof(UseCollisionRules), true )]
	public TagSet IgnoreLayers { get; set; } = new();
	

	[Sync] public Vector3 Velocity { get; set; }

	[Sync] public bool IsOnGround { get; set; }

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
	
	public GameObject? GroundObject { get; set; }
	public Collider? GroundCollider { get; set; }
	public BBox BoundingBox => new(new Vector3( -Radius, -Radius, 0 ), new Vector3( Radius, Radius, Height + (IsCrouching ? _smoothEyeHeight : 0 )));

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
	
	private void OnUpdateController()
	{
		CurrentHoldType = CurrentEquipment.IsValid() ? CurrentEquipment.GetHoldType() : AnimationHelper.HoldTypes.None;

		if ( IsProxy )
		{
			_smoothEyeAngles = Angles.Lerp( _smoothEyeAngles, RawEyeAngles, Time.Delta / Scene.NetworkRate );
		}

		// Eye input
		if ( !IsProxy && IsValid )
		{
			if ( !IsProxy && HealthComponent.State == LifeState.Alive && !LockCamera )
			{
				EyeAngles += Input.AnalogLook * AimDampening;
				EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -90, 90 ) );
			}

			UpdateFromEyes( _smoothEyeHeight );
		}

		if ( IsSeated )
		{
			BodyRoot.LocalTransform = new Transform();

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
			BodyRoot.WorldRotation = Rotation.FromYaw( EyeAngles.yaw );

			
			if ( AnimationHelper.IsValid() )
			{
				AnimationHelper.WithVelocity( Velocity );
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
		if ( !IsOnGround )
		{
			return Global.AirMaxAcceleration;
		}

		return Global.MaxAcceleration;
	}

	private TimeUntil _timeUntilAccelerationRecovered = 0;
	private float _accelerationAddedScale = 0;
	
	private void OnFixedUpdateController()
	{
		var wasGrounded = IsGrounded;
		IsGrounded = IsOnGround;

		if ( IsGrounded != wasGrounded )
		{
			GroundedChanged( wasGrounded, IsGrounded );
		}
		
		_previousVelocity = Velocity;

		UpdateEyes();
		
		BuildWishInput();
		BuildWishVelocity();
		BuildInput();
		
		
		// Perform movement
		
		if (IsSeated)
		{
			return;
		}
		
		var relative = _timeUntilAccelerationRecovered.Fraction.Clamp( 0, 1 );
		var acceleration = GetAcceleration();

		acceleration *= (relative + _accelerationAddedScale).Clamp( 0, 1 );

		Acceleration = acceleration;
		
		CheckLadder();

		var gravity = Global.Gravity;

		if ( _isTouchingLadder )
		{
			LadderMove();
			return;
		}

		if ( IsOnGround )
		{
			Velocity = Velocity.WithZ( 0 );
			Accelerate( WishVelocity );
		}
		else
		{
			if ( !IsNoclipping )
			{
				Velocity -= gravity * Time.Delta * 0.5f;
			}

			Accelerate( WishVelocity.ClampLength( GetMaxAcceleration() ) );
		}

		if ( !IsOnGround )
		{
			if ( !IsNoclipping )
			{
				Velocity -= gravity * Time.Delta * 0.5f;
			}
		}
		else
		{
			Velocity = Velocity.WithZ( 0 );
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

			IsOnGround = false;
			Velocity = WishMove.Normal * EyeAngles.ToRotation() * NoclipSpeed;
			Velocity += Vector3.Up * vertical * NoclipSpeed;
		}

		ApplyFriction( GetFriction() );
		Move();
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

		if ( IsOnGround && !IsFrozen )
		{
			var bhop = Global.BunnyHopping;
			if ( bhop ? Input.Down( "Jump" ) : Input.Pressed( "Jump" ) )
			{
				Punch( Vector3.Up * Global.JumpPower * 1f );
				BroadcastPlayerJumped();
			}
		}
	}

	public SceneTraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f, float liftHead = 0.0f )
	{
		var bbox = BoundingBox;
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
			.WithoutTags( IgnoreLayers )
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
		var wishvel = new Vector3( WishMove.x.Clamp( -1f, 1f ), WishMove.y.Clamp( -1f, 1f ), 0 );
		wishvel *= EyeAngles.WithPitch( 0 ).ToRotation();
		wishvel = wishvel.Normal;

		if ( _isTouchingLadder )
		{
			if ( Input.Pressed( "jump" ) )
			{
				Velocity = _ladderNormal * 100.0f;
				_isTouchingLadder = false;
				return;
			}
			else if ( GroundObject != null && _ladderNormal.Dot( wishvel ) > 0 )
			{
				_isTouchingLadder = false;
				return;
			}
		}

		const float ladderDistance = 1.0f;
		var start = Transform.Position;
		var end = start + (_isTouchingLadder ? _ladderNormal * -1.0f : wishvel) * ladderDistance;

		var pm = Scene.Trace.Ray( start, end )
			.Size( BoundingBox.Mins, BoundingBox.Maxs )
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
		IsOnGround = false;

		var velocity = WishVelocity;
		var normalDot = velocity.Dot( _ladderNormal );
		var cross = _ladderNormal * normalDot;
		Velocity = velocity - cross + -normalDot * _ladderNormal.Cross( Vector3.Up.Cross( _ladderNormal ).Normal );
		Move();
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
		if ( !IsOnGround )
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
		if ( !IsOnGround )
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
		if ( IsSeated )
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

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineBBox( BoundingBox );
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
		if ( speed < 0.01f )
		{
			return;
		}

		// Bleed off some speed, but if we have less than the bleed
		//  threshold, bleed the threshold amount.
		var control = speed < stopSpeed ? stopSpeed : speed;

		// Add the amount to the drop amount.
		var drop = control * Time.Delta * frictionAmount;

		// scale the velocity
		var newspeed = speed - drop;
		if ( newspeed < 0 )
		{
			newspeed = 0;
		}

		if ( newspeed == speed )
		{
			return;
		}

		newspeed /= speed;
		Velocity *= newspeed;
	}

	private SceneTrace BuildTrace( Vector3 from, Vector3 to )
	{
		return BuildTrace( Scene.Trace.Ray( from, to ) );
	}

	private SceneTrace BuildTrace( SceneTrace source )
	{
		var trace = source.Size( BoundingBox ).IgnoreGameObjectHierarchy( GameObject );

		return UseCollisionRules ? trace.WithCollisionRules( Tags ) : trace.WithoutTags( IgnoreLayers );
	}

	/// <summary>
	/// Trace the controller's current position to the specified delta
	/// </summary>
	public SceneTraceResult TraceDirection( Vector3 direction )
	{
		return BuildTrace( GameObject.Transform.Position, GameObject.Transform.Position + direction ).Run();
	}

	private void Move( bool step )
	{
		if ( step && IsOnGround )
		{
			Velocity = Velocity.WithZ( 0 );
		}

		if ( Velocity.Length < 0.001f )
		{
			Velocity = Vector3.Zero;
			return;
		}

		var pos = GameObject.Transform.Position;

		var mover = new CharacterControllerHelper( BuildTrace( pos, pos ), pos, Velocity );
		mover.Bounce = Bounciness;
		mover.MaxStandableAngle = GroundAngle;

		if ( step && IsOnGround )
		{
			mover.TryMoveWithStep( Time.Delta, StepHeight );
		}
		else
		{
			mover.TryMove( Time.Delta );
		}

		Transform.Position = mover.Position;
		Velocity = mover.Velocity;
	}

	private void CategorizePosition()
	{
		var position = Transform.Position;
		var point = position + Vector3.Down * 2;
		var vBumpOrigin = position;
		var wasOnGround = IsOnGround;

		// We're flying upwards too fast, never land on ground
		if ( !IsOnGround && Velocity.z > 40.0f )
		{
			ClearGround();
			return;
		}

		//
		// trace down one step height if we're already on the ground "step down". If not, search for floor right below us
		// because if we do StepHeight we'll snap that many units to the ground
		//
		point.z -= wasOnGround ? StepHeight : 0.1f;


		var pm = BuildTrace( vBumpOrigin, point ).Run();

		//
		// we didn't hit - or the ground is too steep to be ground
		//
		if ( !pm.Hit || Vector3.GetAngle( Vector3.Up, pm.Normal ) > GroundAngle )
		{
			ClearGround();
			return;
		}

		//
		// we are on ground
		//
		IsOnGround = true;
		GroundObject = pm.GameObject;
		GroundCollider = pm.Shape?.Collider as Collider;

		//
		// move to this ground position, if we moved, and hit
		//
		if ( wasOnGround && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
		{
			Transform.Position = pm.EndPosition + pm.Normal * 0.01f;
		}
	}

	/// <summary>
	/// Disconnect from ground and punch our velocity. This is useful if you want the player to jump or something.
	/// </summary>
	public void Punch( in Vector3 amount )
	{
		ClearGround();
		Velocity += amount;
	}

	private void ClearGround()
	{
		IsOnGround = false;
		GroundObject = default;
		GroundCollider = default;
	}

	/// <summary>
	/// Move a character, with this velocity
	/// </summary>
	public void Move()
	{
		if ( TryUnstuck() )
		{
			return;
		}

		if ( IsOnGround )
		{
			Move( true );
		}
		else
		{
			Move( false );
		}

		CategorizePosition();
	}

	/// <summary>
	/// Move from our current position to this target position, but using tracing an sliding.
	/// This is good for different control modes like ladders and stuff.
	/// </summary>
	public void MoveTo( Vector3 targetPosition, bool useStep )
	{
		if ( TryUnstuck() )
		{
			return;
		}

		var pos = Transform.Position;
		var delta = targetPosition - pos;

		var mover = new CharacterControllerHelper( BuildTrace( pos, pos ), pos, delta );
		mover.MaxStandableAngle = GroundAngle;

		if ( useStep )
		{
			mover.TryMoveWithStep( 1.0f, StepHeight );
		}
		else
		{
			mover.TryMove( 1.0f );
		}

		Transform.Position = mover.Position;
	}

	private int _stuckTries;

	private bool TryUnstuck()
	{
		var result = BuildTrace( Transform.Position, Transform.Position ).Run();

		// Not stuck, we cool
		if ( !result.StartedSolid )
		{
			_stuckTries = 0;
			return false;
		}

		//using ( Gizmo.Scope( "unstuck", Transform.World ) )
		//{
		//	Gizmo.Draw.Color = Gizmo.Colors.Red;
		//	Gizmo.Draw.LineBBox( BoundingBox );
		//}

		var AttemptsPerTick = 20;

		for ( var i = 0; i < AttemptsPerTick; i++ )
		{
			var pos = Transform.Position + Vector3.Random.Normal * ((float)_stuckTries / 2.0f);

			// First try the up direction for moving platforms
			if ( i == 0 )
			{
				pos = Transform.Position + Vector3.Up * 2;
			}

			result = BuildTrace( pos, pos ).Run();

			if ( !result.StartedSolid )
			{
				//Log.Info( $"unstuck after {_stuckTries} tries ({_stuckTries * AttemptsPerTick} tests)" );
				Transform.Position = pos;
				return false;
			}
		}

		_stuckTries++;

		return true;
	}
}
