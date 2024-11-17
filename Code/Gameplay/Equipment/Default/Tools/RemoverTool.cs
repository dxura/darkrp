using Dxura.Darkrp.UI;

namespace Dxura.Darkrp.Tools;

[Tool( "Remover", "Remove GameObjects", "Construction" )]
public class RemoverTool : BaseTool
{
    public override string Attack1Control => "Remove selected object";
    
    public override async void PrimaryUseStart()
    {
	    NotificationPanel.Instance.AddEntry("", "Implement me bozo");
        // var tr = Game.ActiveScene.Trace.Ray( new Ray( Toolgun.Player.Head.WorldPosition, Toolgun.Player.Direction.Forward ), 2000 )
        //     .Run();
        //
        // if ( !tr.Hit ) return;
        // if ( tr.GameObject.Tags.HasAny( "player", "grabbed", "map") ) return;
        //
        //
        // Toolgun.BroadcastUseEffects( tr.HitPosition, tr.Normal );
        //
        //
        // if ( tr.GameObject.Tags.Has( "persist" ) )
        // {
	       //  var propHelper = tr.GameObject.GetComponent<PropHelper>();
        //
	       //  if ( propHelper == null )
	       //  {
		      //   NotificationPanel.Instance?.AddEntry("error", "Not valid prop!", 3f);
		      //   return;
	       //  }
        //
	       //  if ( !await PersistenceManager.Instance.DeleteProp( propHelper.PropData.id ) )
	       //  {
		      //   return;
	       //  }
        // }
        //
        //
        // GameManager.Instance.BroadcastDestroyObject( tr.GameObject.Id );
        //
        // if ( tr.Body.IsValid() )
        // {
	       //  var position = tr.Body.GetBounds().Center;
	       //  var rotation = tr.Body.Transform.Rotation;
	       //  var size = tr.Body.GetBounds().Size * tr.Body.Transform.Scale;
	       //  GameManager.Instance.BroadcastDestroyObjectEffect( position, rotation, size );
        // }
    }
}
