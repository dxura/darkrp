using Dxura.Darkrp.UI;
using Sandbox;
using GameSystems.Jobs;
using System.Threading.Tasks;
namespace Dxura.Darkrp;

[Title("Rock")]
[Category("Entities")]
public sealed class Rock1Entity : BaseEntity
{
    [Property] public RockResource CurrentRockResource { get; set; } = null!;
    private int _respawnTime = 2;
    protected override void OnStart()
    {
        if (HealthComponent == null)
        {
            Log.Error("HealthComponent is null.");
            return;
        }

        if (CurrentRockResource == null)
        {
            Log.Error("RockResource is null.");
            return;
        }

        HealthComponent.Health = CurrentRockResource.Health;
        GameObject.Components.GetInChildren<ModelRenderer>().Model = CurrentRockResource.Model;
        _respawnTime = CurrentRockResource.RespawnTime;


    }

    protected override void OnUpdate()
    {
        if (HealthComponent != null && HealthComponent.Health <= 0)
        {
            DestroyRock();
        }

    }

    private void DestroyRock()
    {
        Sound.Play("kill_sound");
        Toast.Instance?.Show("Rock destroyed", ToastType.Error);
        _ = RespawningRock();
        GameObject.Enabled = false;
        
    }

    async Task RespawningRock()
    {
        // wait for this amount of seconds
	    await GameTask.DelayRealtimeSeconds( _respawnTime );

        
        GameObject.Clone(Transform.Position);
        GameObject.Destroy();

    }
}
