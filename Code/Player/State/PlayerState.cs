using GameSystems.Jobs;
using SandbankDatabase;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class PlayerState : Component
{
	/// <summary>
	/// Our local player on this client.
	/// </summary>
	public static PlayerState? Local { get; private set; }

	/// <summary>
	/// The player's ID. This is their SteamID.
	/// </summary>
	[HostSync]
	[Property]
	[Saved]
	public string UID { get; set; } = "";

	/// <summary>
	/// The player's name, which might have to persist if they leave
	/// </summary>
	[HostSync]
	[Saved]
	public string? SteamName { get; set; }

	/// <summary>
	/// The connection of this player
	/// </summary>
	public Connection? Connection => Network.OwnerConnection;

	public bool IsConnected => Connection is not null && (Connection.IsActive || Connection.IsHost); //smh

	/// <summary>
	/// Name of this player
	/// </summary>
	public string DisplayName => $"{SteamName}{(!IsConnected ? " (Disconnected)" : "")}";

	/// <summary>
	/// The job this player belongs to.
	/// </summary>
	[Property]
	[Group( "Setup" )]
	[HostSync]
	[Change( nameof(OnJobPropertyChanged) )]

	public JobResource Job { get; set; } = null!;

	/// <summary>
	/// Is this the local player for this client
	/// </summary>
	public bool IsLocalPlayer => !IsProxy && Connection == Connection.Local;

	/// <summary>
	/// Unique colour or team color of this player
	/// </summary>
	public Color PlayerColor => Job.Color;

	/// <summary>
	/// The main player
	/// </summary>
	[HostSync]
	[ValidOrNull]
	public Player? Player { get; set; }

	public void HostInit()
	{
		RespawnState = RespawnState.Immediate;
	}

	[Authority]
	public void ClientInit()
	{
		Local = this;
	}

	public void Kick()
	{
		if ( Player.IsValid() )
		{
			Player.GameObject.Destroy();
		}

		GameObject.Destroy();
		// todo: actually kick em
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void AssignJob( JobResource job )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		
		// Respect job caps
		if ( job.MaxWorkers != 0 && GameUtils.GetPlayersByJob( job ).Count() >= job.MaxWorkers )
		{
			return;
		}

		Job = job;
		Respawn(Random.Shared.FromList(Respawner.SpawnPoints), true );


		Scene.Dispatch( new JobAssignedEvent( this, job ) );
	}

	/// <summary>
	/// Called when <see cref="Job"/> changes across the network.
	/// </summary>
	private void OnJobPropertyChanged( JobResource before, JobResource after )
	{
		GameObject.Root.Dispatch( new JobChangedEvent( before, after ) );

		// Send this to the player too
		if ( Player.IsValid() )
		{
			Player.GameObject.Root.Dispatch( new JobChangedEvent( before, after ) );
		}
	}

	/// <summary>
	/// Save this player's data.
	/// </summary>
	public void Save()
	{
		Sandbank.Insert("players", this);
	}
}
