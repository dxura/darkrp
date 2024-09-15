using Dxura.Darkrp.UI;
using Sandbox;
using GameSystems.Jobs;
namespace Dxura.Darkrp;

[Title("Rock")]
[Category("Entities")]
public sealed class Rock1Entity : BaseEntity
{
    [Property] public RockResource CurrentRockResource { get; set; } = null!;
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
        GameObject.Destroy();
    }
}
