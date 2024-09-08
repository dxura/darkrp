using System;

namespace SandbankDatabase;

internal static class Shutdown
{
	/// <summary>
	/// S&amp;box doesn't automatically wipe static fields yet so we have to do this
	/// ourselves.
	/// </summary>
	public static void WipeStaticFields()
	{
		Cache.WipeStaticFields();
		ObjectPool.WipeStaticFields();
		PropertyDescriptionsCache.WipeStaticFields();
	}

	public static void ShutdownDatabase()
	{
		// Theoretical possibility that the database could be shut down during initialistion,
		// if closed quickly enough.
		lock ( Initialisation.InitialisationLock )
		{
			if ( Config.STARTUP_SHUTDOWN_MESSAGES )
			{
				Log.Info( "==================================" );
				Log.Info( "Shutting down Sandbank..." );
			}

			try
			{
				if ( Initialisation.CurrentDatabaseState == DatabaseState.Initialised )
				{
					// Set this as a matter of priority since if this is wrong on the next start,
					// it won't be wiped automatically by s&box, and there is a risk the user could
					// do an operation on an uninitialised database.
					Initialisation.CurrentDatabaseState = DatabaseState.Uninitialised;

					Logging.Info( "shutting down database..." );

					Cache.ForceFullWrite();
					WipeStaticFields();
				}

				// Maybe it was in an irrecoverable error state. Let's just set it back.
				Initialisation.CurrentDatabaseState = DatabaseState.Uninitialised;
			}
			catch ( Exception e )
			{
				Initialisation.CurrentDatabaseState = DatabaseState.Uninitialised;
				Logging.Error( $"failed to shutdown database properly - some data may have been lost: {Logging.ExtractExceptionString( e )}" );
			}

			if ( Config.STARTUP_SHUTDOWN_MESSAGES )
			{
				Log.Info( "Shutdown completed" );
				Log.Info( "==================================" );
			}
		}
	}
}
