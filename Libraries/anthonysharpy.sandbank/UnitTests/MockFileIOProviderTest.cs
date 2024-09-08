using System.Linq;

namespace SandbankDatabase;

[TestClass]
public partial class MockFileIOProviderTest
{
	[TestMethod]
	public void CreateDirectory()
	{
		var provider = new MockFileIOProvider();

		provider.CreateDirectory( "test/test2" );

		Assert.IsTrue( provider.DirectoryExists( "test/test2" ) );
	}

	[TestMethod]
	public void DeleteDirectory()
	{
		var provider = new MockFileIOProvider();

		Assert.IsTrue( provider.DirectoryExists( "" ) );
		Assert.IsTrue( provider.DirectoryExists( "/" ) );

		Assert.IsFalse( provider.DirectoryExists( "test/test2" ) );

		provider.CreateDirectory( "test/test2" );
		Assert.IsTrue( provider.DirectoryExists( "test/test2" ) );

		provider.DeleteDirectory( "test/test2" );
		Assert.IsFalse( provider.DirectoryExists( "test/test2" ) );

		provider.CreateDirectory( "test/test2" );
		Assert.IsTrue( provider.DirectoryExists( "test/test2" ) );

		provider.DeleteDirectory( "test" );
		Assert.IsFalse( provider.DirectoryExists( "test/test2" ) );
		Assert.IsFalse( provider.DirectoryExists( "test" ) );
	}

	[TestMethod]
	public void WriteAllText()
	{
		var provider = new MockFileIOProvider();

		provider.WriteAllText( "hello/there/test.txt", "This is a test file" );

		Assert.AreEqual( "This is a test file", provider.ReadAllText( "hello/there/test.txt" ) );
	}

	[TestMethod]
	public void ReadAllText()
	{
		var provider = new MockFileIOProvider();

		Assert.AreEqual( null, provider.ReadAllText( "hello/testing/test.txt" ) );
	}

	[TestMethod]
	public void FindFile()
	{
		var provider = new MockFileIOProvider();

		provider.WriteAllText( "/folder/testing/a.txt", "A" );
		provider.WriteAllText( "/folder/testing/b.txt", "B" );
		provider.WriteAllText( "/folder/testing/c.txt", "C" );

		var files = provider.FindFile( "folder/testing" ).ToList();

		Assert.IsTrue( files.Count == 3 );
		Assert.IsTrue( files.Contains( "a.txt" ) );
		Assert.IsTrue( files.Contains( "b.txt" ) );
		Assert.IsTrue( files.Contains( "c.txt" ) );
	}

	[TestMethod]
	public void DeleteFile()
	{
		var provider = new MockFileIOProvider();

		provider.WriteAllText( "/folder/testing/a.txt", "A" );
		provider.WriteAllText( "/folder/testing/b.txt", "B" );
		provider.WriteAllText( "/folder/testing/c.txt", "C" );

		var files = provider.FindFile( "folder/testing" ).ToList();
		Assert.IsTrue( files.Count == 3 );

		provider.DeleteFile( "folder/testing/b" );

		files = provider.FindFile( "folder/testing" ).ToList();
		Assert.IsTrue( files.Count == 3 );

		provider.DeleteFile( "folder/testing/b.txt" );

		files = provider.FindFile( "folder/testing" ).ToList();

		Assert.IsTrue( files.Count == 2 );
		Assert.IsTrue( files.Contains( "a.txt" ) );
		Assert.IsFalse( files.Contains( "b.txt" ) );
		Assert.IsTrue( files.Contains( "c.txt" ) );
	}

	[TestMethod]
	public void FindDirectory()
	{
		var provider = new MockFileIOProvider();

		provider.CreateDirectory( "test/testdira" );
		provider.CreateDirectory( "test/testdirb" );
		provider.CreateDirectory( "test/testdirc" );

		var directories = provider.FindDirectory( "/test/" ).ToList();

		Assert.IsTrue( directories.Count == 3 );
		Assert.IsTrue( directories.Contains( "testdira" ) );
		Assert.IsTrue( directories.Contains( "testdirb" ) );
		Assert.IsTrue( directories.Contains( "testdirc" ) );

		directories = provider.FindDirectory( "" ).ToList();

		Assert.IsTrue( directories.Count == 1 );
		Assert.IsTrue( directories.Contains( "test" ) );
	}
}
