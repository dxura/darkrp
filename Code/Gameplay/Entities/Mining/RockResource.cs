
[GameResource("Darkrp/Mining/Rock Item", "rck", "A basic rock definition", IconBgColor = "#58c0e0", Icon = "ui/equipment/pickaxe.png")]
public class RockResource : GameResource
{
	[Category( "General" )] public string? Name { get; set; }
	[Category( "General" )] public string? Description { get; set; }
	[Category( "General" )] public float Health { get; set; }
	[Category( "General" )] public Model? Model { get; set; }
	[Category( "General" )] public int RespawnTime { get; set; }
	[Category( "General" )] public SoundEvent? HitSound { get; set; }
}
