using Dxura.Darkrp.UI;
using System.Threading.Tasks;
namespace Dxura.Darkrp;

[Title("Rock")]
[Category("Entities")]
public sealed class Rock1Entity : BaseEntity
{
    [Property] public RockResource CurrentRockResource { get; set; } = null!;
    private int _respawnTime = 2;
    private float _givenRocks = 0;

    /// The player
	public Player Player => PlayerState.IsValid() ? PlayerState.Player : null;
    public PlayerState PlayerState => PlayerState.Local;

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
        _givenRocks = CurrentRockResource.GivenRocks;

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
        Toast.Instance?.Show("You gained " + _givenRocks + " rocks" , ToastType.Error);
        GiveOre();
        _ = RespawningRock();
        GameObject.Enabled = false;

    }

    private void GiveOre()
    {
        PlayerState.RockCount = PlayerState.RockCount + (int)_givenRocks;
        Log.Info(PlayerState.DisplayName + " has gained " + _givenRocks + " rocks" + " and has " + PlayerState.RockCount + " rocks in total");

    }
    async Task RespawningRock()
    {
        // wait for this amount of seconds
        await GameTask.DelayRealtimeSeconds(_respawnTime);


        GameObject.Clone(Transform.Position);
        GameObject.Destroy();

    }
}
