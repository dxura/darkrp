using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Sandbox.Events;

namespace Dxura.Darkrp;

/// <summary>
/// Plop this on something you're using <see cref="ScoreAttribute"/> for. We could codegen this attribute on components that use it to save this hassle.
/// </summary>
public interface IScore
{
	/// <summary>
	/// Looks for a bunch of score attributes from components on a <see cref="PlayerState"/>, and returns a formatted, sorted list of values.
	/// </summary>
	/// <param name="playerState"></param>
	/// <returns></returns>
	public static IEnumerable<(object Value, ScoreAttribute Attribute)> Find( PlayerState playerState )
	{
		var components = playerState.Components.GetAll<IScore>( FindMode.EnabledInSelfAndDescendants );
		var values = new List<(object Value, MemberDescription Member, ScoreAttribute Attribute)>();

		foreach ( var comp in components )
		{
			var type = TypeLibrary.GetType( comp.GetType() );

			foreach ( var member in type.Members )
			{
				if ( member.GetCustomAttribute<ScoreAttribute>() is not { } scoreAttribute )
				{
					continue;
				}

				// Support ShowIf, which looks for a method with a boolean return to see if we can display a value
				var show = type.GetMethod( scoreAttribute.ShowIf )?.InvokeWithReturn<bool>( comp, null ) ?? true;
				if ( !show )
				{
					continue;
				}

				// Support special formatting values
				values.Add( (
					string.Format( scoreAttribute.Format, type.GetValue( comp, member.Name ) ),
					member, scoreAttribute
				) );
			}
		}

		return values.OrderBy( x => x.Member.GetCustomAttribute<OrderAttribute>()?.Value ?? 0 )
			// We don't need to expose x.Member
			.Select( x => (x.Value, x.Attribute) );
	}
}

[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
public class ScoreAttribute : Attribute
{
	public string Name { get; set; }
	public string Format { get; set; } = "{0}";
	public string ShowIf { get; set; } = null;

	public ScoreAttribute( string name )
	{
		Name = name;
	}
}

/// <summary>
/// Handles all the player score values.
/// </summary>
public sealed class PlayerScore : Component,
	IGameEventHandler<KillEvent>,
	IScore
{
	[Property] public PlayerState PlayerState { get; set; }

	[HostSync]
	[Property]
	[ReadOnly]
	[Score( "Kills" )]
	public int Kills { get; set; } = 0;

	[HostSync]
	[Property]
	[ReadOnly]
	[Score( "Deaths" )]
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

		var thisPlayer = PlayerState?.Player;
		if ( !thisPlayer.IsValid() )
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
			if ( victimPlayer == thisPlayer )
			{
				Deaths++;
			}

			return;
		}

		var isFriendly = killerPlayer.Job == victimPlayer.Job;
		var isSuicide = killerPlayer == victimPlayer;

		if ( killerPlayer == thisPlayer )
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
		else if ( victimPlayer == thisPlayer )
		{
			// Only count as death if this wasn't a team kill
			if ( !isFriendly )
			{
				Deaths++;
			}
		}
	}
}
