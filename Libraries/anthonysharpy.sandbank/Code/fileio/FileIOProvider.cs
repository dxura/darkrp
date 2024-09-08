using Sandbox;
using System.Collections.Generic;

namespace SandbankDatabase;

internal sealed class FileIOProvider : IFileIOProvider
{
	public string ReadAllText( string file )
	{
		return FileSystem.Data.ReadAllText( file );
	}

	public void WriteAllText( string file, string text )
	{
		FileSystem.Data.WriteAllText( file, text );
	}

	public void CreateDirectory( string directory )
	{
		FileSystem.Data.CreateDirectory( directory );
	}

	public void DeleteDirectory( string directory, bool recursive = false )
	{
		FileSystem.Data.DeleteDirectory( directory, recursive );
	}

	public bool DirectoryExists( string directory )
	{
		return FileSystem.Data.DirectoryExists( directory );
	}

	public IEnumerable<string> FindFile( string folder, string pattern = "*", bool recursive = false )
	{
		return FileSystem.Data.FindFile( folder, pattern, recursive );
	}

	public IEnumerable<string> FindDirectory( string folder, string pattern = "*", bool recursive = false )
	{
		return FileSystem.Data.FindDirectory( folder, pattern, recursive );
	}

	public void DeleteFile( string file )
	{
		FileSystem.Data.DeleteFile( file );
	}
}
