namespace Dxura.Darkrp;

public class PlayerFlashlight : Component
{
	[Property] private readonly Player _player = null!;
	
	[Property] private readonly SpotLight _light = null!;
	[Property] private readonly SoundPointComponent _soundPoint = null!;
	
	protected override void OnFixedUpdate()
	{
		if ( Input.Pressed( "Flashlight" ) )
		{
			ToggleFlashlight();
		}
	}

	protected override void OnUpdate()
	{
		if ( _light.Enabled )
		{
			UpdateCameraTilt();
		}
	}
	
	[Broadcast(NetPermission.OwnerOnly)]
	public void ToggleFlashlight()
	{
		// Inverts the state of the light
		_light.Enabled = !_light.Enabled;

		// Play the click click sound
		_soundPoint.StartSound();
	}
	
	private void UpdateCameraTilt()
	{
		_light.Transform.Rotation = _player.EyeAngles.ToRotation();
	}

}
