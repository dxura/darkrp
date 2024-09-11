using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static TestClasses;

namespace SandbankDatabase;

[TestClass]
public partial class SandbankBenchmarksTest
{
	[TestCleanup]
	public void Cleanup()
	{
		if ( Sandbank.IsInitialised )
		{
			Sandbank.DeleteAllData();
			Sandbank.Shutdown();
		}
	}

	[TestInitialize]
	public void Initialise()
	{
		Sandbank.InitialiseAsync().GetAwaiter().GetResult();
		Sandbank.DeleteAllData();
	}

	[TestMethod]
	public void BenchmarkObfuscation()
	{
		var text = Obfuscation.ObfuscateFileText( "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Garry Newman HATES bacon.... LOL!" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！" +
			 "Wow! I love bacon! 豚肉が美味しい！"
		);

		for (int i = 0; i < 10000; i++ )
		{
			text = Obfuscation.ObfuscateFileText( text );
			text = Obfuscation.UnobfuscateFileText( text );
		}
	}

	[TestMethod]
	public void BenchmarkInsertTest()
	{
		Sandbank.Insert( "players", new ReadmeExample() ); // Register the class type with the pool.
		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.

		int documents = 100800;

		ReadmeExample[] testData = new ReadmeExample[documents];

		for ( int i = 0; i < documents; i++ )
		{
			testData[i] = new ReadmeExample()
			{
				Health = 100,
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			};
		}

		var watch = new Stopwatch();
		watch.Start();

		for ( int i = 0; i < documents; i++ )
		{
			Sandbank.Insert( "players", testData[i] );
		}

		watch.Stop();

		Console.WriteLine( $"Benchmark: Insert() - {documents} documents inserted in {watch.Elapsed.TotalSeconds} seconds" );
	}

	[TestMethod]
	public void BenchmarkLong()
	{
		Sandbank.Insert( "players", new ReadmeExample() ); // Register the class type with the pool.
		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.

		// Benchmark for at least 30 seconds.

		const int duration = 30;
		const int documentsPerSecond = 20000;
		const int totalDocuments = duration * documentsPerSecond;

		ReadmeExample[] testData = new ReadmeExample[totalDocuments];

		for ( int i = 0; i < totalDocuments; i++ )
		{
			testData[i] = new ReadmeExample()
			{
				Health = Game.Random.Int(1, 100),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			};
		}

		int currentDocument = 0;

		var watch = new Stopwatch();
		watch.Start();

		for (int i = 0; i < duration; i++ )
		{
			for ( int d = 0; d < documentsPerSecond; d++, currentDocument++ )
			{
				Sandbank.Insert( "players", testData[currentDocument] );
			}

			Task.Delay( 500 ).GetAwaiter().GetResult();

			var data = Sandbank.Select<ReadmeExample>( "players", x => x.Health > 50 );

			Task.Delay( 500 ).GetAwaiter().GetResult();
		}

		watch.Stop();

		Console.WriteLine( $"Benchmark: Long() - completed in {watch.Elapsed.TotalSeconds} seconds" );
	}

	[TestMethod]
	public void BenchmarkInsertThreaded()
	{
		Sandbank.Insert( "players", new ReadmeExample() ); // Register the class type with the pool.
		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.

		int documents = 100800;
		int threads = 24;
		int documentsPerThread = documents / threads;

		List<ReadmeExample> testData = new();

		for ( int i = 0; i < documentsPerThread; i++ )
		{
			testData.Add( new ReadmeExample()
			{
				Health = 100,
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		List<Task> tasks = new();

		var watch = new Stopwatch();
		watch.Start();

		for ( int t = 0; t < threads; t++ )
		{
			tasks.Add( Task.Run( () =>
			{
				for ( int i = 0; i < documentsPerThread; i++ )
				{
					Sandbank.Insert<ReadmeExample>( "players", testData[i] );
				}
			} ) );
		}

		Task.WhenAll( tasks ).GetAwaiter().GetResult();

		watch.Stop();

		Console.WriteLine( $"Benchmark: [multi-threaded] Insert() - {documents} documents inserted in {watch.Elapsed.TotalSeconds} seconds" );
	}

	[TestMethod]
	public void BenchmarkSelect()
	{
		int collectionSize = 100800;

		for ( int i = 0; i < collectionSize; i++ )
		{
			Sandbank.Insert( "players", new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.

		var watch = new Stopwatch();
		watch.Start();

		Sandbank.Select<ReadmeExample>( "players", x => x.Health >= 90 );

		watch.Stop();

		Console.WriteLine( $"Benchmark: Select() - {collectionSize} documents searched in {watch.Elapsed.TotalSeconds} seconds" );
	}

	[TestMethod]
	public void BenchmarkSelectThreaded()
	{
		int collectionSize = 100800;
		int threads = 24;

		for ( int i = 0; i < collectionSize; i++ )
		{
			Sandbank.Insert<ReadmeExample>( "players", new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.
		List<Task> tasks = new();

		var watch = new Stopwatch();
		watch.Start();

		for ( int t = 0; t < threads; t++ )
		{
			tasks.Add( Task.Run( () =>
			{
				Sandbank.Select<ReadmeExample>( "players", x => x.Health >= 90 );
			} ) );
		}

		Task.WhenAll( tasks ).GetAwaiter().GetResult();

		watch.Stop();

		Console.WriteLine( $"Benchmark: [multi-threaded] Select() - {collectionSize} documents searched {threads} times in {watch.Elapsed.TotalSeconds} seconds (~10,080 records returned)" );
	}

	// Same as BenchmarkSelectThreaded except with fewer returned records, making
	// it a bit more realistic.
	[TestMethod]
	public void BenchmarkSelectThreadedFewerRecords()
	{
		int collectionSize = 100800;
		int threads = 24;

		for ( int i = 0; i < collectionSize; i++ )
		{
			Sandbank.Insert<ReadmeExample>( "players", new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.
		List<Task> tasks = new();

		var watch = new Stopwatch();
		watch.Start();

		for ( int t = 0; t < threads; t++ )
		{
			tasks.Add( Task.Run( () =>
			{
				Sandbank.Select<ReadmeExample>( "players", x => x.Health == 100 );
			} ) );
		}

		Task.WhenAll( tasks ).GetAwaiter().GetResult();

		watch.Stop();

		Console.WriteLine( $"Benchmark: [multi-threaded] Select() - {collectionSize} documents searched {threads} times in {watch.Elapsed.TotalSeconds} seconds (~1,080 records returned)" );
	}

	[TestMethod]
	public void BenchmarkSelectUnsafeReferences()
	{
		int collectionSize = 100800;

		for ( int i = 0; i < collectionSize; i++ )
		{
			Sandbank.Insert( "players", new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.

		var watch = new Stopwatch();
		watch.Start();

		Sandbank.SelectUnsafeReferences<ReadmeExample>( "players", x => x.Health >= 90 );

		watch.Stop();

		Console.WriteLine( $"Benchmark: SelectUnsafeReferences() - {collectionSize} documents searched in {watch.Elapsed.TotalSeconds} seconds (~10,080 records returned)" );
	}

	[TestMethod]
	public void BenchmarkSelectUnsafeReferencesThreaded()
	{
		int collectionSize = 100800;
		int threads = 24;

		for ( int i = 0; i < collectionSize; i++ )
		{
			Sandbank.Insert<ReadmeExample>( "players", new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.
		List<Task> tasks = new();

		var watch = new Stopwatch();
		watch.Start();

		for ( int t = 0; t < threads; t++ )
		{
			tasks.Add( Task.Run( () =>
			{
				Sandbank.SelectUnsafeReferences<ReadmeExample>( "players", x => x.Health >= 90 );
			} ) );
		}

		Task.WhenAll( tasks ).GetAwaiter().GetResult();

		watch.Stop();

		Console.WriteLine( $"Benchmark: [multi-threaded] SelectUnsafeReferences() - {collectionSize} documents searched {threads} times in {watch.Elapsed.TotalSeconds} seconds (~10,080 records returned)" );
	}

	[TestMethod]
	public void BenchmarkSelectUnsafeReferencesThreaded_MoreIntense()
	{
		int collectionSize = 1008000;
		int threads = 24;

		for ( int i = 0; i < collectionSize; i++ )
		{
			Sandbank.Insert<ReadmeExample>( "players", new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.
		List<Task> tasks = new();

		var watch = new Stopwatch();
		watch.Start();

		for ( int t = 0; t < threads; t++ )
		{
			tasks.Add( Task.Run( () =>
			{
				Sandbank.SelectUnsafeReferences<ReadmeExample>( "players", x => x.Health >= 90 );
			} ) );
		}

		Task.WhenAll( tasks ).GetAwaiter().GetResult();

		watch.Stop();

		Console.WriteLine( $"Benchmark: [multi-threaded] SelectUnsafeReferences_MoreIntense() - {collectionSize} documents searched {threads} times in {watch.Elapsed.TotalSeconds} seconds (~10,080 records returned)" );
	}

	[TestMethod]
	public void BenchmarkSelectOneWithID()
	{
		int collectionSize = 100800;
		string id = "";
		int repeats = 100000;

		for ( int i = 0; i < collectionSize; i++ )
		{
			var data = new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			};

			Sandbank.Insert( "players", data );

			if ( i == collectionSize / 2 )
				id = data.UID;
		}

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.
		var watch = new Stopwatch();

		watch.Start();

		for ( int i = 0; i < repeats; i++ )
		{
			Sandbank.SelectOneWithID<ReadmeExample>( "players", id );
		}

		watch.Stop();

		Console.WriteLine( $"Benchmark: SelectOneWithID() - {collectionSize} documents searched {repeats} times in {watch.Elapsed.TotalSeconds} seconds" );
	}

	[TestMethod]
	public void BenchmarkSelectOneWithIDThreaded()
	{
		int collectionSize = 100800;
		int repeats = 100000;
		int threads = 24;
		string id = "";

		for ( int i = 0; i < collectionSize; i++ )
		{
			var data = new ReadmeExample()
			{
				Health = Game.Random.Next( 101 ),
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			};

			Sandbank.Insert( "players", data );

			if ( i == collectionSize / 2 )
				id = data.UID;
		}

		List<Task> tasks = new();

		Task.Delay( 4000 ).GetAwaiter().GetResult(); // Let the pool warm up.
		var watch = new Stopwatch();
		watch.Start();

		for ( int t = 0; t < threads; t++ )
		{
			tasks.Add( Task.Run( () =>
			{
				for ( int i = 0; i < repeats; i++ )
				{
					Sandbank.SelectOneWithID<ReadmeExample>( "players", id );
				}
			} ) );
		}

		Task.WhenAll( tasks ).GetAwaiter().GetResult();

		watch.Stop();

		Console.WriteLine( $"Benchmark: [multi-threaded] SelectOneWithID() - {collectionSize} documents searched {repeats} times in {watch.Elapsed.TotalSeconds} seconds" );
	}
}
