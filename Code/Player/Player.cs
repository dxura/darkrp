namespace Dxura.Darkrp;



public sealed partial class Player : Component, IDescription, IRespawnable
{
	/// <summary>
	/// A reference to the player's head (the GameObject)
	/// </summary>
	[Property]
	public GameObject? Head { get; set; }

	/// <summary>
	/// A reference to the animation helper (normally on the Body GameObject)
	/// </summary>
	[Property]
	public AnimationHelper? AnimationHelper { get; set; }
	
	/// <summary>
	/// The outline effect for this player.
	/// </summary>
	[RequireComponent]
	public HighlightOutline Outline { get; set; } = null!;
	
	/// <summary>
	/// The position this player last spawned at.
	/// </summary>
	[HostSync]
	public Vector3 SpawnPosition { get; set; }

	/// <summary>
	/// Who's the owner?
	/// </summary>
	[Sync]
	public ulong SteamId { get; set; }
	
	/// <summary>
	/// What are we called?
	/// </summary>
	public string DisplayName => $"{SteamName}{(!IsConnected ? " (Disconnected)" : "")}";
	
	public TimeSince TimeSinceRespawnStateChanged { get; private set; }
	public DamageInfo? LastDamageInfo { get; private set; }

	// IDescription
	string IDescription.DisplayName => DisplayName;
	Color IDescription.Color => Job.Color;
	
	public SceneTraceResult CachedEyeTrace { get; private set; }
	
	
	protected override void OnStart()
	{
		OnStartCamera();
		
		TagBinder.BindTag( "no_shooting",
			() => IsSprinting || TimeSinceSprintChanged < 0.25f || TimeSinceWeaponDeployed < 0.66f );
		TagBinder.BindTag( "no_aiming",
			() => IsSprinting || TimeSinceSprintChanged < 0.25f || TimeSinceGroundedChanged < 0.25f );

		GameObject.Name = $"Player ({DisplayName})";
	}


	protected override void OnUpdate()
	{
		OnUpdateEquipment();
		
		if ( HealthComponent.State == LifeState.Dead )
		{
			OnUpdateCamera();
		}

		OnUpdateController();

		CrouchAmount = CrouchAmount.LerpTo( IsCrouching ? 1 : 0, Time.Delta * Global.CrouchLerpSpeed );
		_smoothEyeHeight =
			_smoothEyeHeight.LerpTo( EyeHeightOffset * (IsCrouching ? CrouchAmount : 1), Time.Delta * 10f );
	}
	
	protected override void OnFixedUpdate()
	{
		if ( !IsValid )
		{
			return;
		}
		
		OnFixedUpdatePresence();
		OnFixedUpdateEffects();
		
		// Cursor
		if ( !IsProxy )
		{
			CachedEyeTrace = Scene.Trace.Ray( AimRay, 100000f )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "ragdoll", "movement" )
				.UseHitboxes()
				.Run();
		}

		if ( HealthComponent.State != LifeState.Alive )
		{
			return;
		}

		if ( IsProxy )
		{
			return;
		}
		
		OnFixedUpdateController();
		OnFixedUpdateUsing();
		OnFixedUpdateEquipment();
		OnFixedUpdateRoleplay();
	}
}
