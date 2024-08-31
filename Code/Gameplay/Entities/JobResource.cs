namespace GameSystems.Jobs;

[GameResource( "Entity Definition", "entity", "" )]
public class EntityResource : GameResource
{
	public GameObject Prefab { get; set; }
	
	[Category( "Financial" )] public int Price { get; set; }
	
	[Category( "Description" )] public string Name { get; set; } = null!;
	[Category( "Description" )] public string Description { get; set; } = null!;
	[Category( "Description" )] public Color Color { get; set; }

	[Category( "Permission" )] public JobResource[] Whitelist { get; set; } = null!;
}
