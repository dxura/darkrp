using Dxura.Darkrp;

namespace Dxura.Darkrp;

public partial class PlayerBody : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public Player Player { get; set; }

	public Vector3 DamageTakenPosition { get; set; }
	public Vector3 DamageTakenForce { get; set; }

	private bool _isFirstPerson = true;
	public bool IsRagdoll => Physics.Enabled;

	internal void SetRagdoll( bool ragdoll )
	{
		Physics.Enabled = ragdoll;
		Renderer.UseAnimGraph = !ragdoll;

		GameObject.Tags.Set( "ragdoll", ragdoll );

		if ( !ragdoll )
		{
			GameObject.Transform.LocalPosition = Vector3.Zero;
			GameObject.Transform.LocalRotation = Rotation.Identity;
		}

		SetFirstPersonView( !ragdoll );

		if ( ragdoll && DamageTakenForce.LengthSquared > 0f )
		{
			ApplyRagdollImpulses( DamageTakenPosition, DamageTakenForce );
		}

		Transform.ClearInterpolation();
	}

	internal void ApplyRagdollImpulses( Vector3 position, Vector3 force )
	{
		if ( !Physics.IsValid() || !Physics.PhysicsGroup.IsValid() )
		{
			return;
		}

		foreach ( var body in Physics.PhysicsGroup.Bodies )
		{
			body.ApplyImpulseAt( position, force );
		}
	}

	public void Refresh()
	{
		SetFirstPersonView( _isFirstPerson );
	}

	public void SetFirstPersonView( bool firstPerson )
	{
		_isFirstPerson = firstPerson;

		// Disable the player's body so it doesn't render.
		Renderer.Enabled = !firstPerson;
	}
}
