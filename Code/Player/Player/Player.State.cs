using GameSystems.Jobs;
using SandbankDatabase;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class Player
{
	/// <summary>
	/// An accessor for health component if we have one.
	/// </summary>
	[Property, Group("State")]
	[RequireComponent]
	public HealthComponent HealthComponent { get; set; } = null!;
	
	/// <summary>
	/// The player's health component
	/// </summary>
	[RequireComponent]
	public ArmorComponent ArmorComponent { get; private set; } = null!;

	/// <summary>
	/// The player's inventory, items, etc.
	/// </summary>
	[RequireComponent]
	public PlayerInventory Inventory { get; private set; } = null!;

	/// <summary>
	/// How long since the player last respawned?
	/// </summary>
	[HostSync]
	public TimeSince TimeSinceLastRespawn { get; private set; }
	
	/// <summary>
	/// Our local player on this client.
	/// </summary>
	public static Player? Local { get; private set; }

	/// <summary>
	/// The player's ID. This is their SteamID.
	/// </summary>
	[HostSync]
	[Property, Group("State")]
	[Saved]
	public string Uid { get; set; } = "";

	/// <summary>
	/// The player's name, which might have to persist if they leave
	/// </summary>
	[HostSync]
	[Saved]
	public string? SteamName { get; set; }

	/// <summary>
	/// The connection of this player
	/// </summary>
	public Connection? Connection => Network.Owner;

	public bool IsConnected => Connection is not null && (Connection.IsActive || Connection.IsHost);

	/// <summary>
	/// The job this player belongs to.
	/// </summary>
	[Property, Group("State")]
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
	}

	/// <summary>
	/// Save this player's data.
	/// </summary>
	public void Save()
	{
		Sandbank.Insert("players", this);
	}

	public void OnKill( DamageInfo damageInfo )
	{
		LastDamageInfo = damageInfo;

		if ( Networking.IsHost )
		{
			ArmorComponent.HasHelmet = false;
			ArmorComponent.Armor = 0f;

			RespawnState = RespawnState.Requested;

			Inventory.Clear();
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
		SteamId = Connection.Local.SteamId;

		CameraMode = CameraMode.FirstPerson;
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

	// Death cam
	private void UpdateDeathCam()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( LastDamageInfo is null )
		{
			return;
		}

		var killer = GetLastKiller();

		if ( killer.IsValid() )
		{
			EyeAngles = Rotation.LookAt( killer.WorldPosition - WorldPosition, Vector3.Up );
		}
	}
}
