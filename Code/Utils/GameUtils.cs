using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using GameSystems.Jobs;

namespace Dxura.Darkrp;

public interface IWeighted
{
	float Weight { get; }
}

/// <summary>
/// A list of game utilities that'll help us achieve common goals with less code... I guess?
/// </summary>
public static class GameUtils
{
	/// <summary>
	/// All players in the game (includes disconnected players before expiration).
	/// </summary>
	public static IEnumerable<Player> AllPlayers => Game.ActiveScene.GetAllComponents<Player>();

	/// <summary>
	/// Get all players on a team.
	/// </summary>
	public static IEnumerable<Player> GetPlayers( JobResource job )
	{
		return AllPlayers.Where( x => x.Job == job );
	}

	/// <summary>
	/// Every <see cref="Player"/> currently in the world.
	/// </summary>
	public static IEnumerable<Player> Players =>
		AllPlayers.Where( x => x.IsValid() );

	/// <summary>
	/// Every <see cref="Player"/> currently in the world, on the given job.
	/// </summary>
	public static IEnumerable<Player> GetPlayersByJob( JobResource job )
	{
		return Players.Where( x => x.Job == job );
	}

	public static IDescription? GetDescription( GameObject go )
	{
		return go.Components.Get<IDescription?>( FindMode.EverythingInSelfAndDescendants );
	}

	public static IDescription? GetDescription( Component? component )
	{
		return component == null ? null : GetDescription( component.GameObject );
	}

	/// <summary>
	/// Get a player from a component that belongs to a player or their descendants.
	/// </summary>
	public static Player? GetPlayerFromComponent( Component component )
	{
		if ( component is Player player )
		{
			return player;
		}

		if ( !component.IsValid() )
		{
			return null;
		}

		return !component.GameObject.IsValid()
			? null
			: component.GameObject.Root.Components.Get<Player>( FindMode.EnabledInSelfAndDescendants );
	}

	/// <summary>
	/// Get a player from a component that belongs to a player or their descendants.
	/// </summary>
	public static Player? GetPlayer( Component component )
	{
		if ( component is Player player )
		{
			return player;
		}

		if ( !component.IsValid() )
		{
			return null;
		}

		return !component.GameObject.IsValid()
			? null
			: component.GameObject.Root.Components.Get<Player>( FindMode.EnabledInSelfAndDescendants );
	}

	public static Equipment? FindEquipment( Component inflictor )
	{
		if ( inflictor is Equipment equipment )
		{
			return equipment;
		}

		return null;
	}
}
