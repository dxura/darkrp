using GameSystems.Jobs;

namespace Dxura.Darkrp;

public static class JobProvider
{
	public static Dictionary<string, JobResource> Jobs { get; } = new();
	public static Dictionary<string, JobGroupResource> JobGroups { get; } = new();

	// On Start load all jobs
	static JobProvider()
	{
		Log.Info( "Loading groups..." );

		// Due to them being static, we need to clear them first incase of any changes.
		// I don't know if this is necessary, but it's better to be safe than sorry.
		Jobs.Clear();
		JobGroups.Clear();
		
		// Get all JobGroup resources
		foreach ( var group in ResourceLibrary.GetAll<JobGroupResource>() )
		{
			Log.Info( $"Loading group: {group.Name}" );
			JobGroups[group.Name] = group;
		}

		Log.Info( "Loading jobs..." );
		
		// Get all Job resources
		foreach ( var job in ResourceLibrary.GetAll<JobResource>() )
		{
			Log.Info( $"Loading job: {job.Name}" );
			Jobs[job.Name] = job;
		}
	}
	
	// Get default job when player spawns
	public static JobResource Default()
	{
		return ResourceLibrary.Get<JobResource>( "gameplay/jobs/citizen.job" );
	}
}
