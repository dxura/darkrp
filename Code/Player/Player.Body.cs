using System.Text.Json.Serialization;
using Dxura.Darkrp;
using GameSystems.Jobs;

namespace Dxura.Darkrp;

public partial class Player
{
	[Property, Feature("Body")] public required GameObject BodyRoot { get; set; }
	[Property, Feature("Body")] public required SkinnedModelRenderer Renderer { get; set; }
	[Property, Feature("Body")] public required ModelPhysics Physics { get; set; }

	public Vector3 DamageTakenPosition { get; set; }
	public Vector3 DamageTakenForce { get; set; }

	private bool _isFirstPerson = true;
	public bool IsRagdoll => Physics.Enabled;

	private void SetRagdoll( bool ragdoll )
	{
		Physics.Enabled = ragdoll;
		Renderer.UseAnimGraph = !ragdoll;

		GameObject.Tags.Set( "ragdoll", ragdoll );

		if ( !ragdoll )
		{
			BodyRoot.LocalPosition = Vector3.Zero;
			BodyRoot.LocalRotation = Rotation.Identity;
		}

		SetFirstPersonView( !ragdoll );

		if ( ragdoll && DamageTakenForce.LengthSquared > 0f )
		{
			ApplyRagdollImpulses( DamageTakenPosition, DamageTakenForce );
		}

		Transform.ClearInterpolation();
	}

	private void ApplyRagdollImpulses( Vector3 position, Vector3 force )
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

	public void RefreshBody()
	{
		SetFirstPersonView( _isFirstPerson );
	}

	public void SetFirstPersonView( bool firstPerson )
	{
		_isFirstPerson = firstPerson;

		// Disable the player's body so it doesn't render.
		Renderer.Enabled = !firstPerson;
	}
	
	/// <summary>
	/// Called to wear an outfit based off a job.
	/// </summary>
	/// <param name="job"></param>
	[Broadcast( NetPermission.HostOnly )]
	private void UpdateBodyFromJob( JobResource job )
	{
		// Renderer.Model = Game.Random.FromList( job.Models );
		// RefreshBody();
	}
}
