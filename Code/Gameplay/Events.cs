using GameSystems.Jobs;
using Sandbox.Events;

namespace Dxura.Darkrp;

/// <summary>
/// Called on the host when a new player joins, before NetworkSpawn is called.
/// </summary>
public record PlayerConnectedEvent( PlayerState PlayerState ) : IGameEvent;

/// <summary>
/// Called on the host when a new player joins, after NetworkSpawn is called.
/// </summary>
public record PlayerJoinedEvent( PlayerState Player ) : IGameEvent;

/// <summary>
/// Called on the host when a player (re)spawns.
/// </summary>
public record PlayerSpawnedEvent( Player Player ) : IGameEvent;

/// <summary>
/// Called on the host when a player is assigned to a team.
/// </summary>
public record JobAssignedEvent( PlayerState Player, JobResource Job ) : IGameEvent;

/// <summary>
/// Called when a job is changed
/// </summary>
public record JobChangedEvent( JobResource Before, JobResource After ) : IGameEvent;
