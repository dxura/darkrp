namespace Dxura.Darkrp;

public enum RespawnState
{
	Not,
	Requested,
	Delayed,
	Immediate
}

public partial class PlayerState
{
	/// <summary>
	/// The prefab to spawn when we want to make a player pawn for the player.
	/// </summary>
	[Property]
	public GameObject PlayerPrefab { get; set; }

	public TimeSince TimeSinceRespawnStateChanged { get; private set; }
	public DamageInfo LastDamageInfo { get; private set; }

	/// <summary>
	/// Are we ready to respawn?
	/// </summary>
	[HostSync]
	[Change( nameof(OnRespawnStateChanged) )]
	public RespawnState RespawnState { get; set; }

	public bool IsRespawning => RespawnState is RespawnState.Delayed;

	private void Spawn( SpawnPointInfo spawnPoint )
	{
		var prefab = PlayerPrefab.Clone( spawnPoint.Transform );
		var pawn = prefab.Components.Get<Player>();

		pawn.PlayerState = this;

		pawn.SetSpawnPoint( spawnPoint );

		prefab.NetworkSpawn( Network.OwnerConnection );

		Player = pawn;


		RespawnState = RespawnState.Not;
		pawn.OnRespawn();
	}

	public void Respawn(SpawnPointInfo spawnPoint, bool forceNew )
	{
		Log.Info(
			$"Spawning player.. ( {GameObject.Name} ({DisplayName}, {Job}), {spawnPoint.Position}, [{string.Join( ", ", spawnPoint.Tags )}] )" );

		if ( forceNew || !Player.IsValid() || Player.HealthComponent.State == LifeState.Dead )
		{
			Player?.GameObject?.Destroy();
			Player = null;

			Spawn( spawnPoint );
		}
		else
		{
			Player.SetSpawnPoint( spawnPoint );
			Player.OnRespawn();
		}
	}

	public void OnKill( DamageInfo damageInfo )
	{
		LastDamageInfo = damageInfo;
	}

	protected void OnRespawnStateChanged( LifeState oldValue, LifeState newValue )
	{
		TimeSinceRespawnStateChanged = 0f;
	}

	public Player GetLastKiller()
	{
		return GameUtils.GetPlayerFromComponent( LastDamageInfo?.Attacker );
	}
}
