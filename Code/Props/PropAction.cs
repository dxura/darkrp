
using Dxura.Darkrp.UI;

namespace Dxura.Darkrp;

/// <summary>
/// Represents an action related to prop management that can be undone.
/// </summary>
public class PropAction : IUndoable
{
	private readonly PropManager _propManager;
	private GameObject Prop { get; }
	private string Name { get; }
	private Vector3 Position { get; }
	private Rotation Rotation { get; }

	public PropAction( PropManager propManager, GameObject prop, string name )
	{
		_propManager = propManager;
		Prop = prop;
		Position = prop.Transform.Position;
		Rotation = prop.Transform.Rotation;
		Name = name;
	}

	/// <summary>
	/// Undoes the prop creation by destroying the prop and removing it from the list.
	/// </summary>
	public void Undo()
	{
		Prop.Destroy();
		_propManager.OwnedProps.Remove( Prop );
		Toast.Instance.Show( $"Undo prop {Name}" );
	}
}
