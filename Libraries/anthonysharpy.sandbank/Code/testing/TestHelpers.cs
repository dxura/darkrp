using System.Runtime.CompilerServices;
using Sandbox;

[assembly: InternalsVisibleTo( "sandbank.unittest" )]

namespace SandbankDatabase;

internal static class TestHelpers
{
	public static bool IsUnitTests => FileSystem.Data == null;
}
