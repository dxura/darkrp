using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Dxura.Darkrp;
using Dxura.Darkrp.UI;
using Sandbox.Events;

namespace Dxura.Darkrp;

public enum CameraMode
{
	FirstPerson,
	ThirdPerson
}
public partial class Player
{
	
	/// <summary>
	/// Get a quick reference to the real Camera GameObject.
	/// </summary>
	public GameObject? CameraGameObject => Camera?.GameObject;
	
	/// <summary>
	/// Should camera be locked
	/// </summary>
	[Property, Feature("Camera")] public bool LockCamera { get; set; }
	
	[Property, Feature("Camera"), Group("Config")] public bool ShouldViewBob { get; set; } = true;
	[Property, Feature("Camera"), Group("Config")] public float RespawnProtectionSaturation { get; set; } = 0.35f;

	[Property, Feature("Camera"), Group("Config")] public float ThirdPersonDistance { get; set; } = 128f;
	[Property, Feature("Camera"), Group("Config")] public float AimFovOffset { get; set; } = -5f;

	private CameraMode _cameraMode;

	public CameraMode CameraMode
	{
		get => _cameraMode;
		set
		{
			if ( _cameraMode == value )
			{
				return;
			}

			_cameraMode = value;
			OnModeChanged();
		}
	}
	
	// These components are cached.
	public CameraComponent? Camera { get; set; }
	public AudioListener? AudioListener { get; set; }
	private ColorAdjustments? ColorAdjustments { get; set; }
	private ScreenShaker? ScreenShaker { get; set; }
	private ChromaticAberration? ChromaticAberration { get; set; }
	private Pixelate? Pixelate { get; set; }

	/// <summary>
	/// The boom for this camera.
	/// </summary>
	[Property, Feature("Camera")]
	private GameObject Boom { get; set; } = null!;

	public float MaxBoomLength { get; set; }
	private float _fieldOfViewOffset = 0f;
	private float _targetFieldOfView = 90f;

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

			return new Ray( WorldPosition + Vector3.Up * 64f, EyeAngles.ToRotation().Forward );
		}
	}

	private void OnStartCamera()
	{
		OnModeChanged();
		Boom.WorldRotation = EyeAngles.ToRotation();
		
		// Cache if local player
		if (!IsProxy)
		{
			Camera = Scene.Camera;
			Camera.GameObject.SetParent(Boom);
		
			Pixelate = Camera.GameObject.Components.GetOrCreate<Pixelate>();
			ChromaticAberration =  Camera.GameObject.Components.GetOrCreate<ChromaticAberration>();
			AudioListener =  Camera.GameObject.Components.GetOrCreate<AudioListener>();
			ScreenShaker =  Camera.GameObject.Components.GetOrCreate<ScreenShaker>();

			// Optional
			ColorAdjustments = Camera.GameObject.Components.Get<ColorAdjustments>();
		}
	}
	
	private void OnUpdateCamera()
	{
		if (IsProxy )
		{
			return;
		}

		// Deathcam
		if (HealthComponent.State == LifeState.Dead)
		{
			if ( LastDamageInfo is null )
			{
				return;
			}

			var killer = GetLastKiller();

			if ( killer.IsValid() )
			{
				EyeAngles = Rotation.LookAt( killer.WorldPosition - WorldPosition, Vector3.Up );
			}
		}

	}

	public void AddFieldOfViewOffset( float degrees )
	{
		_fieldOfViewOffset -= degrees;
	}

	private void UpdateRotation()
	{
		if ( IsSeated )
		{
			Boom.Transform.Local = Boom.Transform.Local.WithRotation( EyeAngles.ToRotation() );
		}
		else
		{
			Boom.WorldRotation = EyeAngles.ToRotation();
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

		if ( CameraMode == CameraMode.ThirdPerson && IsProxy )
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
		Local.CameraMode = Local.CameraMode == CameraMode.FirstPerson
			? CameraMode.ThirdPerson
			: CameraMode.FirstPerson;
	}

	/// <summary>
	/// Bob the view!
	/// This could be better, but it doesn't matter really.
	/// </summary>
	private void ViewBob()
	{
		if ( CameraMode != CameraMode.FirstPerson )
		{
			return;
		}

		var bobSpeed = Velocity.Length.LerpInverse( 0, 300 );
		if ( !IsGrounded )
		{
			bobSpeed *= 0.1f;
		}

		if ( !IsSprinting )
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
		if ( !CurrentEquipment.IsValid() )
		{
			return;
		}

		if ( CurrentEquipment?.Components.Get<ScopeWeaponComponent>( FindMode.EnabledInSelfAndDescendants ) is { } scope )
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

		if ( !IsValid )
		{
			return;
		}

		if ( CurrentEquipment.IsValid() )
		{
			if ( CurrentEquipment?.Tags.Has( "aiming" ) ?? false )
			{
				_fieldOfViewOffset += AimFovOffset;
			}
		}

		// death cam, "zoom" at target.
		if ( HealthComponent.State == LifeState.Dead )
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

			ColorAdjustments.Saturation = HealthComponent.IsGodMode
				? RespawnProtectionSaturation
				: ColorAdjustments.Saturation.MoveToLinear( _defaultSaturation, 1f );
		}

		ApplyRecoil();
		ApplyScope();

		Boom.LocalPosition = Vector3.Zero.WithZ( eyeHeight );

		var timeSinceDamage = _timeSinceDamageTaken.Relative;
		var shortDamageUi = timeSinceDamage.LerpInverse( 0.1f, 0.0f, true );

		if (Camera.IsValid() && ChromaticAberration != null && Pixelate != null)
		{
			ChromaticAberration.Scale = shortDamageUi * 1f;
			Pixelate.Scale = shortDamageUi * 0.2f;
			ScreenShaker?.Apply(Camera);

			_targetFieldOfView = _targetFieldOfView.LerpTo(baseFov + _fieldOfViewOffset, Time.Delta * 5f);
			Camera.FieldOfView = _targetFieldOfView;
		}
	}

	private void ApplyRecoil()
	{
		if ( !CurrentEquipment.IsValid() )
		{
			return;
		}

		if ( CurrentEquipment?.Components.Get<RecoilWeaponComponent>( FindMode.EnabledInSelfAndDescendants ) is
		    { } fn )
		{
			EyeAngles += fn.Current;
		}
	}

	private void OnModeChanged()
	{
		SetBoomLength( CameraMode == CameraMode.FirstPerson ? 0.0f : ThirdPersonDistance );

		var firstPersonPov = CameraMode == CameraMode.FirstPerson && !IsProxy;
		
		SetFirstPersonView( firstPersonPov );

		if ( firstPersonPov )
		{
			CreateViewModel( false );
		}
		else
		{
			ClearViewModel();
		}
	}

	private void SetBoomLength( float length )
	{
		MaxBoomLength = length;
	}
	
}
