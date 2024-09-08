namespace SandbankDatabase; 

static class Config
{
	/// <summary>
	/// Whether to show the startup and shutdown messages in the console when the database
	/// stops and starts.
	/// </summary>
	public static bool STARTUP_SHUTDOWN_MESSAGES = true;
	/// <summary>
	/// If this is true then all warnings are thrown as exceptions. I probably wouldn't
	/// recommend this but you can enable it if you want. This is used in the unit tests
	/// to make life easier.
	/// </summary>
	public static bool WARNINGS_AS_EXCEPTIONS = false;
	/// <summary>
	/// Set this to true if you want clients to be able to use the database too. You
	/// probably don't want this - none of the data will get synced between host and clients
	/// (that's not what it's designed to do). But there might be some situations where you
	/// want to store data on the client for some reason.
	/// </summary>
	public static bool CLIENTS_CAN_USE = false;
	/// <summary>
	/// This controls whether the written JSON files are indented or not.
	/// Indentation makes them more human-readable, but probably makes saving
	/// to disk a little bit slower.
	/// </summary>
	public static bool INDENT_JSON = true;
	/// <summary>
	/// The database will try to make sure that all stale data is written to disk
	/// at most every this many seconds. In the event of a crash, all stale data
	/// is lost, so lower numbers are "safer". But lower numbers can lead to decreased
	/// performance under heavy loads due to increased disk writing.
	/// </summary>
	public static float PERSIST_EVERY_N_SECONDS = 10f;
	/// <summary>
	/// We will only try to perform a partial write this many times per second. A partial
	/// write doesn't write everything, so changing this will not really change
	/// your performance. But it does increase performance by ensuring that are we not 
	/// spamming writes every tick. You probably don't want to change this.
	/// </summary>
	public static int PARTIAL_WRITES_PER_SECOND = 1;
	/// <summary>
	/// Enables logging for helping to diagnose issues. This is mostly for development
	/// purposes.
	/// </summary>
	public static bool ENABLE_LOGGING = false;
	/// <summary>
	/// This is the name of the folder where your files are kept (e.g. "sandbank/my_collection").
	/// There's no reason to change it, but you're more than welcome to. If you're
	/// renaming an existing database, make sure to copy your files across to the new
	/// folder.
	/// </summary>
	public static string DATABASE_NAME = "sandbank";
	/// <summary>
	/// How often the database ticks in milliseconds. I don't recommend changing this as
	/// you are not necessarily making things any faster.
	/// </summary>
	public static int TICK_DELTA = 100;
	/// <summary>
	/// The number of instances of each class used by your database that will be cached in RAM for
	/// faster fetching. Increasing this will improve performance if you are selecting
	/// lots of records at once. For example, if you were fetching 1,000 records per second, 
	/// for optimal performance, the recommended value for this would be 2,000 (about twice as much).
	/// You will probably see little-to-no performance gain by increasing this further.
	/// <br/><br/>
	/// Increasing this will increase memory usage. The memory increase is not affected by
	/// the size of a collection, but is is affected by the size of your data class in memory.
	/// As a very rough rule, a 100,000 pool size takes up around 200mb RAM for each collection.
	/// A very rough formula for estimating the total memory usage is:
	/// <br/><br/>
	/// <strong>200mb * number of collections * CLASS_INSTANCE_POOL_SIZE / 100,000</strong>
	/// </summary>
	public static int CLASS_INSTANCE_POOL_SIZE = 2000;
	/// <summary>
	/// This should always true unless you know what you are doing.
	/// <br/><br/>
	/// If this is true and you rename a field in your data class, the renamed data will remain in
	/// the file. For example, if you renamed "Name" to "PlayerName", both properties would still be there.
	/// This is because any existing JSON is "merged" with any updates.
	/// <br/><br/>
	/// If this is false and you rename a field, the renamed data is destroyed. The new document simply
	/// overwrites the file and no merge is done. So if you renamed "Name" to "PlayerName", "Name" is destroyed.
	/// <br/><br/>
	/// This is here to protect you, so the only time this should be set to false is if you're ready to remove
	/// the renamed data.
	/// </summary>
	public static bool MERGE_JSON = true;
	/// <summary>
	/// Enabling this option will obfuscate files stored on the local filesystem, making them (almost) impossible
	/// to edit. This is useful if you want to store data on the client that you don't want them to be able to
	/// change easily.
	/// <br/><br/>
	/// Note that this is not secure. If someone really wanted to, they could reverse-engineer the data and change
	/// it to whatever they want. However most people will not have the skills or inclination to do this.
	/// <br/><br/>
	/// Note that this will cause saving and loading files to become a bit slower and more CPU intensive.
	/// <br/><br/>
	/// The database will work whether this is enabled or not, regardless of whether some or all of the files are
	/// obfuscated. Files are only obfuscated/unobfuscated when they are saved, so changing this will have no impact
	/// on files until those files are re-saved.
	/// </summary>
	public static bool OBFUSCATE_FILES = false;
}
