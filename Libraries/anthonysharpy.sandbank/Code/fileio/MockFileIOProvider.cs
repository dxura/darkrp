using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SandbankDatabase;

internal sealed class MockFileIOProvider : IFileIOProvider
{
	private MockDirectory _fileSystem = new();

	public string ReadAllText( string file )
	{
		var data = _fileSystem.GetFile( file )?.Contents;

		Logging.Info( $"reading text from file {file} (read {data?.Length ?? 0} characters)" );

		return data;
	}

	public void WriteAllText( string file, string text )
	{
		Logging.Info( $"writing to file {file}" );

		_fileSystem.CreateFileAt( file, text );
	}

	public void CreateDirectory( string directory )
	{
		Logging.Info( $"creating directory {directory}" );

		_fileSystem.CreateDirectoryAt( directory );
	}

	/// <summary>
	/// Don't know how a directory deletion could ever not be recursive but that's
	/// how s&amp;box has it.
	/// </summary>
	public void DeleteDirectory( string directory, bool recursive = false )
	{
		Logging.Info( $"deleting directory {directory}" );

		_fileSystem.DeleteDirectoryAt( directory );
	}

	public bool DirectoryExists( string directory )
	{
		var result = _fileSystem.GetDirectory( directory ) != null;

		Logging.Info( $"checking if directory {directory} exists ({ (result ? "it does" : "it doesn't" ) })" );

		return result;
	}

	public IEnumerable<string> FindFile( string folder, string pattern = "*", bool recursive = false )
	{
		if ( recursive )
			throw new SandbankException( "not supported" );
		if ( pattern != "*" )
			throw new SandbankException( "not supported" );

		var result = _fileSystem.GetFilesInDirectory( folder )
			.Where( x => x.FileType == MockFileType.File );

		Logging.Info( $"finding files in directory {folder} (found {result.Count()})" );

		return result.Select( x => x.Name );
	}

	public IEnumerable<string> FindDirectory( string folder, string pattern = "*", bool recursive = false )
	{
		if ( recursive )
			throw new SandbankException( "not supported" );
		if ( pattern != "*" )
			throw new SandbankException( "not supported" );

		var result = _fileSystem.GetFilesInDirectory( folder )
			.Where( x => x.FileType == MockFileType.Directory );

		Logging.Info( $"finding directories in directory {folder} (found {result.Count()})" );

		return result.Select( x => x.Name );
	}

	public void DeleteFile( string file )
	{
		Logging.Info( $"deleting file {file}" );

		_fileSystem.DeleteFileAt( file );
	}

	class MockFileBase
	{
		public string Name;
		public MockFileType FileType;
	}

	class MockFile : MockFileBase
	{
		public string Contents;
	}

	class MockDirectory : MockFileBase
	{
		ConcurrentDictionary<string, MockFileBase> Files = new();

		public void DeleteFileAt( string path )
		{
			var directoryPath = path.Split( '/' ).Where( x => x.Count() > 0 ).ToList();

			var fileName = directoryPath[directoryPath.Count - 1];
			directoryPath.RemoveAt( directoryPath.Count - 1 );

			var directory = GetDirectory( string.Join('/', directoryPath ) );

			if ( directory == null )
				return;

			if ( !directory.Files.ContainsKey( fileName ) )
				return;

			var file = directory.Files[fileName];

			if ( file.FileType != MockFileType.File )
				return;

			directory.Files.Remove( fileName, out _ );
		}

		public void DeleteDirectoryAt( string path )
		{
			var directoryPath = path.Split( '/' ).Where( x => x.Count() > 0 ).ToList();

			var directoryName = directoryPath[directoryPath.Count - 1];
			directoryPath.RemoveAt( directoryPath.Count - 1 );

			var directory = GetDirectory( string.Join( '/', directoryPath ) );

			if ( directory == null )
				return;

			if ( !directory.Files.ContainsKey( directoryName ) )
				return;

			var directoryToDelete = directory.Files[directoryName];

			if ( directoryToDelete.FileType != MockFileType.Directory )
				return;

			directory.Files.Remove( directoryName, out _ );
		}

		public void CreateDirectoryAt( string path )
		{
			if ( GetDirectory( path ) != null )
				return;

			var parts = path.Split( '/' ).Where( x => x.Count() > 0 ).ToList();
			string parentPath = "";

			// If there is only one part then we just create the directory on ourselves.
			if ( parts.Count == 1 )
			{
				Files[parts[0]] = new MockDirectory()
				{
					FileType = MockFileType.Directory,
					Name = parts[0],
				};

				return;
			}

			var folderName = parts[parts.Count - 1];

			// parts now only contains the parent directories.
			parts.RemoveAt( parts.Count - 1 );

			// Make sure parent paths exist.
			// Don't do the last path because that isn't a parent path.
			foreach ( var part in parts )
			{
				parentPath += part + "/";

				if ( GetDirectory( parentPath ) == null )
					CreateDirectoryAt( parentPath );
			}

			var directory = GetDirectory( parentPath );

			directory.Files[folderName] = new MockDirectory()
			{
				FileType = MockFileType.Directory,
				Name = folderName,
			};
		}

		public void CreateFileAt( string path, string contents )
		{
			DeleteFileAt( path );

			var parts = path.Split( '/' ).Where( x => x.Count() > 0 ).ToList();

			var fileName = parts[parts.Count - 1];
			parts.RemoveAt( parts.Count - 1 );

			var containingFolder = string.Join( '/', parts );

			var directory = GetDirectory( containingFolder );

			if (directory == null)
			{
				CreateDirectoryAt( containingFolder );
				directory = GetDirectory( containingFolder );
			}

			directory.Files[fileName] = new MockFile()
			{
				FileType = MockFileType.File,
				Name = fileName,
				Contents = contents
			};
		}

		public MockDirectory GetDirectory( string directory )
		{
			if ( directory.Count() == 0 || directory == "/" )
				return this;

			var parts = directory.Split( '/' ).Where(x => x.Count() > 0);
			var current = this;

			foreach ( var part in parts )
			{
				if ( !current.Files.ContainsKey( part ) )
					return null;

				var next = current.Files[part];

				if ( next.FileType != MockFileType.Directory )
					return null;

				current = (MockDirectory)next;
			}

			return current;
		}

		public MockFile GetFile( string path )
		{
			var parts = path.Split( '/' ).Where( x => x.Count() > 0 ).ToList();
			var fileName = parts.Last();

			parts.RemoveAt( parts.Count - 1 );
			
			var directory = GetDirectory( string.Join( "/", parts ) );

			if ( directory == null )
				return null;

			if ( !directory.Files.ContainsKey( fileName ) )
				return null;

			var file = directory.Files[fileName];

			if ( file.FileType != MockFileType.File )
				return null;

			return (MockFile)file;
		}

		public List<MockFileBase> GetFilesInDirectory( string directory )
		{
			var dir = GetDirectory( directory );

			return dir != null ? 
				dir.Files.Select( x => x.Value).ToList() 
				: new List<MockFileBase>();
		}
	}

	private enum MockFileType
	{
		File,
		Directory
	}
}
