using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using GameSystems.Jobs;

namespace Dxura.Darkrp;

/// <summary>
/// Some network utils, mainly RPC filters.
/// </summary>
public class NetworkUtils
{
	/// <summary>
	/// Makes a RPC filter based on your team.
	/// </summary>
	/// <returns></returns>
	public static IDisposable RpcMyJob()
	{
		return RpcJob( PlayerState.Local.Job );
	}

	/// <summary>
	/// Makes a RPC filter to a specific job.
	/// </summary>
	/// <param name="job"></param>
	/// <returns></returns>
	public static IDisposable RpcJob( JobResource job )
	{
		return Rpc.FilterInclude(
			GameUtils.GetPlayers( job )
				.Select( x => x.Connection ) );
	}
}
