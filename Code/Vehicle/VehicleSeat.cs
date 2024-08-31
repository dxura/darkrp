using Dxura.Darkrp;

namespace Dxura.Darkrp;

public sealed class VehicleSeat : Component
{
	[Property] public Vehicle Vehicle { get; set; }
	[Property] public bool HasInput { get; set; } = true;
	[Property] public List<VehicleExitVolume> ExitVolumes { get; set; }

	[HostSync] public Player Player { get; private set; }

	public bool CanEnter( Player player )
	{
		return !Player.IsValid();
	}

	[Broadcast]
	private void BroadcastEnteredVehicle( Player player )
	{
		player.GameObject.SetParent( GameObject, false );

		// Zero out our transform
		player.Transform.Local = new Transform();
	}

	[Broadcast]
	private void RpcEnter( Player player )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		Player = player;
		player.CurrentSeat = this;

		BroadcastEnteredVehicle( player );

		if ( player.CurrentEquipment.IsValid() )
		{
			player.CurrentEquipment?.Holster();
		}

		if ( HasInput )
		{
			Network.AssignOwnership( player.Network.OwnerConnection );
		}
	}

	public bool Enter( Player player )
	{
		if ( !CanEnter( player ) )
		{
			return false;
		}

		Log.Info( "Trying to enter a vehicle" );

		player.GameObject.SetParent( GameObject, false );
		player.TimeSinceSeatChanged = 0;

		using ( Rpc.FilterInclude( Connection.Host ) )
		{
			RpcEnter( player );
		}

		return true;
	}

	public bool CanLeave( Player player )
	{
		if ( !Player.IsValid() )
		{
			return false;
		}

		if ( Player.TimeSinceSeatChanged < 0.5f )
		{
			return false;
		}

		if ( Player != player )
		{
			return false;
		}

		return true;
	}

	[Broadcast]
	private void RpcLeave()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		Player.SetCurrentEquipment( Player.Inventory.Equipment.FirstOrDefault() );

		Player.CurrentSeat = null;
		Player = null;

		if ( HasInput )
		{
			Network.DropOwnership();
		}
	}

	public bool Leave( Player player )
	{
		if ( !CanLeave( player ) )
		{
			return false;
		}

		player.GameObject.SetParent( null, true );

		// Move player to best exit point
		player.Transform.Position = FindExitLocation();
		player.CharacterController.Velocity = Vehicle.Rigidbody.Velocity;

		using ( Rpc.FilterInclude( Connection.Host ) )
		{
			RpcLeave();
		}

		return true;
	}

	public Vector3 FindExitLocation()
	{
		// TODO: Multiple volumes (e.g. fallback)
		return ExitVolumes[0].CheckClosestFreeSpace( Transform.Position );
	}

	internal void Eject()
	{
		Leave( Player );
	}
}
