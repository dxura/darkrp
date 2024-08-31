namespace GameSystems.Jobs;

[GameResource( "Job Definition", "job", "" )]
public class JobResource : GameResource
{
	[Category( "Description" )] public string Name { get; set; } = null!;
	[Category( "Description" )] public string Description { get; set; } = null!;
	[Category( "Description" )] public Color Color { get; set; }
	[Category( "Description" )] public JobGroupResource Group { get; set; } = null!;
	[Category( "Description" )] public int SortingLevel { get; set; }

	[Category( "Appearance" )] public List<Model> Models { get; set; } = null!;

	[Category( "Gameplay" )] public float Salary { get; set; }
	[Category( "Gameplay" )] public int MaxWorkers { get; set; }
	[Category( "Gameplay" )] public List<EquipmentResource> Equipment { get; set; } = new();
	[Category( "Gameplay" )] public bool Vote { get; set; }
}
