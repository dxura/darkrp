using Dxura.Darkrp.UI;
using Sandbox;

namespace Dxura.Darkrp;

[Title( "Ore Vendor" )]
[Category( "Entities" )]
public sealed class AtmEntity : BaseEntity
{

	[Property] public OreVendorMenu? _oreVendorMenu { get; set; }
	public override void OnUse( Player player )
	{   
		_oreVendorMenu.IsVendorOpen = true;
	}

}