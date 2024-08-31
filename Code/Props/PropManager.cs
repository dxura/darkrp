using Dxura.Darkrp.UI;

namespace Dxura.Darkrp;

public sealed class PropManager : SingletonComponent<PropManager>
{
	
	/// <summary>
	/// List of props we can spawn (Local)
	/// </summary>
	[Property] public List<Model> Props { get; set; } = new();

	[Property] public GameObject PropPrefab { get; set; } = null!;
	[Property] public int PropLimit { get; set; } = 10;
	[Property] public float SpawnProtectionTimeWindow { get; set; } = 1;


	// List to store currently spawned props.
	public List<GameObject> OwnedProps { get; set; } = new();

	// History for undo actions
	private List<IUndoable> History { get; set; } = new();

	// for spawn protection
	private TimeSince _timeSinceLastSpawn;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		// Handle undo input
		if ( Input.Pressed( "Undo" ) )
		{
			try
			{
				UndoLastAction();
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}
		}
	}
	
	public void SpawnProp(string identifier, bool isCloudModel)
	{
	    // Check if the time since the last spawn is less than the allowed time window
	    if (_timeSinceLastSpawn <= SpawnProtectionTimeWindow)
	    {
	        TriggerSpawnProtection();
	        return;
	    }

	    // Check if the prop limit has been reached
	    if (OwnedProps.Count >= PropLimit)
	    {
	        NotifyPropLimit();
	        return;
	    }

	    var localPlayerCamera = PlayerState.Local?.Player?.CameraGameObject;
	    if (localPlayerCamera == null)
	    {
	        Log.Warning("Local player camera not found.");
	        return;
	    }

	    // Calculate spawn position based on the player's camera
	    Vector3 spawnPosition = CalculateSpawnPosition(localPlayerCamera);

	    // Clone the prop prefab at the calculated position
	    var prop = PropPrefab.Clone(spawnPosition);

	    // Set up the prop based on whether it's a cloud model or local model
	    if (isCloudModel)
	    {
	        var propHelper = prop.Components.GetOrCreate<PropHelper>();
	        if (propHelper != null)
	        {
	            propHelper.SetCloudModel(identifier);
	        }
	        else
	        {
	            Log.Warning($"PropHelper component not found on prop prefab for cloud model: {identifier}");
	            prop.Destroy();
	            return;
	        }
	    }
	    else
	    {
	        var propComp = prop.Components.Get<Prop>();
	        if (propComp != null)
	        {
	            propComp.Model = Model.Load(identifier);
	        }
	        else
	        {
	            Log.Warning($"Prop component not found on prop prefab for local model: {identifier}");
	            prop.Destroy();
	            return;
	        }
	    }

	    // Spawn the prop on all clients
	    if (prop.NetworkSpawn())
	    {
	        OwnedProps.Add(prop);
	        History.Add(new PropAction(this, prop, identifier));
	        NotifyPropSpawned(identifier);

	        // Reset the timer 
	        _timeSinceLastSpawn = 0;
	    }
	    else
	    {
	        Log.Warning($"Network spawn failed for {(isCloudModel ? "cloud" : "local")} model: {identifier}");
	        prop.Destroy();
	    }
	}	

	private void NotifyPropSpawned(string identifier)
	{
		Toast.Instance.Show( $"Prop {identifier} spawned ({OwnedProps.Count}/{PropLimit})" );
	}

	private void NotifyPropLimit()
	{
		Toast.Instance.Show( $"You have reached your prop limit ({PropLimit})" );
	}

	private void TriggerSpawnProtection()
	{
		// Reset the timer 
		_timeSinceLastSpawn = 0;

		// Log the protection trigger or notify the player
		Toast.Instance.Show( $"Spawn protection activated, please slow down !" );
	}

	public void UndoLastAction()
	{
		if ( History.Count > 0 )
		{
			History.Last().Undo();
			History.RemoveAt( History.Count - 1 );
		}
	}

	public int PropCount()
	{
		return OwnedProps.Count;
	}

	public void RemoveAllProps()
	{
		OwnedProps.ForEach( prop => prop.Destroy() );

		Toast.Instance.Show( "All props cleared" );
		OwnedProps.Clear();
	}

	public static string GetPropThumbnail( string propName )
	{
		var thumbnailPath = $"{propName.ToLower().Replace( ".vmdl", "" )}.vmdl_c.png";
		return thumbnailPath;
	}
	
	private Vector3 CalculateSpawnPosition(GameObject camera)
	{
		// Calculate spawn offset based on the player's camera position and orientation
		var spawnOffset = camera.Transform.World.Forward * -50f;

		Vector3? nullablePlayerPos = TraceUtils.ForwardLineTrace(Scene, camera.Transform, 100);
		var playerPos = nullablePlayerPos ?? Vector3.Zero;

		if (playerPos == Vector3.Zero)
		{
			playerPos = GameObject.Transform.World.Position + GameObject.Transform.Local.Forward * 150;
		}

		return playerPos + spawnOffset;
	}
}
