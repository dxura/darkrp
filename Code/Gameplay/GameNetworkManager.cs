using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Network;

namespace Dxura.Darkrp;

public sealed class GameNetworkManager : SingletonComponent<GameNetworkManager>, Component.INetworkListener
{
	/// <summary>
	/// Which player prefab should we spawn?
	/// </summary>
	[Property]
	public required GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// Is this game multiplayer? If not, we won't create a lobby.
	/// </summary>
	[Property]
	public bool IsMultiplayer { get; set; } = true;

	protected override void OnStart()
	{
		if ( !IsMultiplayer )
		{
			OnActive( Connection.Local );
			return;
		}

		//
		// Create a lobby if we're not connected
		//
		if ( !Networking.IsActive )
		{
			Networking.CreateLobby(new LobbyConfig());
		}
	}

	/// <summary>
	/// Tries to recycle a player state owned by this player (if they disconnected) or makes a new one.
	/// </summary>
	/// <param name="channel"></param>
	/// <returns></returns>
	private Player GetOrCreatePlayer( Connection channel)
	{
		// A candidate player state has no owner.
		var candidatePlayer = Scene.GetAllComponents<Player>()
			.FirstOrDefault( x => x.Connection is null && x.SteamId == channel.SteamId.ToString() );

		if ( candidatePlayer.IsValid() )
		{
			Log.Warning(
				$"Found existing player state for {channel.SteamId} that we can re-use. {candidatePlayer}");
			return candidatePlayer;
		}

		Assert.True( PlayerPrefab.IsValid(), "Could not spawn player as no PlayerPrefab assigned." );

		var playerGameObject = PlayerPrefab.Clone();
		playerGameObject.BreakFromPrefab();
		playerGameObject.Name = $"Player ({channel.DisplayName})";
		playerGameObject.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );

		// var playerData = Sandbank.SelectOneWithID<Player>("players", channel.SteamId.ToString());
		//
		// if ( playerData != null )
		// {
		// 	Log.Warning($"Found existing player data for {channel.SteamId} that we can re-use.");
		// 	Sandbank.CopySavedData(playerData, Player);
		// }
		
		var player = playerGameObject.GetComponent<Player>();
		player.SteamId = channel.SteamId.ToString();
		player.SteamName = channel.DisplayName;
		
		return player;
	}

	/// <summary>
	/// Called when a network connection becomes active
	/// </summary>
	/// <param name="channel"></param>
	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is becoming active" );
		
		var player = GetOrCreatePlayer( channel );
		if ( !player.IsValid() )
		{
			throw new Exception( $"Something went wrong when trying to create Player for {channel.DisplayName}" );
		}
		
		OnPlayerJoined( player, channel );
	}

	private void OnPlayerJoined( Player player, Connection channel )
	{
		// Dunno if we need both of these events anymore? But I'll keep them for now.
		Scene.Dispatch( new PlayerConnectedEvent( player ) );

		// Either spawn over network, or claim ownership
		if ( !player.Network.Active )
		{
			player.GameObject.NetworkSpawn( channel );
		}
		else
		{
			player.Network.AssignOwnership( channel );
		}

		player.ClientInit();
		
		Scene.Dispatch( new PlayerJoinedEvent( player ) );
	}
}
