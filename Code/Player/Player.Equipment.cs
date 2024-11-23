using Sandbox.Diagnostics;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class Player :
	IGameEventHandler<EquipmentDeployedEvent>,
	IGameEventHandler<EquipmentHolsteredEvent>,
	IGameEventHandler<PlayerSpawnedEvent>
{
	/// <summary>
	/// What weapon are we using?
	/// </summary>
	[Property]
	[ReadOnly]
	public Equipment? CurrentEquipment { get; private set; }
	
	/// <summary>
	/// What equipment do we have right now?
	/// </summary>
	public IEnumerable<Equipment> Equipment => Components.GetAll<Equipment>( FindMode.EverythingInSelfAndDescendants );

	/// <summary>
	/// A <see cref="GameObject"/> that will hold all of our equipment.
	/// </summary>
	[Property]
	public GameObject WeaponGameObject { get; set; } = null!;

	public bool CantSwitch = false;

	public GameObject ViewModelGameObject => Scene.Camera.GameObject;

	/// <summary>
	/// How inaccurate are things like gunshots?
	/// </summary>
	public float Spread { get; set; }

	private void OnFixedUpdateEquipment()
	{
		var isAiming = CurrentEquipment.IsValid() && CurrentEquipment.Tags.Has( "aiming" );

		var spread = Global.BaseSpreadAmount;
		var scale = Global.VelocitySpreadScale;
		if ( isAiming )
		{
			spread *= Global.AimSpread;
		}

		if ( isAiming )
		{
			scale *= Global.AimVelocitySpreadScale;
		}

		var velLen =  Velocity.Length;
		spread += velLen.Remap( 0, Global.SpreadVelocityLimit, 0, 1, true ) * scale;

		if ( IsCrouching && IsGrounded )
		{
			spread *= Global.CrouchSpreadScale;
		}

		if ( !IsGrounded )
		{
			spread *= Global.AirSpreadScale;
		}

		Spread = spread;
	}

	void IGameEventHandler<EquipmentDeployedEvent>.OnGameEvent( EquipmentDeployedEvent eventArgs )
	{
		CurrentEquipment = eventArgs.Equipment;
	}

	void IGameEventHandler<EquipmentHolsteredEvent>.OnGameEvent( EquipmentHolsteredEvent eventArgs )
	{
		if ( eventArgs.Equipment == CurrentEquipment )
		{
			CurrentEquipment = null;
		}
	}

	[Authority]
	private void SetCurrentWeapon( Equipment? equipment )
	{
		SetCurrentEquipment( equipment );
	}

	[Authority]
	private void ClearCurrentWeapon()
	{
		CurrentEquipment?.Holster();
	}

	public void Holster()
	{
		if ( IsProxy )
		{
			if ( Networking.IsHost )
			{
				ClearCurrentWeapon();
			}

			return;
		}

		CurrentEquipment?.Holster();
	}

	public TimeSince TimeSinceWeaponDeployed { get; private set; }

	public void SetCurrentEquipment( Equipment? weapon )
	{
		if ( weapon == CurrentEquipment ) 
			return;

		ClearCurrentWeapon();

		if ( IsProxy )
		{
			if ( Networking.IsHost )
				SetCurrentWeapon( weapon );

			return;
		}

		TimeSinceWeaponDeployed = 0;

		weapon?.Deploy();
	}

	public void ClearViewModel()
	{
		foreach ( var weapon in Equipment )
		{
			weapon.ClearViewModel();
		}
	}

	public void CreateViewModel( bool playDeployEffects = true )
	{
		if ( CameraMode != CameraMode.FirstPerson )
		{
			return;
		}

		var weapon = CurrentEquipment;
		if ( weapon.IsValid() )
		{
			weapon.CreateViewModel( playDeployEffects );
		}
	}

	public void ClearLoadout()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		foreach ( var wpn in Equipment )
		{
			wpn.GameObject.Destroy();
			wpn.Enabled = false;
		}
	}

	[Authority( NetPermission.HostOnly )]
	public void RefillAmmo()
	{
		foreach ( var wpn in Equipment )
		{
			if ( wpn.Components.Get<AmmoComponent>( FindMode.EnabledInSelfAndDescendants ) is { } ammo )
			{
				ammo.Ammo = ammo.MaxAmmo;
			}
		}
	}

	/// <summary>
	/// Try to drop the given held equipment item.
	/// </summary>
	/// <param name="weapon">Item to drop.</param>
	/// <param name="forceRemove">If we can't drop, remove it from the inventory anyway.</param>
	public void Drop( Equipment weapon, bool forceRemove = false )
	{
		using ( Rpc.FilterInclude( Connection.Host ) )
		{
			DropHost( weapon, forceRemove );
		}
	}

	[Broadcast]
	private void DropHost( Equipment weapon, bool forceRemove )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( !weapon.IsValid() )
		{
			return;
		}

		var canDrop = GameManager.Instance.Get<EquipmentDropper>() is { } dropper && dropper.CanDrop(this, weapon );

		if ( canDrop )
		{
			var tr = Scene.Trace.Ray( new Ray( AimRay.Position, AimRay.Forward ), 128 )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.WithoutTags( "trigger" )
				.Run();

			var position = tr.Hit
				? tr.HitPosition + tr.Normal * weapon.Resource.WorldModel.Bounds.Size.Length
				: AimRay.Position + AimRay.Forward * 32f;
			var rotation = Rotation.From( 0, EyeAngles.yaw + 90, 90 );

			var baseVelocity = Velocity;
			var droppedWeapon = DroppedEquipment.Create( weapon.Resource, position, rotation, weapon );

			if ( !tr.Hit )
			{
				droppedWeapon.Rigidbody.Velocity = baseVelocity + AimRay.Forward * 200.0f + Vector3.Up * 50;
				droppedWeapon.Rigidbody.AngularVelocity = Vector3.Random * 8.0f;
			}
		}

		if ( canDrop || forceRemove )
		{
			RemoveEquipment( weapon );
		}
	}

	private void OnUpdateEquipment()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( Input.Pressed( "Drop" ) && CurrentEquipment.IsValid() )
		{
			Drop( CurrentEquipment );
			return;
		}

		if ( CantSwitch )
		{
			return;
		}

		foreach ( var slot in Enum.GetValues<EquipmentSlot>() )
		{
			if ( slot == EquipmentSlot.Undefined )
			{
				continue;
			}

			if ( !Input.Pressed( $"Slot{(int)slot}" ) )
			{
				continue;
			}

			SwitchToSlot( slot );
			return;
		}

		var wheel = Input.MouseWheel;

		if ( wheel.y == 0f )
		{
			return;
		}

		var availableWeapons = Equipment.OrderBy( x => x.Resource.Slot ).ToList();
		if ( availableWeapons.Count == 0 )
		{
			return;
		}

		var currentSlot = 0;
		for ( var index = 0; index < availableWeapons.Count; index++ )
		{
			var weapon = availableWeapons[index];
			if ( !weapon.IsDeployed )
			{
				continue;
			}

			currentSlot = index;
			break;
		}

		var slotDelta = wheel.y > 0f ? 1 : -1;
		currentSlot += slotDelta;

		if ( currentSlot < 0 )
		{
			currentSlot = availableWeapons.Count - 1;
		}
		else if ( currentSlot >= availableWeapons.Count )
		{
			currentSlot = 0;
		}

		var weaponToSwitchTo = availableWeapons[currentSlot];
		if ( weaponToSwitchTo == CurrentEquipment )
		{
			return;
		}

		Switch( weaponToSwitchTo );
	}
	
	public void HolsterCurrent()
	{
		Assert.True( !IsProxy || Networking.IsHost );
		SetCurrentEquipment( null );
	}

	public void SwitchToSlot( EquipmentSlot slot )
	{
		Assert.True( !IsProxy || Networking.IsHost );

		var equipment = Equipment
			.Where( x => x.Resource.Slot == slot )
			.ToArray();

		if ( equipment.Length == 0 )
		{
			return;
		}

		if ( equipment.Length == 1 && CurrentEquipment == equipment[0])
		{
			HolsterCurrent();
			return;
		}

		var index = Array.IndexOf( equipment, CurrentEquipment );
		Switch( equipment[(index + 1) % equipment.Length] );
	}

	/// <summary>
	/// Tries to set the player's current weapon to a specific one, which has to be in the player's inventory.
	/// </summary>
	/// <param name="equipment"></param>
	public void Switch( Equipment? equipment )
	{
		Assert.True( !IsProxy || Networking.IsHost );

		if ( !Equipment.Contains( equipment ) )
		{
			return;
		}

		SetCurrentEquipment( equipment );
	}

	/// <summary>
	/// Removes the given weapon and destroys it.
	/// </summary>
	public void RemoveEquipment( Equipment equipment )
	{
		Assert.True( Networking.IsHost );

		if ( !Equipment.Contains( equipment ) )
		{
			return;
		}

		if ( CurrentEquipment == equipment )
		{
			var otherEquipment = Equipment.Where( x => x != equipment );
			var orderedBySlot = otherEquipment.OrderBy( x => x.Resource.Slot );
			var targetWeapon = orderedBySlot.FirstOrDefault();

			if ( targetWeapon.IsValid() )
			{
				Switch( targetWeapon );
			}
		}

		equipment.GameObject.Destroy();
		equipment.Enabled = false;
	}

	/// <summary>
	/// Removes the given weapon (by its resource data) and destroys it.
	/// </summary>
	public void Remove( EquipmentResource resource )
	{
		var equipment = Equipment.FirstOrDefault( w => w.Resource == resource );
		if ( !equipment.IsValid() )
		{
			return;
		}

		RemoveEquipment( equipment );
	}

	public Equipment? Give( EquipmentResource? resource, bool makeActive = true )
	{
		Assert.True( Networking.IsHost );

		// If we're in charge, let's make some equipment.
		if ( resource == null )
		{
			Log.Warning( "A player loadout without a equipment? Nonsense." );
			return null;
		}

		var pickupResult = CanTake( resource );

		if ( pickupResult == PickupResult.None )
		{
			return null;
		}

		// Don't let us have the exact same equipment
		if ( Has( resource ) )
		{
			return null;
		}

		if ( pickupResult == PickupResult.Swap )
		{
			var slotCurrent =
				Equipment.FirstOrDefault( equipment => equipment.Enabled && equipment.Resource.Slot == resource.Slot );
			if ( slotCurrent.IsValid() )
			{
				Drop( slotCurrent, true );
			}
		}

		if ( !resource.MainPrefab.IsValid() )
		{
			Log.Error(
				$"equipment doesn't have a prefab? {resource}, {resource.MainPrefab}, {resource.ViewModelPrefab}" );
			return null;
		}

		// Create the equipment prefab and put it on the GameObject.
		var gameObject = resource.MainPrefab.Clone( new CloneConfig()
		{
			Transform = new Transform(), Parent = WeaponGameObject
		} );
		var component = gameObject.Components.Get<Equipment>( FindMode.EverythingInSelfAndDescendants );
		gameObject.NetworkSpawn( Network.Owner );
		component.OwnerId = Id;

		if ( makeActive )
		{
			SetCurrentEquipment( component );
		}

		return component;
	}

	public bool Has( EquipmentResource resource )
	{
		return Equipment.Any( weapon => weapon.Enabled && weapon.Resource == resource );
	}

	public bool HasInSlot( EquipmentSlot slot )
	{
		return Equipment.Any( weapon => weapon.Enabled && weapon.Resource.Slot == slot );
	}

	public enum PickupResult
	{
		None,
		Pickup,
		Swap
	}

	public PickupResult CanTake( EquipmentResource resource )
	{
		return Has( resource ) ? PickupResult.Swap : PickupResult.Pickup;
	}

	public void TryPurchaseBuyMenuItem( string equipmentId )
	{
		using var _ = Rpc.FilterInclude( Connection.Host );
		PurchaseBuyMenuItem( equipmentId );
	}

	[Broadcast]
	private void PurchaseBuyMenuItem( string equipmentId )
	{
		if ( !Networking.IsHost )
		{
			Log.Warning( $"Tried to purchase an buy menu item ({equipmentId}) but is not the host." );
			return;
		}

		var equipmentData = BuyMenuItem.GetById( equipmentId );

		if ( equipmentData == null )
		{
			Log.Warning( $"Attempted purchase but EquipmentData (Id: {equipmentId}) not known!" );
			return;
		}

		equipmentData.Purchase( this );
	}

	public void Purchase( int resourceId )
	{
		using var _ = Rpc.FilterInclude( Connection.Host );
		PurchaseAsHost( resourceId );
	}

	[Broadcast]
	private void PurchaseAsHost( int resourceId )
	{
		if ( !Networking.IsHost )
		{
			Log.Warning( $"Tried to purchase an inventory resource ({resourceId}) but is not the host." );
			return;
		}

		var resource = ResourceLibrary.Get<EquipmentResource>( resourceId );

		if ( resource == null )
		{
			Log.Warning( $"Attempted purchase but EquipmentResource (Id: {resource}) not known!" );
			return;
		}

		if ( Balance < resource.Price )
		{
			return;
		}

		if ( Give( resource, false ) is null )
		{
			return;
		}

		Balance -= resource.Price;
	}

	public void OnGameEvent( PlayerSpawnedEvent eventArgs )
	{
		
		// Ignore if not for us
		if ( eventArgs.Player != this )
		{
			return;	
		}
		
		var isFirst = true;
		
		// Give default equipment (Citizen)
		foreach (var equipmentResource in JobProvider.Default().Equipment)
		{
			equipmentResource.CanDrop = false; // Prevent dropping default
			Give( equipmentResource, isFirst);
			isFirst = false;
		}
		
		// Then any job-specific equipment
		foreach (var equipmentResource in eventArgs.Player.Job.Equipment)
		{
			equipmentResource.CanDrop = false; // Prevent dropping job-specific equipment
			Give( equipmentResource, isFirst);
		}
	}
}
