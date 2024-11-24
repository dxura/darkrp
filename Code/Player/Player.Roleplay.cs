using System.Text.Json.Serialization;
using Sandbox.Diagnostics;

namespace Dxura.Darkrp;

public partial class Player
{
    /// <summary>
    /// Players current cash balance
    /// </summary>
    [HostSync]
    [Order( -100 )]
    public int Balance { get; set; } = 1000;
    
    [HostSync] [Property, Feature("Misc")] [JsonIgnore] public PlayerSeat? CurrentSeat { get; set; }
    public TimeSince TimeSinceSeatChanged { get; set; } = 0;
    public bool IsSeated => CurrentSeat.IsValid();
    
    private void OnFixedUpdateRoleplay()
    {
        if (IsProxy || HealthComponent.State != LifeState.Alive)
        {
            return;
        }
        
        // Update seat
        if ( CurrentSeat.IsValid() )
        {
            if ( CurrentSeat.HasInput )
            {
                CurrentSeat?.Vehicle?.InputState.UpdateFromLocal();
            }

            if ( Input.Pressed( "Use" ) )
            {
                // Try leaving the seat
                CurrentSeat?.Leave( this );
            }
        }
    }

    public void SetBalance( int amount )
    {
        using var _ = Rpc.FilterInclude( Connection.Host );
        SetBalanceHost( amount );
    }

    [Broadcast]
    private void SetBalanceHost( int amount )
    {
        Assert.True( Networking.IsHost );
        Balance = amount;
    }

    public void GiveMoney( int amount )
    {
        using var _ = Rpc.FilterInclude( Connection.Host );
        GiveMoneyHost( amount );
    }

    [Broadcast]
    private void GiveMoneyHost( int amount )
    {
        Assert.True( Networking.IsHost );
        Balance += amount;
    }


}
