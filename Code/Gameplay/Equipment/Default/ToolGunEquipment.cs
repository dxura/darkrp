namespace Dxura.Darkrp;

using Sandbox;
public class ToolGunEquipment : InputWeaponComponent
{
    public TimeSince TimeSinceLastScreen { get; set; }
    [Property] public GameObject TextScreenPrefab { get; set; } = null!;
    [Property] public float ScreenCooldown { get; set; } = 3.0f;
    protected override void OnInputUpdate()
	{

        Ray WeaponRay = Equipment.Owner?.AimRay ?? new Ray( );

        if ( Input.Down( "attack1" ) )
        {
            SceneTraceResult tr = Scene.Trace.Ray( WeaponRay, 2000 )
				.Run();

            // If we hit, draw a sphere
            if ( tr.Hit )
            {
                Gizmo.Draw.SolidSphere( tr.HitPosition, 10.0f );
                if ( TextScreenPrefab != null && TimeSinceLastScreen > ScreenCooldown )
                {
                    TimeSinceLastScreen = 0;
                    Log.Info("Creating a text screen");
                    var eyePos = WeaponRay.Position;
		            var eyeDir = WeaponRay.Forward;
		            var eyeRot = Rotation.From( new Angles( 0.0f, Equipment.Owner?.EyeAngles.yaw ?? 0f, 0.0f ) );
                    GameObject itemEntity = TextScreenPrefab.Clone(tr.HitPosition - Vector3.Forward * 2);
                    itemEntity.Transform.Rotation *= eyeRot * Rotation.From( new Angles( 0.0f, 180.0f, 0.0f ) );

                    TextScreen TextScreen = itemEntity.Components.Get<TextScreen>();
                    TextScreen.Text = "Hello World!";

                    itemEntity.NetworkSpawn();
                }
            }
        }
    }
}