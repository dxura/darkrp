using SandbankDatabase;
using System.Collections.Generic;
using System;

/// <summary>
/// We have to define these here because s&amp;box's type library can't recognise
/// types defined outside the assembly.
/// </summary>
internal static class TestClasses
{
	public class NullUIDClass
	{
		[Saved] public string UID { get; set; }
	}

	public class NoUIDClass
	{
		[Saved] public string Name { get; set; }
	}

	public class ValidClass1
	{
		[Saved] public string UID { get; set; }
		[Saved] public int Health { get; set; }
	}

	public class ClassWithNonSavedProperty
	{
		[Saved] public string UID { get; set; }
		public string Name { get; set; }
		[Saved] public int Health { get; set; }
	}

	public class ValidClass1Copy
	{
		[Saved] public string UID { get; set; }
		[Saved] public int Health { get; set; }
	}

	public class ValidClass2
	{
		[Saved] public string UID { get; set; }
		[Saved] public int Health { get; set; }
	}

	public class ReadmeExample
	{
		[Saved] public string UID { get; set; }
		[Saved] public float Health { get; set; }
		[Saved] public string Name { get; set; }
		[Saved] public int Level { get; set; }
		[Saved] public DateTime LastPlayTime { get; set; }
		[Saved] public List<string> Items { get; set; } = new();
	}

	public class ReadmeExampleWithFewerFields
	{
		[Saved] public string UID { get; set; }
		[Saved] public float Health { get; set; }
	}
}
