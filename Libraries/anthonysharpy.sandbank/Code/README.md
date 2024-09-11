# Sandbank

Sandbank is the fast, easy-to-use no-SQL database for s&box.

Sandbank is a local data store and does not save anything in the cloud. However, if that's something you'd be interested in, message me on Discord (anthonysharpy) and I can probably put together a service for you that does this for low or no cost.

# Installation

### Via the package manager

In the editor, go to `View -> Library Manager` and find Sandbank Database. You might have to restart s&box.

### Directly from source

Alternatively you can get the latest version of the source code from https://github.com/anthonysharpy/sandbank. This can be put directly in your source code, or wherever else you want to put it.

# Usage

### Basic introduction

The database uses the document model. This means that data is saved as JSON files. It also means that there is no need to create SQL queries or do joins or anything like that.

A "document" is just a class containing some data. For example, data for a specific player.

Each document belongs to a "collection". Every collection contains many documents. Most databases will have multiple collections. For example, you might have a "players" collection for player data, and a "houses" collection for players' houses, etc.

Data files are saved in s&box's data folder. For example:

`C:\Program Files (x86)\Steam\steamapps\common\sbox\data\my_organisation\my_project\sandbank`.

The basics you need to know:
- To configure the database and change some performance-related settings, you can modify `Config.cs` (the default settings will work very well for 95% of people).

- The data you want to save must be in a _**class**_. Structs are not supported. Structs are supported when used inside a class, though.

- You can't use different class types with the same collection. It's one class per collection.

- Any data you want saved must be a public property with the `[Saved]` attribute. If you're putting it on something like a class or a List of classes, all public properties in those classes will get saved. If you don't want this, you can add the `[JsonIgnore]` attribute probably to the properties you don't want saved (I will probably add more control over this later).

- Every document _**must**_ have a _**public string property**_ called "UID" (unique ID) . This is the _**unique**_ primary ID of the document and is also used as the document's file name. You can set this to be whatever you want. For example, you might want it to be a player's Steam ID. Alternatively, you can leave it empty, and the database will automatically populate the ID for you as a random GUID.

- When your server is _**switched-off**_, you can easily edit your data just by editing the files.

An example:

### Specifying your data
```
// Note how this is also a component. This is the recommended way to do it.
// If you store your data in a component you can sync it over the network as
// well as save it.

class PlayerData : Component
{
	[Saved] public string UID { get; set; }
	[Saved, Sync] public float Health { get; set; }
	[Saved, Sync] public string Name { get; set; }
	[Saved, Sync] public int Level { get; set; }
	[Saved] public DateTime LastPlayTime { get; set; }
	[Saved, Sync] public List<string> Items { get; set; } = new();
}
```

### Querying
```
using SandbankDatabase;

private PlayerData _myPlayerData = new();

public void SaveData()
{
	Log.Info($"My ID is empty: {_myPlayerData.UID}");

	_myPlayerData.Health = 100;
	_myPlayerData.Name = "Bob";

	// Insert the player. Their ID is populated from within the function because the
	// class is passed by reference.
	Sandbank.Insert("players", _myPlayerData);

	Log.Info($"My ID is now populated: {_myPlayerData.UID}");

	var playerWith100Health = Sandbank.SelectOne<PlayerData>("players", x => x.Health == 100);

	Log.Info($"The player with 100 health is: {playerWith100Health.Name}"); // "Bob".

	Sandbank.DeleteWithID<PlayerData>("players", playerWith100Health.UID);
}
```

### Using the data

If you fetch data from the database and want to put it in a component in the scene or something like that, you can either copy each field yourself, or use the helper method `CopySavedData`, which will copy all `[Saved]` public properties:

```
var player = GetOurPlayer(); // Get the player in the scene.
var ourPlayerData = Sandbank.SelectOneWithID<PlayerData>("players", "123");
Sandbank.CopySavedData<PlayerData>(ourPlayerData, player.Data);
```

### Slow queries

A well-designed query should return instantly.

However, if you're doing something really hardcore, you should consider wrapping the call in its own thread:

```
GameTask.RunInThreadAsync( () => {
	var houses = Sandbank.Select<House>("houses", x => x.OwnerName == "Steve");

	// Do something.
});
```

If you find this doesn't suit your needs then please make an issue.

### Renaming properties

If you rename the properties in your data class, the data is not lost. For example, if you renamed a property called `Name` to `PlayerName`, the `Name` property will still be saved on file, and `PlayerName` will be created alongside it.

This is good because it means you can't accidentally delete all your data. It's also bad because it makes it harder to remove old data from your database.

In order to get around this, first start the database and migrate your data:

```
playerData.PlayerName = playerData.Name;
Sandbank.Insert<PlayerData>("players", playerData);
```

Next, check the changes were applied and _**make a backup of your data**_. Make sure you don't have any other unused fields as they will get deleted next.

Lastly, set `MERGE_JSON` in `Config.cs` to false. Then, start up the database again and wait a few seconds for the changes to take effect. The renamed data should be gone now.

You must then set `MERGE_JSON` back to true, or it will spam warnings at you.

# Performance

### CPU

Sandbank is designed to be thread-safe, letting you squeeze more out of it. 

Sandbank creates a copy of itself in program memory, so for most use-cases it is probably faster than a conventional database, unless you have hundreds of thousands of records, and you know how to index them efficiently.

Here are some benchmarks using the above PlayerData class on a Ryzen 5 5500 with 12 logical processors:

| Operation                                                                                  | Total Time    | Speed                             | Notes                  |
|--------------------------------------------------------------------------------------------|---------------|-----------------------------------|------------------------|
| 100,800 inserts (one thread) | 0.6117 seconds | 165,000 documents inserted/second | In reality this is probably faster than your disk could keep up with anyway. |
| 100,800 inserts (24 threads) | 0.1263 seconds | 798,000 documents inserted/second | |
| Search 100,800 documents [x => x.Health >= 90] (one thread) | 0.0377 seconds | 2,674,000 documents searched/second | ~10,080 records being returned here. |
| Search 2,419,200 documents [x => x.Health >= 90] (24 threads) | 0.1910 seconds | 12,666,000 documents searched/second | ~10,080 records being returned here per thread. |
| Search 2,419,200 documents [x => x.Health == 100] (24 threads) | 0.1097 seconds | 22,053,000 documents searched/second | ~1,008 records being returned here per thread, hence much faster due to less memory copying. This is probably the more realistic scenario. |
| Search 100,800 documents [x => x.Health >= 90] (one thread, unsafe references) | 0.0273 seconds | 3,692,000 documents searched/second |  ~10,080 records being returned here. |
| Search 2,419,200 documents [x => x.Health >= 90] (24 threads, unsafe references) | 0.0990 seconds | 24,436,000 documents searched/second |  ~10,080 records being returned here per thread. |
| Search 100,800 documents by ID 100,000 times (one thread) | 0.1170 seconds | 855,000 lookups/second | 1 document returned. |
| Search 100,800 documents by ID 2,400,000 times (24 threads) | 0.5930 seconds | 4,047,000 lookups/second | 1 document returned. |

The above figures represent the time it took to write/read the data to/from the cache only (not to disk). As you can see, searching by ID is basically instant, inserts are very quick, and regular searches are relatively quick. These benchmarks used an optimal pool size of around 200,000 (about 240mb worth of extra memory).

The speed of searching the database will depend heavily on:
- The size of your collection and documents
- The complexity of your query
- The number of documents returned

### Memory

The database stores all data in memory in a cache. 10,000 of the above PlayerData classes only take up around 12mb memory. Unless you're handling millions of documents, or your documents are very big, you don't really need to worry about memory.

### Disk

The disk space used is less than the amount of memory used. Changes to the cache are written slowly to the disk over time in a background thread. Under extreme loads (thousands of documents being inserted per second) this may throttle your hard-drive a little, but it shouldn't impact performance too much.

# Data Consistency

Data is written to disk slowly over time. The frequency at which this is done, as well as a number of other things, is configurable in `Config.cs`. By default, the database aims to write any change to disk in under 10 seconds.

Sandbank attempts to shut itself down gracefully in the background when the server stops. However, it is still recommended to call `Shutdown()` before an anticipated server shutdown to ensure that the database is terminated properly. If the server crashes or if the server process is suddenly terminated, any data that is not written to disk by that point is lost.

# Features at a glance

- Ability to store data on the client and/or server.
- Optional file obfuscation to prevent players tampering with locally-stored files.

# Contributions

Contributions are more than welcome. Also, feel free to ask questions or raise issues on the GitHub page: https://github.com/anthonysharpy/sandbank. If you do want to contribute something, it's probably a good idea to raise an issue first.

Please note that the project is not entirely open-source and there are some very minor restrictions around what you can do (such as creating other versions of the software). Please read the licence if you are unsure.

# Learn More

- [Database repair guide](RepairGuide.md)
- [Advanced optimisation guide](OptimisationGuide.md)
