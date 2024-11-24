using GameSystems.Jobs;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace Dxura.Darkrp;

public enum RespawnState
{
	Not,
	Requested,
	Delayed,
	Immediate
}

public partial class Player : IRespawnable, IDescription
{
	/// <summary>
	/// Our local player on this client.
	/// </summary>
	public static Player Local { get; private set; } = null!;
	
	/// <summary>
	/// The player's ID. This is their SteamID.
	/// </summary>
	[HostSync]
	[Property, Group("State")]
	public string SteamId { get; set; } = "";

	/// <summary>
	/// The player's name, which might have to persist if they leave
	/// </summary>
	[HostSync]
	public string? SteamName { get; set; }
	
	/// <summary>
	/// What are we called?
	/// </summary>
	public string DisplayName => $"{SteamName}{(!IsConnected ? " (Disconnected)" : "")}";

	/// <summary>
	/// The connection of this player
	/// </summary>
	public Connection? Connection => Network.Owner;

	public bool IsConnected => Connection is not null && (Connection.IsActive || Connection.IsHost);
	
	/// <summary>
	/// How long since the player last respawned?
	/// </summary>
	[HostSync]
	public TimeSince TimeSinceLastRespawn { get; private set; }
	
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
	/// The job this player belongs to.
	/// </summary>
	[Property, Group("State")]
	[HostSync]
	[Change( nameof(OnJobPropertyChanged) )]
	public JobResource Job { get; set; } = null!;
	
	public TimeSince TimeSinceRespawnStateChanged { get; private set; }
	public DamageInfo? LastDamageInfo { get; private set; }
	
	// IDescription
	string IDescription.DisplayName => DisplayName;
	Color IDescription.Color => Job.Color;

	/// <summary>
	/// Is this the local player for this client
	/// </summary>
	public bool IsLocalPlayer => !IsProxy && Connection == Connection.Local;

	/// <summary>
	/// Unique colour or team color of this player
	/// </summary>
	public Color PlayerColor => Job.Color;
	
	public SceneTraceResult CachedEyeTrace { get; private set; }

	public void OnFixedUpdateState()
	{
		if (IsProxy)
		{
			return;
		}
		
		CachedEyeTrace = Scene.Trace.Ray( AimRay, 100000f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "ragdoll", "movement" )
			.UseHitboxes()
			.Run();
	}
	
	[Authority]
	public void ClientInit()
	{
		Local = this;
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
	}

	public void Teleport( Transform transform )
	{
		Teleport( transform.Position, transform.Rotation );
	}

	[Authority]
	public void Teleport( Vector3 position, Rotation rotation )
	{
		Transform.World = new Transform( position, rotation );
		Transform.ClearInterpolation();
		EyeAngles = rotation.Angles();

		if ( IsValid )
		{
			Velocity = Vector3.Zero;
			IsOnGround = true;
		}
	}
	
		public void OnKill( DamageInfo damageInfo )
	{
		LastDamageInfo = damageInfo;

		if ( Networking.IsHost )
		{
			ArmorComponent.HasHelmet = false;
			ArmorComponent.Armor = 0f;

			RespawnState = RespawnState.Requested;

			ClearLoadout();
			DoRagdoll();
		}

		PlayerBoxCollider.Enabled = false;

		if ( IsProxy )
		{
			return;
		}
		
		Holster();

		_previousVelocity = Vector3.Zero;
		CameraMode = CameraMode.ThirdPerson;
	}

	public void SetSpawnPoint( SpawnPointInfo spawnPoint )
	{
		SpawnPosition = spawnPoint.Position;
		SpawnRotation = spawnPoint.Rotation;

		SpawnPointTags.Clear();

		foreach ( var tag in spawnPoint.Tags )
		{
			SpawnPointTags.Add( tag );
		}
	}

	public void OnRespawn()
	{
		Assert.True( Networking.IsHost );

		OnHostRespawn();
		OnClientRespawn();
	}

	private void OnHostRespawn()
	{
		Assert.True( Networking.IsHost );

		_previousVelocity = Vector3.Zero;

		// Leave a seat if we are in one.
		if ( CurrentSeat.IsValid() )
		{
			CurrentSeat.Leave( this );
		}

		Teleport( SpawnPosition, SpawnRotation );

		DamageTakenForce = Vector3.Zero;

		if ( HealthComponent.State != LifeState.Alive )
		{
			ArmorComponent.HasHelmet = false;
			ArmorComponent.Armor = 0f;
		}

		HealthComponent.Health = HealthComponent.MaxHealth;
		HealthComponent.State = LifeState.Alive;
		
		TimeSinceLastRespawn = 0f;

		ResetBody();
		Scene.Dispatch( new PlayerSpawnedEvent( this ) );
	}

	[Authority]
	private void OnClientRespawn()
	{
		CameraMode = CameraMode.FirstPerson;
	}
	
	[Broadcast( NetPermission.HostOnly )]
	private void DoRagdoll()
	{
		SetRagdoll( true );
	}

	private void ResetBody()
	{
		DamageTakenForce = Vector3.Zero;

		PlayerBoxCollider.Enabled = true;
		
		SetRagdoll(false);

		UpdateBodyFromJob(Job);
	}
		
	/// <summary>
	/// Are we ready to respawn?
	/// </summary>
	[HostSync]
	[Change( nameof(OnRespawnStateChanged) )]
	public RespawnState RespawnState { get; set; } = RespawnState.Immediate;

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

	private void OnRespawnStateChanged( LifeState oldValue, LifeState newValue )
	{
		TimeSinceRespawnStateChanged = 0f;
	}

	private Player? GetLastKiller()
	{
		if ( LastDamageInfo == null )
		{
			return null;
		}
		
		return GameUtils.GetPlayerFromComponent( LastDamageInfo.Attacker );
	}
	
	public void OnGameEvent( JobChangedEvent eventArgs )
	{
		Sound.Play( JobChangedSound, WorldPosition);
		UpdateBodyFromJob(eventArgs.After);
	}


}
