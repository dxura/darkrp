using Sandbox.Citizen;

namespace Dxura.Darkrp.Player;

[Group( "Player" )]
[Title( "Player Controller" )]
public sealed class PlayerController : Component
{
	
	[Property] public CharacterController CharacterController { get; set; } = null!;
	[Property] public float CrouchMoveSpeed { get; set; } = 64.0f;
	[Property] public float WalkMoveSpeed { get; set; } = 190.0f;
	[Property] public float RunMoveSpeed { get; set; } = 190.0f;
	[Property] public float SprintMoveSpeed { get; set; } = 320.0f;

	[Property] public CameraComponent Camera { get; set; } = null!;
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; } = null!;
	
	public float EyeHeight = 64;

	public bool WishCrouch;

	[Sync] public bool Crouching { get; set; }
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public Vector3 WishVelocity { get; set; }

	private const float DuckHeight = 64 - 36;

	private RealTimeSince _lastGrounded;
	private RealTimeSince _lastJump;
	private RealTimeSince _lastUngrounded;

	private float CurrentMoveSpeed
	{
		get
		{
			if ( Crouching )
			{
				return CrouchMoveSpeed;
			}

			if ( Input.Down( "run" ) )
			{
				return SprintMoveSpeed;
			}

			if ( Input.Down( "walk" ) )
			{
				return WalkMoveSpeed;
			}

			return RunMoveSpeed;
		}
	}

	protected override void OnStart()
	{
		if ( IsProxy )
		{
			return;
		}

		// Enable sight & movement for local player authority (Controller & Camera)
		CharacterController.Enabled = true;
		AnimationHelper.Enabled = true;
		Camera.Enabled = true;
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			MouseInput();
			Transform.Rotation = new Angles( 0, EyeAngles.yaw, 0 );
		}

		UpdateAnimation();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		CrouchingInput();
		MovementInput();
	}

	private void MouseInput()
	{
		var e = EyeAngles;
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp( -90, 90 );
		e.roll = 0.0f;
		EyeAngles = e;
	}

	private float GetFriction()
	{
		return CharacterController.IsOnGround
			? 6.0f
			:
			// air friction
			0.2f;
	}

	private void MovementInput()
	{
		var cc = CharacterController;

		var halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		WishVelocity = Input.AnalogMove;

		if ( _lastGrounded < 0.2f && _lastJump > 0.3f && Input.Pressed( "jump" ) )
		{
			_lastJump = 0;
			cc.Punch( Vector3.Up * 300 );
		}

		if ( !WishVelocity.IsNearlyZero() )
		{
			WishVelocity = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation() * WishVelocity;
			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.ClampLength( 1 );
			WishVelocity *= CurrentMoveSpeed;

			if ( !cc.IsOnGround )
			{
				WishVelocity = WishVelocity.ClampLength( 50 );
			}
		}


		cc.ApplyFriction( GetFriction() );

		if ( cc.IsOnGround )
		{
			cc.Accelerate( WishVelocity );
			cc.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
		else
		{
			cc.Velocity += halfGravity;
			cc.Accelerate( WishVelocity );
		}

		cc.Move();

		if ( !cc.IsOnGround )
		{
			cc.Velocity += halfGravity;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
		}

		if ( cc.IsOnGround )
		{
			_lastGrounded = 0;
		}
		else
		{
			_lastUngrounded = 0;
		}
	}

	private bool CanUncrouch()
	{
		if ( !Crouching )
		{
			return true;
		}

		if ( _lastUngrounded < 0.2f )
		{
			return false;
		}

		var tr = CharacterController.TraceDirection( Vector3.Up * DuckHeight );
		return !tr.Hit; // hit nothing - we can!
	}

	public void CrouchingInput()
	{
		WishCrouch = Input.Down( "duck" );

		if ( WishCrouch == Crouching )
		{
			return;
		}

		// crouch
		if ( WishCrouch )
		{
			CharacterController.Height = 36;
			Crouching = WishCrouch;

			// if we're not on the ground, slide up our bbox so when we crouch
			// the bottom shrinks, instead of the top, which will mean we can reach
			// places by crouch jumping that we couldn't.
			if ( !CharacterController.IsOnGround )
			{
				CharacterController.MoveTo( Transform.Position += Vector3.Up * DuckHeight, false );
				Transform.ClearInterpolation();
				EyeHeight -= DuckHeight;
			}

			return;
		}

		// uncrouch
		if ( !WishCrouch )
		{
			if ( !CanUncrouch() )
			{
				return;
			}

			CharacterController.Height = 64;
			Crouching = WishCrouch;
		}
	}

	private void UpdateCamera()
	{
		var targetEyeHeight = Crouching ? 28 : 64;
		EyeHeight = EyeHeight.LerpTo( targetEyeHeight, RealTime.Delta * 10.0f );

		var targetCameraPos = Transform.Position + new Vector3( 0, 0, EyeHeight );

		// smooth view z, so when going up and down stairs or ducking, it's smooth af
		if ( _lastUngrounded > 0.2f )
		{
			targetCameraPos.z = Camera.Transform.Position.z.LerpTo( targetCameraPos.z, RealTime.Delta * 25.0f );
		}

		Camera.Transform.Position = targetCameraPos;
		Camera.Transform.Rotation = EyeAngles;
		Camera.FieldOfView = Preferences.FieldOfView;
	}

	protected override void OnPreRender()
	{
		UpdateBodyVisibility();

		if ( IsProxy )
		{
			return;
		}

		UpdateCamera();
	}

	private void UpdateAnimation()
	{
		var wv = WishVelocity.Length;

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( CharacterController.Velocity );
		AnimationHelper.IsGrounded = CharacterController.IsOnGround;
		AnimationHelper.DuckLevel = Crouching ? 1.0f : 0.0f;

		AnimationHelper.MoveStyle =
			wv < 160f ? CitizenAnimationHelper.MoveStyles.Walk : CitizenAnimationHelper.MoveStyles.Run;

		var lookDir = EyeAngles.ToRotation().Forward * 1024;
		AnimationHelper.WithLook( lookDir, 1, 0.5f, 0.25f );
	}

	private void UpdateBodyVisibility()
	{
		var renderMode = ModelRenderer.ShadowRenderType.On;
		if ( !IsProxy )
		{
			renderMode = ModelRenderer.ShadowRenderType.ShadowsOnly;
		}

		AnimationHelper.Target.RenderType = renderMode;

		foreach ( var clothing in AnimationHelper.Target.Components.GetAll<ModelRenderer>( FindMode.InChildren ) )
		{
			if ( !clothing.Tags.Has( "clothing" ) )
			{
				continue;
			}

			clothing.RenderType = renderMode;
		}
	}
}
