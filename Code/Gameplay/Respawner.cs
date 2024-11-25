using Sandbox.Events;

namespace Dxura.Darkrp;

public class Respawner : Component, IGameEventHandler<PlayerJoinedEvent>
{
    [Property] [HostSync] private float RespawnDelaySeconds { get; set; } = 3f;

    public static readonly List<SpawnPointInfo> SpawnPoints = new();
    private TimeUntil _nextUpdateTime = 0f;

    public int GetRespawnTime()
    {
        if (Player.Local != null)
        {
            return (RespawnDelaySeconds - Player.Local.TimeSinceRespawnStateChanged)
                .Clamp(0f, RespawnDelaySeconds)
                .CeilToInt();
        }

        return 10;
    }

    protected override void OnStart()
    {
        RefreshSpawnPoints();
    }

    private void RefreshSpawnPoints()
    {
        SpawnPoints.Clear();
        foreach (var spawnPoint in Scene.GetAllComponents<SpawnPoint>())
        {
            SpawnPoints.Add(new SpawnPointInfo(new Transform(spawnPoint.Transform.Position, spawnPoint.Transform.Rotation), Array.Empty<string>()));
        }

        if (SpawnPoints.Count == 0)
        {
            Log.Warning("No spawn points found in the scene!");
        }
    }

    protected override void OnFixedUpdate()
    {
        if (!Networking.IsHost)
            return;

        if (_nextUpdateTime > 0)
            return;

        _nextUpdateTime = 1.0f; // Set next update time to 1 second from now

        if (SpawnPoints.Count == 0)
        {
            RefreshSpawnPoints();
            if (SpawnPoints.Count == 0) return;
        }

        foreach (var player in GameUtils.AllPlayers)
        {
            if (player.IsValid() && player.HealthComponent.State == LifeState.Alive)
                continue;

            if (!player.IsConnected)
                continue;

            switch (player.RespawnState)
            {
                case RespawnState.Requested:
                    player.RespawnState = RespawnState.Delayed;
                    break;

                case RespawnState.Delayed:
                    if (player.TimeSinceRespawnStateChanged > RespawnDelaySeconds)
                    {
                        player.RespawnState = RespawnState.Immediate;
                    }
                    break;

                case RespawnState.Immediate:
                    player.Respawn();
                    break;
            }
        }
    }

    // "Spawn" player when joined
    public void OnGameEvent(PlayerJoinedEvent eventArgs)
    {
        var spawnPoint = Game.Random.FromList(SpawnPoints);
        eventArgs.Player.SetSpawnPoint(spawnPoint);
        eventArgs.Player.Respawn();
    }
}
