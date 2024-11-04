namespace Dxura.Darkrp;

public class PickaxeEquipment : InputWeaponComponent
{
    // The range the player can interact with rocks
    [Property] private float InteractRange { get; set; } = 60f;

    // The damage the pickaxe does to rocks
    private float PickaxeDamage { get; set; } = 10f;
    [Property] public EquipmentResource CurrentPickaxeResource { get; set; } = null!;
    private float _lastPickupTime;
    private const float DeltaPickupTime = 0.90f;
    protected override void OnStart()
    {
        if (IsProxy)
        {
            Enabled = false;
        }

        PickaxeDamage = CurrentPickaxeResource.PickaxeDamage;
    }

    protected override void OnInputUpdate()
    {
        // Starting position of the line (camera position)
        var start = Player.CameraGameObject!.Transform.Position;

        // Direction of the line (the direction the camera is facing)
        var direction = Player.CameraGameObject!.Transform.World.Forward;

        // Calculate the end position based on direction and interact range
        var end = start + direction * InteractRange;

        if (Input.Down("attack1") && RealTime.Now - _lastPickupTime > DeltaPickupTime)
        {
            SceneTraceResult tr = Scene.Trace.Ray(start, end).Run();

            if (tr.Hit && tr.GameObject.Tags.Has("rock"))
            {

                tr.GameObject.Parent!.Components.Get<HealthComponent>().Health -= PickaxeDamage;
                Sound.Play("rock_chop");
                _lastPickupTime = RealTime.Now;

            }
        }
    }
}