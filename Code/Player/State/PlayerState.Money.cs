using Dxura.Darkrp;
using Sandbox.Diagnostics;

namespace Dxura.Darkrp;

public partial class PlayerState : IScore
{
	/// <summary>
	/// Players current cash balance
	/// </summary>
	[HostSync]
	[Order( -100 )]
	[Score( "Balance", Format = "${0:N0}" )]
	public int Balance { get; set; } = 16_000;

	public void SetCash( int amount )
	{
		using var _ = Rpc.FilterInclude( Connection.Host );
		SetCashHost( amount );
	}

	[Broadcast]
	private void SetCashHost( int amount )
	{
		Assert.True( Networking.IsHost );
		Balance = amount;
	}

	public void GiveCash( int amount )
	{
		using var _ = Rpc.FilterInclude( Connection.Host );
		GiveCashHost( amount );
	}

	[Broadcast]
	private void GiveCashHost( int amount )
	{
		Assert.True( Networking.IsHost );
		Balance += amount;
	}
}
