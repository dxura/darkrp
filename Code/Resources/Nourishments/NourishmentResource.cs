namespace Dxura.Darkrp;
public enum NourishmentTypes { Food, Drink };
[GameResource("Darkrp/Nourishment", "nrsh", "A basic nourishment definition", IconBgColor = "#5877E0", Icon = "restaurant")]
public class NourishmentResource : GameResource
{
  [Category("Display")] public required string Name { get; set; }

  [Category("Appearance")] public Color ModelColor { get; set; }
	[Category("Appearance")] public required Model Model { get; set; }
  [Category("Appearance")] public Material? ModelMaterial { get; set; }

  [Category("Logic")] public NourishmentTypes NourishmentType { get; set; }
  [Category("Logic")] public int Price { get; set; }
  [Category("Logic")] public SoundEvent? UseSound { get; set; }
  [Category("Logic")] public int HealAmount { get; set; }
}