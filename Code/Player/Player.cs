namespace Dxura.Darkrp;

public sealed partial class Player : Component
{
	
	protected override void OnStart()
	{
		GameObject.Name = $"Player ({DisplayName})";

		// Set Local reference if this is our player
		if (!IsProxy)
		{
			Local = this;
		}
		
		OnStartCamera();
		OnStartController();
	}

	protected override void OnUpdate()
	{
		OnUpdateEquipment();
		OnUpdateCamera();
		OnUpdateController();
	}
	
	protected override void OnFixedUpdate()
	{
		OnFixedUpdatePresence();
		OnFixedUpdateEffects();
		
		OnFixedUpdateController();
		OnFixedUpdateUsing();
		OnFixedUpdateEquipment();
		OnFixedUpdateRoleplay();
	}
}
