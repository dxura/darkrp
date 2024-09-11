using System.Collections.Generic;

namespace SandbankDatabase;

/// <summary>
/// Defines an implementation of a class that provides file access.
/// </summary>
internal interface IFileIOProvider
{
	public bool DirectoryExists( string directory );
	public void CreateDirectory( string directory );
	public void DeleteDirectory( string directory, bool recursive = false );
	public void WriteAllText( string file, string text );
	public string ReadAllText( string file );
	public void DeleteFile( string file );
	public IEnumerable<string> FindFile( string folder, string pattern = "*", bool recursive = false );
	public IEnumerable<string> FindDirectory( string folder, string pattern = "*", bool recursive = false );
}
