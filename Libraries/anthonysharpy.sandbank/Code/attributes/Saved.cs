using System;

namespace SandbankDatabase;

/// <summary>
/// Add this attribute to a property to allow it to be saved to file.
/// </summary>
[AttributeUsage( AttributeTargets.Property )]
public sealed class Saved : Attribute
{
}
