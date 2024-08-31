namespace Dxura.Darkrp;

public sealed class BoneSwapperUtility : Component
{
	[Property] public float Time { get; set; } = 0.5f;
	public RealTimeSince TimeSinceStart { get; set; } = 0;
	private bool done = false;

	[Property] public GameObject Target { get; set; }
	[Property] public GameObject NewParent { get; set; }

	protected override void OnUpdate()
	{
		if ( TimeSinceStart > Time && !done )
		{
			done = true;
			Target.SetParent( NewParent, false );
		}
	}
}
