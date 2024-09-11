## When I first use the database, the game freezes temporarily

This is because the database initialises itself (loads all the data into memory etc) when you make your first request. Usually this is fine since most people's databases are very small.

However, if this is a problem for you, then you can call `Sandbank.InitialiseAsync()` when the game starts to pre-load the data.