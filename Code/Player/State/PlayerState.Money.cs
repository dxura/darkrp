using SandbankDatabase;
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
