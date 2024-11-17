using Dxura.Darkrp.Tools;

namespace Dxura.Darkrp;

public class ToolgunEquipment : InputWeaponComponent
{

    [Property, Group( "Sounds" )] SoundEvent UseSound { get; set; }

    [Property, Group( "Prefabs" )] GameObject LinePrefab { get; set; }
    [Property, Group( "Prefabs" )] GameObject MarkerPrefab { get; set; }

    private BaseTool? _currentTool = null;

    protected override void OnStart()
    {
        if ( _currentTool == null )
            SetTool( TypeLibrary.GetType<BaseTool>( "Dxura.Darkrp.Tools.RemoverTool" ) );
    }

    protected override void OnInputUpdate()
    {
        if ( Input.Pressed( "attack1" ) ) _currentTool?.PrimaryUseStart();
        if ( Input.Down( "attack1" ) ) _currentTool?.PrimaryUseUpdate();
        if ( Input.Released( "attack1" ) ) _currentTool?.PrimaryUseEnd();

        if ( Input.Pressed( "attack2" ) ) _currentTool?.SecondaryUseStart();
        if ( Input.Down( "attack2" ) ) _currentTool?.SecondaryUseUpdate();
        if ( Input.Released( "attack2" ) ) _currentTool?.SecondaryUseEnd();

        if ( Input.Pressed( "reload" ) ) _currentTool?.ReloadUseStart();
        if ( Input.Down( "reload" ) ) _currentTool?.ReloadUseUpdate();
        if ( Input.Released( "reload" ) ) _currentTool?.ReloadUseEnd();
    }

    public void SetTool( TypeDescription toolDescription )
    {
        if ( _currentTool != null )
        {
            if ( _currentTool.GetType() == toolDescription.TargetType ) return;

            _currentTool?.OnUnequip();
            _currentTool = null;
        }

        if ( toolDescription == null ) return;

        _currentTool = TypeLibrary.Create<BaseTool>( toolDescription.TargetType );
        _currentTool.Toolgun = this;
        _currentTool?.OnEquip();

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
