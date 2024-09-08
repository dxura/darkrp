using System;

namespace SandbankDatabase;

public sealed class SandbankException : Exception
{
	public SandbankException( string message ) : base( message )
	{
	}
}
