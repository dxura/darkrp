using Dxura.Darkrp.UI;
using Sandbox;
using GameSystems.Jobs;
namespace Dxura.Darkrp;

[Title( "Rock" )]
[Category( "Entities" )]
public sealed class Rock1Entity : BaseEntity
{
    protected override void OnStart()
    {

    }

    protected override void OnUpdate()
    {
        if(HealthComponent != null && HealthComponent.Health <= 0)
        {
            DestroyRock();
        }
        
    }

    private void DestroyRock()
    {
        Sound.Play( "kill_sound" );
        GameObject.Destroy();
    }
}
