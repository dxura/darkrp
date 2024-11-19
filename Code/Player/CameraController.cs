using Dxura.Darkrp.UI;
using Sandbox.Events;

namespace Dxura.Darkrp;

public enum CameraMode
{
	FirstPerson,
	ThirdPerson
}

public sealed class CameraController : Component, IGameEventHandler<DamageTakenEvent>
{
	[Property] public Player Player { get; set; } = null!;

	[Property] [Group( "Config" )] public bool ShouldViewBob { get; set; } = true;
	[Property] [Group( "Config" )] public float RespawnProtectionSaturation { get; set; } = 0.25f;

	[Property] public float ThirdPersonDistance { get; set; } = 128f;
	[Property] public float AimFovOffset { get; set; } = -5f;

	private CameraMode _mode;

	public CameraMode Mode
	{
		get => _mode;
		set
		{
			if ( _mode == value )
			{
				return;
			}

			_mode = value;
			OnModeChanged();
		}
	}
	
	// These components are cached.
	public CameraComponent Camera { get; set; } = null!;
	public AudioListener AudioListener { get; set; } = null!;
	public ColorAdjustments ColorAdjustments { get; set; } = null!;
	public ScreenShaker ScreenShaker { get; set; } = null!;
	public ChromaticAberration ChromaticAberration { get; set; } = null!;
	public Pixelate Pixelate { get; set; } = null!;

	/// <summary>
	/// The boom for this camera.
	/// </summary>
	[Property]
	public GameObject Boom { get; set; } = null!;

	public float MaxBoomLength { get; set; }

	/// <summary>
	/// Constructs a ray using the camera's GameObject
	/// </summary>
	public Ray AimRay
	{
		get
		{
			if ( Camera.IsValid() )
			{
				return new Ray( Camera.WorldPosition + Camera.WorldRotation.Forward,
					Camera.WorldRotation.Forward );
			}

			return new Ray( WorldPosition + Vector3.Up * 64f, Player.EyeAngles.ToRotation().Forward );
		}
	}

	protected override void OnStart()
	{
		Camera = Scene.Camera;
		Camera.GameObject.SetParent(Boom);
		
		Pixelate = Camera.GameObject.Components.GetOrCreate<Pixelate>();
		ChromaticAberration =  Camera.GameObject.Components.GetOrCreate<ChromaticAberration>();
		AudioListener =  Camera.GameObject.Components.GetOrCreate<AudioListener>();
		ScreenShaker =  Camera.GameObject.Components.GetOrCreate<ScreenShaker>();

		// Optional
		ColorAdjustments =  Camera.GameObject.Components.Get<ColorAdjustments>();
		
		OnModeChanged();
		Boom.WorldRotation = Player.EyeAngles.ToRotation();

	}

	private float _fieldOfViewOffset = 0f;
	private float _targetFieldOfView = 90f;

	public void AddFieldOfViewOffset( float degrees )
	{
		_fieldOfViewOffset -= degrees;
	}

	private void UpdateRotation()
	{
		if ( Player.IsInVehicle )
		{
			Boom.Transform.Local = Boom.Transform.Local.WithRotation( Player.EyeAngles.ToRotation() );
		}
		else
		{
			Boom.WorldRotation = Player.EyeAngles.ToRotation();
		}
	}

	/// <summary>
	/// Updates the camera's position, from player code
	/// </summary>
	/// <param name="eyeHeight"></param>
	internal void UpdateFromEyes( float eyeHeight )
	{
		if ( !Camera.IsValid() )
		{
			return;
		}

		// All transform effects are additive to camera local position, so we need to reset it before anything is applied
		Camera.LocalPosition = Vector3.Zero;
		Camera.LocalRotation = Rotation.Identity;

		if ( Mode == CameraMode.ThirdPerson && Player.IsProxy )
		{
			// orbit cam: spectating only
			var angles = Boom.WorldRotation.Angles();
			angles += Input.AnalogLook;
			Boom.WorldRotation = angles.WithPitch( angles.pitch.Clamp( -90, 90 ) ).ToRotation();
		}
		else
		{
			UpdateRotation();
		}

		if ( MaxBoomLength > 0 )
		{
			var tr = Scene.Trace.Ray( new Ray( Boom.WorldPosition, Boom.WorldRotation.Backward ),
					MaxBoomLength )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.WithoutTags( "trigger", "player", "ragdoll" )
				.Run();

			Camera.LocalPosition = Vector3.Backward * (tr.Hit ? tr.Distance - 5.0f : MaxBoomLength);
		}

		if ( ShouldViewBob )
		{
			ViewBob();
		}

		Update( eyeHeight );
	}

	private float _walkBob;
	private float _lerpBobSpeed;

	[DeveloperCommand( "Toggle Third Person", "Player" )]
	public static void ToggleThirdPerson()
	{
		var pl = Player.Local;
		pl.CameraController.Mode = pl.CameraController.Mode == CameraMode.FirstPerson
			? CameraMode.ThirdPerson
			: CameraMode.FirstPerson;
	}

	/// <summary>
	/// Bob the view!
	/// This could be better, but it doesn't matter really.
	/// </summary>
	private void ViewBob()
	{
		if ( Mode != CameraMode.FirstPerson )
		{
			return;
		}

		var bobSpeed = Player.CharacterController.Velocity.Length.LerpInverse( 0, 300 );
		if ( !Player.IsGrounded )
		{
			bobSpeed *= 0.1f;
		}

		if ( !Player.IsSprinting )
		{
			bobSpeed *= 0.3f;
		}

		_lerpBobSpeed = _lerpBobSpeed.LerpTo( bobSpeed, Time.Delta * 10f );

		_walkBob += Time.Delta * 10.0f * _lerpBobSpeed;
		var yaw = MathF.Sin( _walkBob ) * 0.5f;
		var pitch = MathF.Cos( -_walkBob * 2f ) * 0.5f;

		Boom.LocalRotation *= Rotation.FromYaw( -yaw * _lerpBobSpeed );
		Boom.LocalRotation *= Rotation.FromPitch( -pitch * _lerpBobSpeed * 0.5f );
	}

	private void ApplyScope()
	{
		if ( !Player.CurrentEquipment.IsValid() )
		{
			return;
		}

		if ( Player?.CurrentEquipment?.Components.Get<ScopeWeaponComponent>( FindMode.EnabledInSelfAndDescendants ) is
		    { } scope )
		{
			var fov = scope.GetFOV();
			_fieldOfViewOffset -= fov;
		}
	}

	private bool _fetchedInitial = false;
	private float _defaultSaturation = 1f;

	private void Update( float eyeHeight )
	{
		var baseFov = GameSettingsSystem.Current.FieldOfView;
		_fieldOfViewOffset = 0;

		if ( !Player.IsValid() )
		{
			return;
		}

		if ( Player.CurrentEquipment.IsValid() )
		{
			if ( Player.CurrentEquipment?.Tags.Has( "aiming" ) ?? false )
			{
				_fieldOfViewOffset += AimFovOffset;
			}
		}

		// deathcam, "zoom" at target.
		if ( Player.HealthComponent.State == LifeState.Dead )
		{
			_fieldOfViewOffset += AimFovOffset;
		}

		if ( ColorAdjustments.IsValid() )
		{
			if ( !_fetchedInitial )
			{
				_defaultSaturation = ColorAdjustments.Saturation;
				_fetchedInitial = true;
			}

			ColorAdjustments.Saturation = Player.HealthComponent.IsGodMode
				? RespawnProtectionSaturation
				: ColorAdjustments.Saturation.MoveToLinear( _defaultSaturation, 1f );
		}

		ApplyRecoil();
		ApplyScope();

		Boom.LocalPosition = Vector3.Zero.WithZ( eyeHeight );

		ApplyCameraEffects();
		ScreenShaker?.Apply( Camera );

		_targetFieldOfView = _targetFieldOfView.LerpTo( baseFov + _fieldOfViewOffset, Time.Delta * 5f );
		Camera.FieldOfView = _targetFieldOfView;
	}

	private RealTimeSince _timeSinceDamageTaken = 1;

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		_timeSinceDamageTaken = 0;
	}

	private void ApplyCameraEffects()
	{
		var timeSinceDamage = _timeSinceDamageTaken.Relative;
		var shortDamageUi = timeSinceDamage.LerpInverse( 0.1f, 0.0f, true );
		ChromaticAberration.Scale = shortDamageUi * 1f;
		Pixelate.Scale = shortDamageUi * 0.2f;
	}

	private void ApplyRecoil()
	{
		if ( !Player.CurrentEquipment.IsValid() )
		{
			return;
		}

		if ( Player.CurrentEquipment?.Components.Get<RecoilWeaponComponent>( FindMode.EnabledInSelfAndDescendants ) is
		    { } fn )
		{
			Player.EyeAngles += fn.Current;
		}
	}

	private void OnModeChanged()
	{
		SetBoomLength( Mode == CameraMode.FirstPerson ? 0.0f : ThirdPersonDistance );

		var firstPersonPov = Mode == CameraMode.FirstPerson && !IsProxy;
		
		Player.SetFirstPersonView( firstPersonPov );

		if ( firstPersonPov )
		{
			Player.CreateViewModel( false );
		}
		else
		{
			Player.ClearViewModel();
		}
	}

	private void SetBoomLength( float length )
	{
		MaxBoomLength = length;
	}
}
