using System.Drawing;

namespace GameSystems.Jobs;

[GameResource( "Job Group Definition", "group", "" )]
public class JobGroupResource : GameResource
{
	[Category( "Description" )] public string Name { get; set; } = null!;

	[Category( "Gameplay" )] public Color Color { get; set; }
}
