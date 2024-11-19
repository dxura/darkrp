using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class DamageTracker : Component, IGameEventHandler<DamageTakenGlobalEvent>,
	IGameEventHandler<PlayerSpawnedEvent>
{
	[Property] public bool ClearBetweenRounds { get; set; } = true;
	[Property] public bool ClearOnRespawn { get; set; } = false;

	public Dictionary<Player, List<DamageInfo>> Registry { get; set; } = new();
	public Dictionary<Player, List<DamageInfo>> MyInflictedDamage { get; set; } = new();

	[Broadcast( NetPermission.HostOnly )]
	protected void RpcRefresh()
	{
		Refresh();
	}

	public List<DamageInfo> GetDamageOnMe()
	{
		return GetDamageInflictedTo( Player.Local );
	}

	public List<DamageInfo> GetDamageInflictedTo( Player player )
	{
		if ( !Registry.TryGetValue( player, out var list ) )
		{
			return new List<DamageInfo>();
		}

		return list;
	}

	public List<DamageInfo> GetMyInflictedDamage( Player player )
	{
		if ( !MyInflictedDamage.TryGetValue( player, out var list ) )
		{
			return new List<DamageInfo>();
		}

		return list;
	}

	public struct GroupedDamage
	{
		public Player? Attacker { get; set; }
		public int Count { get; set; }
		public float Damage { get; set; }
	}

	public List<GroupedDamage> GetGroupedDamage( Player player )
	{
		var groups = new List<GroupedDamage>();

		GetDamageInflictedTo( player )
			.GroupBy( x => x.Attacker )
			.ToList()
			.ForEach( group =>
			{
				groups.Add( new GroupedDamage
				{
					Attacker = group.First().Attacker is Player attackerPlayer ? attackerPlayer : null,
					Count = group.Count(),
					Damage = group.Sum( x => x.Damage )
				} );
			} );


		return groups;
	}

	public List<GroupedDamage> GetGroupedInflictedDamage( Player player )
	{
		var groups = new List<GroupedDamage>();

		GetMyInflictedDamage( player )
			.GroupBy( x => x.Attacker )
			.ToList()
			.ForEach( group =>
			{
				groups.Add( new GroupedDamage
				{
					Attacker = group.First().Attacker is Player attackerPlayer ? attackerPlayer : null,
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
		var player = victim as Player;

		if ( !player.IsValid() )
		{
			return;
		}

		var attackerPlayer = attacker as Player;
		if ( attackerPlayer == Player.Local )
		{
			if ( !MyInflictedDamage.TryGetValue( player, out var myList ) )
			{
				MyInflictedDamage.Add( player, new List<DamageInfo> { eventArgs.DamageInfo } );
			}
			else
			{
				myList.Add( eventArgs.DamageInfo );
			}
		}

		if ( !Registry.TryGetValue( player, out var list ) )
		{
			Registry.Add( player, new List<DamageInfo> { eventArgs.DamageInfo } );
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
