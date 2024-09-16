using Dxura.Darkrp;
using GameSystems.Jobs;

namespace Dxura.Darkrp;

/// <summary>
/// What slot is this equipment for?
/// </summary>
public enum EquipmentSlot
{
	Undefined = 0,

	/// <summary>
	/// Core/essential items 
	/// </summary>
	Primary = 1,

	/// <summary>
	/// Everything else
	/// </summary>
	Secondary = 2,

	/// <summary>
	/// Knives etc.
	/// </summary>
	Melee = 3,

	/// <summary>
	/// Grenades etc.
	/// </summary>
	Utility = 4,

	/// <summary>
	/// C4 etc.
	/// </summary>
	Special = 5
}

/// <summary>
/// A resource definition for a piece of equipment. This could be a weapon, or a deployable, or a gadget, or a grenade.. Anything really.
/// </summary>
[GameResource( "DarkRp/Equipment Item", "equip", "", IconBgColor = "#5877E0", Icon = "track_changes" )]
public class EquipmentResource : GameResource
{
	public static HashSet<EquipmentResource> All { get; set; } = new();

	[Category( "Base" )] public string Name { get; set; } = "My Equipment";

	[Category( "Base" )] public string Description { get; set; } = "";

	[Category( "Base" )] public EquipmentSlot Slot { get; set; }

	/// <summary>
	/// The equipment's icon
	/// </summary>
	[Group( "Base" )]
	[ImageAssetPath]
	public string Icon { get; set; }

	/// <summary>
	/// Is this equipment shown in the buy menu
	/// </summary>
	[Category( "Economy" )]
	public bool IsPurchasable { get; set; } = true;

	/// <summary>
	/// How much is this equipment to buy in the buy menu?
	/// </summary>
	[Category( "Economy" )]
	public int Price { get; set; } = 0;

	/// <summary>
	/// The prefab to create and attach to the player when spawning it in.
	/// </summary>
	[Category( "Prefabs" )]
	public GameObject MainPrefab { get; set; }

	/// <summary>
	/// The prefab to create when making a viewmodel for this equipment.
	/// </summary>
	[Category( "Prefabs" )]
	public GameObject ViewModelPrefab { get; set; }

	/// <summary>
	/// The equipment's model
	/// </summary>
	[Category( "Information" )]
	public Model WorldModel { get; set; }

	[Category( "Dropping" )] public Vector3 DroppedSize { get; set; } = new(8, 2, 8);

	[Category( "Dropping" )] public Vector3 DroppedCenter { get; set; } = new(0, 0, 0);
	[Category( "Dropping" )] public bool CanDrop { get; set; } = true;
	[Category( "Damage" )] public float? ArmorReduction { get; set; }

	[Category( "Damage" )] public float? HelmetReduction { get; set; }

	/// <summary>
	/// Mining system pickaxe damage
	/// </summary>
	[Category( "Mining" )] public float PickaxeDamage { get; set; }



	protected override void PostLoad()
	{
		if ( All.Contains( this ) )
		{
			Log.Warning( "Tried to add two of the same equipment (?)" );
			return;
		}

		All.Add( this );
	}
}
