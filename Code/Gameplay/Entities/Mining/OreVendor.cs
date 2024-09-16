using Dxura.Darkrp.UI;
using Sandbox;

namespace Dxura.Darkrp;

[Title( "Ore Vendor" )]
[Category( "Entities" )]
public sealed class AtmEntity : BaseEntity
{

	[Property] public OreVendorMenu? _oreVendorMenu { get; set; }

	/// The player state
	public PlayerState PlayerState => PlayerState.Local;

	public override void OnUse( Player player )
	{  
		if(PlayerState.Local?.Job.Name != "Miner")
		{
        	Toast.Instance.Show($"[Ore Vendor] You must be a miner to sell ores!");
        	return; 
		}
		else
		{
			_oreVendorMenu.IsVendorOpen = true;
		}

		
	}

}