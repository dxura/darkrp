namespace Dxura.Darkrp;

public class ConfigManager
{
	/// <summary>
	/// Dictionary that contains the Configurations.
	/// </summary>
	private static Dictionary<string, object> _config = new ();

	/// <summary>
	/// Get the Configuration by Key.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public static object GetConfig( string key )
	{
		return _config.TryGetValue( key, out var value ) ? value : null;
	}

	/// <summary>
	/// Set's the Configuration by Key. 
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	private static void SetConfig( string key, object value )
	{
		_config[key] = value;
	}

	/// <summary>
	/// Add a new Configuration.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	private static void AddConfig( string key, object value )
	{
		SetConfig( key, value );
	}

	static ConfigManager()
	{
		//AddConfig( "DefaultName", "John Doe" );
	}
}
