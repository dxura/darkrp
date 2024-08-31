using Dxura.Darkrp;
using Dxura.Darkrp;
using GameSystems.Jobs;
using Sandbox.Events;

namespace Dxura.Darkrp;

public sealed class HumanOutfitter : Component,
	IGameEventHandler<JobChangedEvent>
{
	[Property] public Player Player { get; set; } = null!;
	[Property] public SkinnedModelRenderer Renderer { get; set; } = null!;
	
	void IGameEventHandler<JobChangedEvent>.OnGameEvent( JobChangedEvent eventArgs )
	{
		UpdateFromJob( eventArgs.After );
	}

	/// <summary>
	/// Called to wear an outfit based off a job.
	/// </summary>
	/// <param name="job"></param>
	[Broadcast( NetPermission.HostOnly )]
	public void UpdateFromJob( JobResource job )
	{
		Renderer.Model = Game.Random.FromList( job.Models );
		Player.Body?.Refresh();
	}
}
