using GameSystems.Jobs;
using SandbankDatabase;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class Player
{
    /// <summary>
    /// Players current cash balance
    /// </summary>
    [HostSync]
    [Order( -100 )]
    [Saved]
    public int Balance { get; set; } = 1000;

    public void SetBalance( int amount )
    {
        using var _ = Rpc.FilterInclude( Connection.Host );
        SetBalanceHost( amount );
        Save();
    }

    [Broadcast]
    private void SetBalanceHost( int amount )
    {
        Assert.True( Networking.IsHost );
        Balance = amount;
        Save();
    }

    public void GiveMoney( int amount )
    {
        using var _ = Rpc.FilterInclude( Connection.Host );
        GiveMoneyHost( amount );
        Save();
    }

    [Broadcast]
    private void GiveMoneyHost( int amount )
    {
        Assert.True( Networking.IsHost );
        Balance += amount;
        Save();
    }
}