using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class DamageTracker : Component, IGameEventHandler<DamageTakenGlobalEvent>,
	IGameEventHandler<PlayerSpawnedEvent>
{
	[Property] public bool ClearBetweenRounds { get; set; } = true;
	[Property] public bool ClearOnRespawn { get; set; } = false;

	public Dictionary<PlayerState, List<DamageInfo>> Registry { get; set; } = new();
	public Dictionary<PlayerState, List<DamageInfo>> MyInflictedDamage { get; set; } = new();

	[Broadcast( NetPermission.HostOnly )]
	protected void RpcRefresh()
	{
		Refresh();
	}

	public List<DamageInfo> GetDamageOnMe()
	{
		return GetDamageInflictedTo( PlayerState.Local );
	}

	public List<DamageInfo> GetDamageInflictedTo( PlayerState player )
	{
		if ( !Registry.TryGetValue( player, out var list ) )
		{
			return new List<DamageInfo>();
		}

		return list;
	}

	public List<DamageInfo> GetMyInflictedDamage( PlayerState player )
	{
		if ( !MyInflictedDamage.TryGetValue( player, out var list ) )
		{
			return new List<DamageInfo>();
		}

		return list;
	}

	public struct GroupedDamage
	{
		public PlayerState? Attacker { get; set; }
		public int Count { get; set; }
		public float Damage { get; set; }
	}

	public List<GroupedDamage> GetGroupedDamage( PlayerState player )
	{
		var groups = new List<GroupedDamage>();

		GetDamageInflictedTo( player )
			.GroupBy( x => x.Attacker )
			.ToList()
			.ForEach( group =>
			{
				groups.Add( new GroupedDamage
				{
					Attacker = group.First().Attacker is Player attackerPlayer ? attackerPlayer.PlayerState : null,
					Count = group.Count(),
					Damage = group.Sum( x => x.Damage )
				} );
			} );


		return groups;
	}

	public List<GroupedDamage> GetGroupedInflictedDamage( PlayerState player )
	{
		var groups = new List<GroupedDamage>();

		GetMyInflictedDamage( player )
			.GroupBy( x => x.Attacker )
			.ToList()
			.ForEach( group =>
			{
				groups.Add( new GroupedDamage
				{
					Attacker = group.First().Attacker is Player attackerPlayer ? attackerPlayer.PlayerState : null,
					Count = group.Count(),
					Damage = group.Sum( x => x.Damage )
				} );
			} );


		return groups;
	}

	public void Refresh()
	{
		MyInflictedDamage.Clear();
		Registry.Clear();
	}

	void IGameEventHandler<DamageTakenGlobalEvent>.OnGameEvent( DamageTakenGlobalEvent eventArgs )
	{
		var attacker = eventArgs.DamageInfo.Attacker;
		var victim = eventArgs.DamageInfo.Victim;
		var playerState = victim is Player playerVictim ? playerVictim.PlayerState : null;

		if ( !playerState.IsValid() )
		{
			return;
		}

		var attackerPlayerState = attacker is Player attackerPlayer ? attackerPlayer.PlayerState : null;
		if ( attackerPlayerState == PlayerState.Local )
		{
			if ( !MyInflictedDamage.TryGetValue( playerState, out var myList ) )
			{
				MyInflictedDamage.Add( playerState, new List<DamageInfo> { eventArgs.DamageInfo } );
			}
			else
			{
				myList.Add( eventArgs.DamageInfo );
			}
		}

		if ( !Registry.TryGetValue( playerState, out var list ) )
		{
			Registry.Add( playerState, new List<DamageInfo> { eventArgs.DamageInfo } );
		}
		else
		{
			list.Add( eventArgs.DamageInfo );
		}
	}

	void IGameEventHandler<PlayerSpawnedEvent>.OnGameEvent( PlayerSpawnedEvent eventArgs )
	{
		if ( !ClearOnRespawn )
		{
			return;
		}

		// Only include the owner
		using ( Rpc.FilterInclude( eventArgs.Player.Network.OwnerConnection ) )
		{
			// Send the refresh
			RpcRefresh();
		}
	}
}
