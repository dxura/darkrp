using Dxura.Darkrp.UI;
using Sandbox.Events;

namespace Dxura.Darkrp;

/// <summary>
/// Dispatched on the host when a player starts being spawn protected.
/// </summary>
public record SpawnProtectionStartEvent( Player Player ) : IGameEvent;

/// <summary>
/// Dispatched on the host when a player stops being spawn protected.
/// </summary>
public record SpawnProtectionEndEvent( Player Player ) : IGameEvent;

/// <summary>
/// Makes respawned players invulnerable for a given duration, or until they move / shoot.
/// </summary>
public sealed class SpawnProtection : Component,
	IGameEventHandler<PlayerSpawnedEvent>
{
	private readonly Dictionary<Player, TimeSince> _spawnProtectedSince = new();

	[Property] [HostSync] public float MaxDurationSeconds { get; set; } = 10f;

	void IGameEventHandler<PlayerSpawnedEvent>.OnGameEvent( PlayerSpawnedEvent eventArgs )
	{
		Enable( eventArgs.Player );
	}

	public void DisableAll()
	{
		foreach ( var (player, _) in _spawnProtectedSince.ToArray() )
		{
			Disable( player );
		}
	}

	protected override void OnDisabled()
	{
		DisableAll();
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost || _spawnProtectedSince.Count == 0 )
		{
			return;
		}

		foreach ( var (player, since) in _spawnProtectedSince.ToArray() )
		{
			if ( !player.IsValid || since > MaxDurationSeconds )
			{
				Disable( player );
			}
		}
	}

	public void Enable( Player player )
	{
		_spawnProtectedSince[player] = 0f;

		player.HealthComponent.IsGodMode = true;

		Scene.Dispatch( new SpawnProtectionStartEvent( player ) );
	}

	public void Disable( Player player )
	{
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !player.PlayerState.IsValid() )
		{
			return;
		}

		if ( !player.Network.Active || !player.PlayerState.Network.Active )
		{
			return;
		}

		_spawnProtectedSince.Remove( player );
		player.HealthComponent.IsGodMode = false;

		Scene.Dispatch( new SpawnProtectionEndEvent( player ) );
	}
}
