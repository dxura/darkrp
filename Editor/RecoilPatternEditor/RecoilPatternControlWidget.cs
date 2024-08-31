using Dxura.Darkrp;
using Editor;
using Sandbox;

namespace Dxura.Darkrp.Editor;

[CustomEditor( typeof( RecoilPattern ) )]
public class RecoilPatternControlWidget : ControlWidget
{
	public RecoilPatternControlWidget( SerializedProperty property ) : base( property )
	{
		FixedHeight = 256;
		Layout = Layout.Column();

		var editor = new RecoilPatternEditor( null );
		editor.SetProperty( property );
		Layout.Add( editor );
	}
}
