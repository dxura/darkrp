using Dxura.Darkrp.Tools;

namespace Dxura.Darkrp;

public class ToolgunEquipment : InputWeaponComponent
{

    [Property, Group( "Sounds" )] SoundEvent UseSound { get; set; }

    [Property, Group( "Prefabs" )] GameObject LinePrefab { get; set; }
    [Property, Group( "Prefabs" )] GameObject MarkerPrefab { get; set; }

    public BaseTool? CurrentTool = null;

    protected override void OnStart()
    {
        if ( CurrentTool == null )
            SetTool( TypeLibrary.GetType<BaseTool>( "Dxura.Darkrp.Tools.RemoverTool" ) );
    }

    protected override void OnInputUpdate()
    {
        if ( Input.Pressed( "attack1" ) ) CurrentTool?.PrimaryUseStart();
        if ( Input.Down( "attack1" ) ) CurrentTool?.PrimaryUseUpdate();
        if ( Input.Released( "attack1" ) ) CurrentTool?.PrimaryUseEnd();

        if ( Input.Pressed( "attack2" ) ) CurrentTool?.SecondaryUseStart();
        if ( Input.Down( "attack2" ) ) CurrentTool?.SecondaryUseUpdate();
        if ( Input.Released( "attack2" ) ) CurrentTool?.SecondaryUseEnd();

        if ( Input.Pressed( "reload" ) ) CurrentTool?.ReloadUseStart();
        if ( Input.Down( "reload" ) ) CurrentTool?.ReloadUseUpdate();
        if ( Input.Released( "reload" ) ) CurrentTool?.ReloadUseEnd();
    }

    public void SetTool( TypeDescription toolDescription )
    {
        if ( CurrentTool != null )
        {
            if ( CurrentTool.GetType() == toolDescription.TargetType ) return;

            CurrentTool?.OnUnequip();
            CurrentTool = null;
        }

        if ( toolDescription == null ) return;

        CurrentTool = TypeLibrary.Create<BaseTool>( toolDescription.TargetType );
        CurrentTool.Toolgun = this;
        CurrentTool?.OnEquip();

        // ToolMenu.Instance?.UpdateInspector();
    }


    
    [Broadcast]
    public void BroadcastUseEffects( Vector3 hitPosition, Vector3 hitNormal = default )
    {
        // var startPosition = (Player?.ViewModel?.Muzzle ?? Muzzle).WorldPosition;
        //
        // var playerRenderer = Player?.Body?.Components?.Get<SkinnedModelRenderer>();
        // playerRenderer?.Set( "b_attack", true );
        // Player?.ViewModel?.ModelRenderer?.Set( "b_attack", true );
        //
        // MarkerPrefab.Clone( hitPosition, Rotation.LookAt( hitNormal, Vector3.Up ) );
        // var lineObj = LinePrefab.Clone( startPosition );
        // lineObj.BreakFromPrefab();
        // var line = lineObj.Components.Get<LineParticle>( FindMode.EverythingInSelfAndDescendants );
        // line.Init( startPosition, hitPosition );
        //
        // var sound = Sound.Play( UseSound, startPosition );
        // if ( Connection.Local.Id == Rpc.CallerId ) sound.ListenLocal = true;
    }

}
