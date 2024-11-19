using System.Text.Json.Serialization;
using Dxura.Darkrp;

namespace Dxura.Darkrp;

public partial class Player
{
	[Property, Feature("Presence"), Group("Footsteps")] private float FootstepBaseVolume { get; set; } = 5f;
	[Property, Feature("Presence"), Group("Footsteps")] private float FootstepScale { get; set; } = 1f;
	[Property, Feature("Presence"), Group("Footsteps")] private float SprintFootstepScale { get; set; } = 2f;
	
	[Property, Feature("Presence"), Group("Torch")] private readonly SpotLight _torchSpotLight = null!;
	[Property, Feature("Presence"), Group("Torch")] private readonly SoundEvent _torchToggleSound = null!;

	private TimeSince _timeSinceStep;

	private bool _flipFlop;

	private float GetStepFrequency()
	{
		if ( IsSprinting )
		{
			return 0.25f;
		}

		return 0.34f;
	}

	private void Footstep()
	{
		// Don't make footsteps sometimes
		if ( IsCrouching || IsSlowWalking )
		{
			return;
		}

		var tr = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 20, WorldPosition + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
		{
			return;
		}

		if ( tr.Surface is null )
		{
			return;
		}

		_timeSinceStep = 0;

		_flipFlop = !_flipFlop;

		var sound = _flipFlop ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;
		if ( sound is null )
		{
			return;
		}

		var scale = IsSprinting ? SprintFootstepScale : FootstepScale;
		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		if ( !handle.IsValid() )
		{
			return;
		}

		handle.Occlusion = false;
		handle.Volume = FootstepBaseVolume * scale;
		handle.ListenLocal = !IsProxy;
	}

	private void OnFixedUpdatePresence()
	{
		if ( !IsValid || IsInVehicle || HealthComponent.State != LifeState.Alive )
		{
			return;
		}
		
		
		if ( _timeSinceStep >= GetStepFrequency() && CharacterController.Velocity.Length > 50f )
		{
			Footstep();
		}

		
		// Flashlight logic (Is local only)
		if (!IsProxy)
		{
			if ( Input.Pressed( "Flashlight" ) )
			{
				_torchSpotLight.Enabled = !_torchSpotLight.Enabled;
				Sound.Play( _torchToggleSound, WorldPosition );
			}
			
			if (_torchSpotLight.Enabled)
			{
				_torchSpotLight.WorldRotation = EyeAngles.ToRotation();
			}

		}

	}
	
}
