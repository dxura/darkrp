using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace Dxura.Darkrp;

public record EquipmentDroppedEvent( DroppedEquipment Dropped, Player Player ) : IGameEvent;

public record EquipmentPickedUpEvent( Player Player, DroppedEquipment Dropped, Equipment Equipment ) : IGameEvent;

public partial class DroppedEquipment : Component, IUse
{
	[Property] public EquipmentResource Resource { get; set; }

	public Rigidbody Rigidbody { get; private set; }

	public static DroppedEquipment Create( EquipmentResource resource, Vector3 positon, Rotation? rotation = null,
		Equipment heldWeapon = null, bool networkSpawn = true )
	{
		Assert.True( Networking.IsHost );

		var go = new GameObject();
		go.Transform.Position = positon;
		go.Transform.Rotation = rotation ?? Rotation.Identity;
		go.Name = resource.Name;
		go.Tags.Add( "pickup" );

		var droppedWeapon = go.Components.Create<DroppedEquipment>();
		droppedWeapon.Resource = resource;

		var renderer = go.Components.Create<SkinnedModelRenderer>();
		renderer.Model = resource.WorldModel;

		var collider = go.Components.Create<BoxCollider>();
		collider.Scale = resource.DroppedSize;
		collider.Center = resource.DroppedCenter;

		droppedWeapon.Rigidbody = go.Components.Create<Rigidbody>();

		Game.ActiveScene.Dispatch( new EquipmentDroppedEvent( droppedWeapon, heldWeapon?.Owner ) );

		if ( heldWeapon is not null )
		{
			foreach ( var state in heldWeapon.Components.GetAll<IDroppedWeaponState>() )
			{
				state.CopyToDroppedWeapon( droppedWeapon );
			}
		}

		if ( networkSpawn )
		{
			go.NetworkSpawn();
		}

		return droppedWeapon;
	}

	public UseResult CanUse( Player player )
	{
		if ( player.Inventory.CanTake( Resource ) != PlayerInventory.PickupResult.Pickup )
		{
			return "Can't pick this up";
		}

		return true;
	}

	private bool _isUsed;

	public void OnUse( Player player )
	{
		if ( _isUsed )
		{
			return;
		}

		_isUsed = true;

		if ( !player.IsValid() )
		{
			return;
		}

		var currentActiveSlot = player.CurrentEquipment?.Resource.Slot ?? EquipmentSlot.Melee;
		var weapon = player.Inventory.Give( Resource, Resource.Slot < currentActiveSlot );

		if ( !weapon.IsValid() )
		{
			return;
		}

		foreach ( var state in weapon.Components.GetAll<IDroppedWeaponState>() )
		{
			state.CopyFromDroppedWeapon( this );
		}

		Game.ActiveScene.Dispatch( new EquipmentPickedUpEvent( player, this, weapon ) );

		GameObject.Destroy();
	}
}
