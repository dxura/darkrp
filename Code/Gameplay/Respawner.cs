using Dxura.Darkrp.UI;

namespace Dxura.Darkrp;

// TODO: Add delay
public class Respawner : Component
{
	[Property] [HostSync] public float RespawnDelaySeconds { get; set; } = 3f;

	private readonly List<SpawnPointInfo> _spawnPointInfos = new();

	/// <summary>
	/// How long until respawn?
	/// </summary>
	/// <returns></returns>
	public int GetRespawnTime()
	{
		return (RespawnDelaySeconds - PlayerState.Local.TimeSinceRespawnStateChanged).Clamp( 0f, RespawnDelaySeconds )
			.CeilToInt();
	}

	protected override void OnStart()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>();

		foreach ( var spawnPoint in spawnPoints )
		{
			_spawnPointInfos.Add( new SpawnPointInfo(new Transform(spawnPoint.Transform.Position, spawnPoint.Transform.Rotation), Array.Empty<string>() ));
		}
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		foreach ( var player in GameUtils.AllPlayers )
		{
			if ( player.Player.IsValid() && player.Player.HealthComponent.State == LifeState.Alive )
			{
				continue;
			}

			if ( !player.IsConnected )
			{
				continue;
			}

			switch ( player.RespawnState )
			{
				case RespawnState.Requested:
					player.RespawnState = RespawnState.Delayed;
					break;

				case RespawnState.Delayed:
					if ( player.TimeSinceRespawnStateChanged > RespawnDelaySeconds )
					{
						player.RespawnState = RespawnState.Immediate;
					}

					break;

				case RespawnState.Immediate:
					player.Respawn(Random.Shared.FromList(_spawnPointInfos), true );
					break;
			}
		}
	}
}
