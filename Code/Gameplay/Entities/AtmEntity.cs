using Dxura.Darkrp.UI;
using Sandbox;

namespace Dxura.Darkrp;

[Title( "Atm" )]
[Category( "Entities" )]
public sealed class AtmEntity : BaseEntity
{

	[Property] public AtmMainMenu? MainMenu { get; set; }
	public override void OnUse( Player player )
	{
		MainMenu.IsAtmOpen = true;
	}
}
