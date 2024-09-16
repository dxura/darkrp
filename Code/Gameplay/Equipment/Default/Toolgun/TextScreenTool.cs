using Sandbox;

namespace Dxura.Darkrp;

public class TextScreenTool : Tool
{
    public override string Name => "Text Screen";
    public override string Description => "A tool that allows you to create text screens";

    public override string Icon => "ui/equipment/physgun.png";

    public TimeSince TimeSinceLastScreen { get; set; }

    /// <summary>
    /// Gets the prefab for the text screen, probably need a better way to do this.
    /// </summary>
    public GameObject TextScreenPrefab { get; set; } = SceneUtility.GetPrefabScene(ResourceLibrary.Get<PrefabFile>("gameplay/entities/textscreen/textscreen.prefab"));
    public float ScreenCooldown { get; } = 3.0f;

    public override void UseTool(Player Player)
    {
        Ray WeaponRay = Player.AimRay;

        SceneTraceResult tr = Scene.Trace.Ray(WeaponRay, 2000)
                        .Run();

        if (tr.Hit)
        {
            if (TextScreenPrefab != null && TimeSinceLastScreen > ScreenCooldown)
            {
                TimeSinceLastScreen = 0;
                Log.Info("Creating a text screen");
                var eyePos = WeaponRay.Position;
                var eyeDir = WeaponRay.Forward;
                var eyeRot = Rotation.From(new Angles(0.0f, Player.EyeAngles.yaw, 0.0f));
                GameObject itemEntity = TextScreenPrefab.Clone(tr.HitPosition + (tr.Normal * 2));
                itemEntity.Transform.Rotation *= Rotation.From(tr.Normal.EulerAngles);

                TextScreen TextScreen = itemEntity.Components.Get<TextScreen>();
                TextScreen.Text = "Hello World!";

                itemEntity.NetworkSpawn();
            }
        }
    }

}