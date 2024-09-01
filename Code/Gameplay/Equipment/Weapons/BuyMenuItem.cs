namespace Dxura.Darkrp;

public abstract class BuyMenuItem
{
	public string Id { get; protected init; }
	public string Name { get; protected init; }
	public string Icon { get; protected init; }

	public virtual int GetPrice( Player player )
	{
		return 0;
	}

	public virtual bool IsOwned( Player player )
	{
		return true;
	}

	public virtual bool IsVisible( Player player )
	{
		return true;
	}

	protected virtual void OnPurchase( Player player ) { }

	public void Purchase( Player player )
	{
		if ( IsOwned( player ) )
		{
			return;
		}

		var price = GetPrice( player );
		player.PlayerState.GiveMoney( -price );
		OnPurchase( player );
	}

	public static IEnumerable<BuyMenuItem> GetAll()
	{
		return new List<BuyMenuItem>
		{
			new ArmorEquipment( "kevlar", "Kevlar", "ui/equipment/armor.png" ),
			new ArmorWithHelmetEquipment( "kevlar_helmet", "Kevlar + Helmet", "ui/equipment/helmet.png" ),
		};
	}

	public static BuyMenuItem? GetById( string id )
	{
		return GetAll().FirstOrDefault( x => x.Id == id );
	}
}

public class ArmorEquipment : BuyMenuItem
{
	public ArmorEquipment( string id, string name, string icon )
	{
		Id = id;
		Name = name;
		Icon = icon;
	}

	public override int GetPrice( Player player )
	{
		return 650;
	}

	protected override void OnPurchase( Player player )
	{
		player.ArmorComponent.Armor = player.ArmorComponent.MaxArmor;
	}

	public override bool IsOwned( Player player )
	{
		return player.ArmorComponent.Armor == player.ArmorComponent.MaxArmor;
	}
}

public class ArmorWithHelmetEquipment : BuyMenuItem
{
	public ArmorWithHelmetEquipment( string id, string name, string icon )
	{
		Id = id;
		Name = name;
		Icon = icon;
	}

	public override int GetPrice( Player player )
	{
		if ( player.ArmorComponent.Armor == player.ArmorComponent.MaxArmor )
		{
			return 350;
		}

		return 1000;
	}

	protected override void OnPurchase( Player player )
	{
		player.ArmorComponent.Armor = player.ArmorComponent.MaxArmor;
		player.ArmorComponent.HasHelmet = true;
	}

	public override bool IsOwned( Player player )
	{
		return player.ArmorComponent.Armor == player.ArmorComponent.MaxArmor && player.ArmorComponent.HasHelmet;
	}
}
