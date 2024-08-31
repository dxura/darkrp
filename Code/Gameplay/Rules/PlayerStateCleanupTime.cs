using Dxura.Darkrp;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class PlayerStateCleanupTime : Component, IGameEventHandler<EnterStateEvent>
{
	[Property] public float CleanupTime { get; set; } = 120f;

	void IGameEventHandler<EnterStateEvent>.OnGameEvent( EnterStateEvent eventArgs )
	{
		PlayerState.DisconnectCleanupTime = CleanupTime;
	}
}
