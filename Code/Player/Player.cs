namespace Dxura.Darkrp;

public sealed partial class Player : Component
{
	
	protected override void OnStart()
	{
		GameObject.Name = $"Player ({DisplayName})";
		
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
