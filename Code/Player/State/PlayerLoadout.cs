namespace Dxura.Darkrp;

public class PlayerLoadout : Component
{
	[Property] public PlayerState PlayerState { get; set; }

	[Property] public List<EquipmentResource> Equipment { get; set; }
	[Property] [HostSync] public bool HasDefuseKit { get; set; }

	/// <summary>
	/// Clears the player's loadout equipment.
	/// </summary>
	public void SetFrom( Player player )
	{
		Equipment.Clear();
		Equipment.AddRange( player.Inventory.Equipment.Select( x => x.Resource ) );
	}
}
