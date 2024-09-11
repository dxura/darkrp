using System;
using System.Collections.Generic;

namespace SandbankDatabase;

static class Initialisation
{
	public static DatabaseState CurrentDatabaseState;

	/// <summary>
	/// Only let one thread initialse the database at once.
	/// </summary>
	public static object InitialisationLock = new();

	public static void Initialise()
	{
		lock ( InitialisationLock )
		{
			if ( CurrentDatabaseState != DatabaseState.Uninitialised )
				return; // Probably another thread already did all this.

			if ( !Config.MERGE_JSON )
				Logging.ScaryWarn( "Config.MERGE_JSON is set to false - this will delete data if you rename or remove a data field" );

			if ( Config.STARTUP_SHUTDOWN_MESSAGES )
			{
				Log.Info( "==================================" );
				Log.Info( "Initialising Sandbank..." );
			}

			try
			{
				Shutdown.WipeStaticFields();
				FileController.Initialise();
				FileController.EnsureFileSystemSetup();
				LoadCollections();
				Ticker.Initialise();

				CurrentDatabaseState = DatabaseState.Initialised;

				if ( Config.STARTUP_SHUTDOWN_MESSAGES )
				{
					Log.Info( "Sandbank initialisation finished successfully" );
					Log.Info( "==================================" );
				}
			}
			catch ( Exception e )
			{
				Logging.Error( $"failed to initialise database: {Logging.ExtractExceptionString( e )}" );

				if ( Config.STARTUP_SHUTDOWN_MESSAGES )
				{
					Log.Info( "Sandbank initialisation finished unsuccessfully" );
					Log.Info( "==================================" );
				}
			}
		}
	}

	private static void LoadCollections()
	{
		int attempt = 0;
		string error = null;
		List<string> collectionNames;

		while ( true )
		{
			if ( attempt++ >= 10 )
				throw new SandbankException( $"failed to load collection list after 10 tries: {error}" );

			(collectionNames, error) = FileController.ListCollectionNames();

			if (error == null)
				break;
		}

		attempt = 0;

		foreach ( var collectionName in collectionNames )
		{
			Logging.Log( $"attempting to load collection \"{collectionName}\"" );

			while ( true )
			{
				if ( attempt++ >= 10 )
					throw new SandbankException( $"failed to load collection {collectionName} after 10 tries: {error}");

				error = LoadCollection( collectionName );

				if ( error == null )
					break;
			}
		}
	}

	/// <summary>
	/// Returns null on success or the error message on failure.
	/// </summary>
	private static string LoadCollection(string name)
	{
		var (definition, error) = FileController.LoadCollectionDefinition( name );

		if ( error != null )
			return $"failed loading collection definition for collection \"{name}\": {error}";

		if (definition == null)
			return $"found a folder for collection {name} but the definition.txt was missing in that folder or failed to load";

		(var documents, error) = FileController.LoadAllCollectionsDocuments( definition );

		if ( error != null )
			return $"failed loading documents for collection \"{name}\": {error}";

		Cache.CreateCollection( name, definition.DocumentClassType );
		Cache.InsertDocumentsIntoCollection( name, documents );

		Log.Info( $"Loaded collection {name} with {documents.Count} documents" );

		return null;
	}
}

internal enum DatabaseState
{
	Uninitialised,
	Initialised
}
