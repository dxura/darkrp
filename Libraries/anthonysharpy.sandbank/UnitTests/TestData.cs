using System;
using static TestClasses;

namespace SandbankDatabase;

internal static class TestData
{
	public static ReadmeExample TestData1 = new ReadmeExample()
	{
		UID = "",
		Health = 100,
		Name = "TestPlayer1",
		Level = 10,
		LastPlayTime = DateTime.UtcNow,
		Items = new() { "gun", "frog", "banana" }
	};

	public static ReadmeExample TestData2 = new ReadmeExample()
	{
		UID = "",
		Health = 90,
		Name = "TestPlayer2",
		Level = 15,
		LastPlayTime = DateTime.UtcNow,
		Items = new() { "apple", "box" }
	};
}
