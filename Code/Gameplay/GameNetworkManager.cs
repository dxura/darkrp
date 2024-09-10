using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using SandbankDatabase;
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
	public GameObject PlayerStatePrefab { get; set; }

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
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}
	}

	/// <summary>
	/// Tries to recycle a player state owned by this player (if they disconnected) or makes a new one.
	/// </summary>
	/// <param name="channel"></param>
	/// <returns></returns>
	private PlayerState GetOrCreatePlayerState( Connection channel = null )
	{
		// A candidate player state has no owner.
		var playerState = Scene.GetAllComponents<PlayerState>()
			.FirstOrDefault(x => x.Connection is null && x.UID == channel.SteamId.ToString());

		if ( playerState.IsValid() )
		{
			Log.Warning(
				$"Found existing player state for {channel.SteamId} that we can re-use. {playerState}");
			return playerState;
		}

		Assert.True( PlayerStatePrefab.IsValid(), "Could not spawn player as no PlayerStatePrefab assigned." );

		var player = PlayerStatePrefab.Clone();
		player.BreakFromPrefab();
		player.Name = $"PlayerState ({channel.DisplayName})";
		player.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );
		playerState = player.Components.Get<PlayerState>();

		var playerData = Sandbank.SelectOneWithID<PlayerState>("players", channel.SteamId.ToString());

		if ( playerData != null )
		{
			Log.Warning($"Found existing player data for {channel.SteamId} that we can re-use.");
			Sandbank.CopySavedData(playerData, playerState);
		}
		else
		{
			playerState.UID = channel.SteamId.ToString();
			playerState.SteamName = channel.DisplayName;
		}

		return playerState;
	}

	/// <summary>
	/// Called when a network connection becomes active
	/// </summary>
	/// <param name="channel"></param>
	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is becoming active" );

		var playerState = GetOrCreatePlayerState( channel );
		if ( !playerState.IsValid() )
		{
			throw new Exception( $"Something went wrong when trying to create PlayerState for {channel.DisplayName}" );
		}

		OnPlayerJoined( playerState, channel );
	}

	public void OnPlayerJoined( PlayerState playerState, Connection channel )
	{
		// Dunno if we need both of these events anymore? But I'll keep them for now.
		Scene.Dispatch( new PlayerConnectedEvent( playerState ) );

		// Either spawn over network, or claim ownership
		if ( !playerState.Network.Active )
		{
			playerState.GameObject.NetworkSpawn( channel );
		}
		else
		{
			playerState.Network.AssignOwnership( channel );
		}

		playerState.HostInit();
		playerState.ClientInit();

		Scene.Dispatch( new PlayerJoinedEvent( playerState ) );
	}
}
