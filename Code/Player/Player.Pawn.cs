using GameSystems.Jobs;

namespace Dxura.Darkrp;

public enum RespawnState
{
	Not,
	Requested,
	Delayed,
	Immediate
}

public partial class Player :  Component.INetworkListener
{
	/// <summary>
	/// The position this player last spawned at.
	/// </summary>
	[HostSync]
	public Vector3 SpawnPosition { get; set; }

	/// <summary>
	/// The rotation this player last spawned at.
	/// </summary>
	[HostSync]
	public Rotation SpawnRotation { get; set; }

	/// <summary>
	/// The tags of the last spawn point of this pawn.
	/// </summary>
	[HostSync]
	public NetList<string> SpawnPointTags { get; private set; } = new();

	/// <summary>
	/// Who's the owner?
	/// </summary>
	[Sync]
	public ulong SteamId { get; set; }
	
	/// <summary>
	/// What are we called?
	/// </summary>
	public string DisplayName => $"{SteamName}{(!IsConnected ? " (Disconnected)" : "")}";
	
	/// <summary>
	/// How long has it been since this player has d/c'd
	/// </summary>
	private RealTimeSince TimeSinceDisconnected { get; set; }

	public TimeSince TimeSinceRespawnStateChanged { get; private set; }
	public DamageInfo? LastDamageInfo { get; private set; }

	/// <summary>
	/// How long does it take to clean up a player once they disconnect?
	/// </summary>
	public static float DisconnectCleanupTime { get; set; } = 30f;

	void INetworkListener.OnDisconnected( Connection channel )
	{
		if ( Connection == channel )
		{
			TimeSinceDisconnected = 0;
		}
	}

	private void OnUpdatePawn()
	{
		if ( IsConnected )
		{
			return;
		}

		if ( IsProxy )
		{
			return;
		}

		if ( TimeSinceDisconnected > DisconnectCleanupTime )
		{
			GameObject.Destroy();
		}
	}
	
	/// <summary>
	/// Are we ready to respawn?
	/// </summary>
	[HostSync]
	[Change( nameof(OnRespawnStateChanged) )]
	public RespawnState RespawnState { get; set; }

	public bool IsRespawning => RespawnState is RespawnState.Delayed;

	private void Spawn( SpawnPointInfo spawnPoint )
	{
		RespawnState = RespawnState.Not;
		OnRespawn();
	}

	public void Respawn(SpawnPointInfo spawnPoint, bool forceNew )
	{
		Log.Info(
			$"Spawning player.. ( {GameObject.Name} ({DisplayName}, {Job}), {spawnPoint.Position}, [{string.Join( ", ", spawnPoint.Tags )}] )" );

		if ( forceNew || HealthComponent.State == LifeState.Dead )
		{
			Spawn( spawnPoint );
		}
		else
		{
			SetSpawnPoint( spawnPoint );
			OnRespawn();
		}
	}

	protected void OnRespawnStateChanged( LifeState oldValue, LifeState newValue )
	{
		TimeSinceRespawnStateChanged = 0f;
	}

	public Player? GetLastKiller()
	{
		if ( LastDamageInfo == null )
		{
			return null;
		}
		
		return GameUtils.GetPlayerFromComponent( LastDamageInfo.Attacker );
	}
}
