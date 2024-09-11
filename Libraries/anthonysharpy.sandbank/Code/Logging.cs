using System;

namespace SandbankDatabase;

static class Logging
{
	public static void Log(string message)
	{
		if ( !Config.ENABLE_LOGGING )
			return;

		Sandbox.Internal.GlobalGameNamespace.Log.Info( $"Sandbank: {message}" );
	}
	public static void Info( string message )
	{
		if ( !Config.ENABLE_LOGGING )
			return;

		Sandbox.Internal.GlobalGameNamespace.Log.Info( $"Sandbank: {message}" );
	}

	public static void ScaryWarn(string message)
	{
		Sandbox.Internal.GlobalGameNamespace.Log.Warning( $"Sandbank: ============= WARNING =============" );
		Sandbox.Internal.GlobalGameNamespace.Log.Warning( $"Sandbank: {message}" );
		Sandbox.Internal.GlobalGameNamespace.Log.Warning( $"Sandbank: ===================================" );
	}

	public static void Warn( string message )
	{
		if ( Config.WARNINGS_AS_EXCEPTIONS )
			throw new SandbankException( $"Sandbank: {message}" );

		Sandbox.Internal.GlobalGameNamespace.Log.Warning( $"Sandbank: {message}" );
	}

	public static void Error( string message )
	{
		if (Config.WARNINGS_AS_EXCEPTIONS )
			throw new SandbankException( $"Sandbank: {message}" );

		Sandbox.Internal.GlobalGameNamespace.Log.Error( $"Sandbank: {message}" );
	}

	public static string ExtractExceptionString(Exception e)
	{
		return $"{e.Message}\n\n{e.StackTrace}\n{e.InnerException}";
	}
}
