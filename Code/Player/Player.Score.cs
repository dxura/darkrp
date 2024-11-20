using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Dxura.Darkrp;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class Player : IGameEventHandler<KillEvent>
{
	[HostSync]
	[Property, Group("Score")]
	[ReadOnly]
	public int Kills { get; set; } = 0;

	[HostSync]
	[Property, Group("Score")]
	[ReadOnly]
	public int Deaths { get; set; } = 0;

	void IGameEventHandler<KillEvent>.OnGameEvent( KillEvent eventArgs )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		var damageInfo = eventArgs.DamageInfo;

		if ( !damageInfo.Attacker.IsValid() )
		{
			return;
		}

		if ( !damageInfo.Victim.IsValid() )
		{
			return;
		}

		if ( !IsValid )
		{
			return;
		}

		var killerPlayer = GameUtils.GetPlayerFromComponent( damageInfo.Attacker );
		var victimPlayer = GameUtils.GetPlayerFromComponent( damageInfo.Victim );

		if ( !victimPlayer.IsValid() )
		{
			return;
		}

		if ( !killerPlayer.IsValid() )
		{
			if ( victimPlayer == this )
			{
				Deaths++;
			}

			return;
		}

		var isFriendly = killerPlayer.Job == victimPlayer.Job;
		var isSuicide = killerPlayer == victimPlayer;

		if ( killerPlayer == this )
		{
			if ( isFriendly )
			{
				// Killed by friendly/teammate
				Kills--;
			}
			else if ( isSuicide )
			{
				// Killed by suicide
				Kills--;
			}
			else
			{
				// Valid kill, add score
				Kills++;
			}
		}
		else if ( victimPlayer == this )
		{
			// Only count as death if this wasn't a team kill
			if ( !isFriendly )
			{
				Deaths++;
			}
		}
	}
}
