using Sandbox;

namespace Dxura.Darkrp;

public class TextScreenTool : Tool
{
    public override string Name => "Text Screen";
    public override string Description => "A tool that allows you to create text screens";

    public override string Icon => "ui/equipment/physgun.png";

    public TimeSince TimeSinceLastScreen { get; set; }

    private List<IUndoable> History { get; set; } = new();

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
                itemEntity.WorldRotation *= Rotation.From(tr.Normal.EulerAngles);

                TextScreen TextScreen = itemEntity.Components.Get<TextScreen>();
                TextScreen.Text = "Hello World!";

                itemEntity.NetworkSpawn();

                History.Add(TextScreen);
            }
        }
    }

    public override void OpenToolOptions(Player Player)
    {
        Log.Info("Opening tool options");
    }

    public void UndoLastAction()
    {
        if (History.Count > 0)
        {
            History.Last().Undo();
            History.RemoveAt(History.Count - 1);
        }
    }

    protected override void OnFixedUpdate()
    {
        base.OnFixedUpdate();

        // Handle undo input
        if (Input.Pressed("Undo"))
        {
            try
            {
                UndoLastAction();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}