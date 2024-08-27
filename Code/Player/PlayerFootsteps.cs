namespace Dxura.Darkrp.Player;

[Group( "Player" )]
[Title( "Player Footsteps" )]
public sealed class PlayerFootsteps : Component
{
	[Property] private SkinnedModelRenderer? Source { get; set; }
	
	private TimeSince _timeSinceStep;

	protected override void OnEnabled()
	{
		if ( Source is null )
		{
			return;
		}

		Source.OnFootstepEvent += OnEvent;
	}

	protected override void OnDisabled()
	{
		if ( Source is null )
		{
			return;
		}

		Source.OnFootstepEvent -= OnEvent;
	}

	private void OnEvent( SceneModel.FootstepEvent e )
	{
		if ( _timeSinceStep < 0.2f )
		{
			return;
		}

		var tr = Scene.Trace
			.Ray( e.Transform.Position + Vector3.Up * 20, e.Transform.Position + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
		{
			return;
		}

		if ( tr.Surface is null )
		{
			return;
		}

		_timeSinceStep = 0;

		var sound = e.FootId == 0 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;
		if ( sound is null )
		{
			return;
		}

		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		handle.Volume *= e.Volume;
	}
}
