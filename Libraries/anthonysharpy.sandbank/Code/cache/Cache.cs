using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SandbankDatabase;

static internal class Cache
{
	/// <summary>
	/// Indicates that a full or partial write to disk is in progress.
	/// </summary>
	public static object WriteInProgressLock = new();

	/// <summary>
	/// All the stale documents.
	/// </summary>
	public static ConcurrentBag<Document> StaleDocuments = new();

	private static ConcurrentDictionary<string, Collection> _collections = new();
	private static float _timeSinceLastFullWrite = 0;
	private static object _timeSinceLastFullWriteLock = new();
	private static int _staleDocumentsFoundAfterLastFullWrite;
	private static int _staleDocumentsWrittenSinceLastFullWrite;
	private static float _partialWriteInterval = 1f / Config.PARTIAL_WRITES_PER_SECOND;
	private static TimeSince _timeSinceLastPartialWrite = 0;
	private static object _collectionCreationLock = new();
	private static bool _cacheWriteEnabled = true;

	public static int GetDocumentsAwaitingWriteCount()
	{
		return StaleDocuments.Count();
	}

	/// <summary>
	/// Used in the tests when we want to invalidate everything in the caches.
	/// 
	/// A bit crude and doesn't wipe everything.
	/// </summary>
	public static void WipeCaches()
	{
		if ( !TestHelpers.IsUnitTests )
			throw new Exception( "this can only be called during tests" );

		StaleDocuments = new();

		foreach ( var collection in _collections )
			collection.Value.CachedDocuments = new();
	}

	/// <summary>
	/// Used in the tests when we want to do writing to disk manually.
	/// </summary>
	public static void DisableCacheWriting()
	{
		if ( !TestHelpers.IsUnitTests )
			throw new Exception( "this can only be called during tests" );

		_cacheWriteEnabled = false;
	}

	public static void WipeStaticFields()
	{
		lock (WriteInProgressLock)
		{
			_collections.Clear();
			_timeSinceLastFullWrite = 0;
			_staleDocumentsFoundAfterLastFullWrite = 0;
			_staleDocumentsWrittenSinceLastFullWrite = 0;
			StaleDocuments.Clear();
			_partialWriteInterval = 1f / Config.PARTIAL_WRITES_PER_SECOND;
			_timeSinceLastPartialWrite = 0;
		}
	}

	public static Collection GetCollectionByName<T>( string name, bool createIfDoesntExist )
	{
		if ( !_collections.ContainsKey( name ) )
		{
			if ( createIfDoesntExist )
			{
				Logging.Log( $"creating new collection \"{name}\"" );
				CreateCollection( name, typeof( T ) );
			}
			else
			{
				return null;
			}
		}

		return _collections[name];
	}

	private static float GetTimeSinceLastFullWrite()
	{
		lock ( _timeSinceLastFullWriteLock )
		{
			return _timeSinceLastFullWrite;
		}
	}

	private static void ResetTimeSinceLastFullWrite()
	{
		lock ( _timeSinceLastFullWriteLock )
		{
			_timeSinceLastFullWrite = 0;
		}
	}

	public static void CreateCollection( string name, Type documentClassType )
	{
		// Only allow one thread to create a collection at once or this will
		// be madness.
		lock ( _collectionCreationLock )
		{
			if ( _collections.ContainsKey( name ) )
				return;

			ObjectPool.TryRegisterType( documentClassType.FullName, documentClassType );

			Collection newCollection = new()
			{
				CollectionName = name,
				DocumentClassType = documentClassType,
				DocumentClassTypeSerialized = documentClassType.FullName
			};

			FileController.CreateCollectionLock( name );
			_collections[name] = newCollection;

			int attempt = 0;
			string error = "";

			while ( true )
			{
				if ( attempt++ >= 10 )
					throw new SandbankException( $"failed to save \"{name}\" collection definition after 10 tries - is the file in use by something else?: {error}" );

				error = FileController.SaveCollectionDefinition( newCollection );

				if ( error == null )
					break;
			}
		}
	}

	public static void InsertDocumentsIntoCollection( string collection, List<Document> documents )
	{
		foreach ( var document in documents )
			_collections[collection].CachedDocuments[document.UID] = document;
	}

	public static void Tick()
	{
		if ( Initialisation.CurrentDatabaseState != DatabaseState.Initialised || !_cacheWriteEnabled)
			return;

		GameTask.RunInThreadAsync( () => 
		{
			lock ( _timeSinceLastFullWriteLock )
			{
				_timeSinceLastFullWrite += Config.TICK_DELTA;
			}

			if ( GetTimeSinceLastFullWrite() >= Config.PERSIST_EVERY_N_SECONDS )
			{
				// Do this immediately otherwise when the server is stuttering it can spam
				// full writes.
				ResetTimeSinceLastFullWrite();

				lock ( WriteInProgressLock )
				{
					FullWrite();
				}
			}
			else if ( _timeSinceLastPartialWrite > _partialWriteInterval )
			{
				PartialWrite();
				_timeSinceLastPartialWrite = 0;
			}
		} );
	}

	/// <summary>
	/// Force the cache to perform a full-write of all stale entries.
	/// </summary>
	public static void ForceFullWrite()
	{
		lock ( WriteInProgressLock )
		{
			Logging.Log( "beginning forced full-write..." );

			ReevaluateStaleDocuments();
			FullWrite();

			Logging.Log( "finished forced full-write..." );
		}
	}

	/// <summary>
	/// Figure out how many documents we should write for our next partial write.
	/// </summary>
	private static int GetNumberOfDocumentsToWrite()
	{
		float progressToNextWrite = GetTimeSinceLastFullWrite() / Config.PERSIST_EVERY_N_SECONDS;
		int documentsWeShouldHaveWrittenByNow = (int)(_staleDocumentsFoundAfterLastFullWrite * progressToNextWrite);
		int numberToWrite = documentsWeShouldHaveWrittenByNow - _staleDocumentsWrittenSinceLastFullWrite;

		if ( numberToWrite <= 0 )
			return 0;

		return numberToWrite;
	}

	/// <summary>
	/// Write some (but probably not all) of the stale documents to disk. The longer
	/// it's been since our last partial write, the more documents we will write.
	/// </summary>
	private static void PartialWrite()
	{
		try
		{
			lock ( WriteInProgressLock )
			{
				var numberOfDocumentsToWrite = GetNumberOfDocumentsToWrite();

				if ( numberOfDocumentsToWrite > 0 )
				{
					Logging.Log( "performing partial write..." );

					PersistStaleDocuments( numberOfDocumentsToWrite );
				}
			}
		}
		catch ( Exception e )
		{
			throw new SandbankException( "partial write failed: " + Logging.ExtractExceptionString( e ) );
		}
	}

	/// <summary>
	/// Perform a full-write to (maybe) guarantee we meet our write deadline target.
	/// Also, re-evaluate cache to determine what is now stale.
	/// </summary>
	private static void FullWrite()
	{
		try
		{
			Logging.Log( "performing full write..." );

			// Persist any remaining items first.
			PersistStaleDocuments();
			_staleDocumentsWrittenSinceLastFullWrite = 0;

			ReevaluateStaleDocuments();
		}
		catch ( Exception e )
		{
			throw new SandbankException( "full write failed: " + Logging.ExtractExceptionString( e ) );
		}
	}

	/// <summary>
	/// Persist some of the stale documents to disk. We generally don't want to persist
	/// them all at once, as this can cause lag spikes.
	/// </summary>
	private static void PersistStaleDocuments( int numberToWrite = int.MaxValue )
	{
		int remainingDocumentCount = _staleDocumentsFoundAfterLastFullWrite - _staleDocumentsWrittenSinceLastFullWrite;

		Logging.Log( $"remaining documents left to write: {remainingDocumentCount}" );

		if ( numberToWrite > remainingDocumentCount )
			numberToWrite = remainingDocumentCount;

		int realCount = StaleDocuments.Count();

		if ( numberToWrite > realCount )
			numberToWrite = realCount;

		Logging.Log( $"we are persisting {numberToWrite} documents to disk now" );

		_staleDocumentsWrittenSinceLastFullWrite += numberToWrite;

		int misses = 0;
		int failures = 0;

		for ( int i = 0; i < numberToWrite; i++ )
		{
			if ( !StaleDocuments.TryTake( out Document document ) )
			{
				misses++;
				continue;
			}

			if ( !PersistDocumentToDisk( document ) )
				failures++;
		}

		if (misses > 0)
			Logging.Log( $"missed {misses} times when persisting stale documents..." );

		_staleDocumentsWrittenSinceLastFullWrite -= (misses + failures);
	}

	/// <summary>
	/// Returns true on success, false otherwise.
	/// </summary>
	private static bool PersistDocumentToDisk( Document document  )
	{
		int attempt = 0;
		string error = "";

		while ( true )
		{
			if ( attempt++ >= 3 )
			{
				Logging.Error( $"failed to persist document \"{document.UID}\" from collection \"{document.CollectionName}\" to disk after 3 tries: " + error );
				return false;
			}

			error = FileController.SaveDocument( document );

			if ( error == null )
				return true;
		}
	}

	/// <summary>
	/// Re-examine the cache and figure out what's stale and so what needs writing to
	/// disk.
	/// </summary>
	private static void ReevaluateStaleDocuments()
	{
		Logging.Log( "re-evaluating stale documents..." );

		_staleDocumentsFoundAfterLastFullWrite = StaleDocuments.Count();

		Logging.Log( $"found {_staleDocumentsFoundAfterLastFullWrite} stale documents" );
	}
}
