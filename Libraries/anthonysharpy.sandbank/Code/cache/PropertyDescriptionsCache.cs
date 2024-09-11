using Sandbox.Internal;
using System.Collections.Concurrent;
using Sandbox;
using System.Linq;

namespace SandbankDatabase;

/// <summary>
/// When we clone objects via reflection, we do so based on the available properties on the
/// class. We cache that information here, rather than deducing it on every clone.
/// </summary>
internal static class PropertyDescriptionsCache
{
	private static ConcurrentDictionary<string, PropertyDescription[]> _propertyDescriptionsCache = new();

	public static void WipeStaticFields()
	{
		_propertyDescriptionsCache = new();
	}

	public static bool DoesClassHaveUIDProperty( string classTypeName, object instance )
	{
		// If we have a record of it then it must do since we only cache valid types.
		if ( _propertyDescriptionsCache.TryGetValue( classTypeName, out var properties ) )
			return true;

		return GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( instance )
			.Where( prop => prop.Attributes.Any( a => a is Saved ) )
			.Any( x => x.Name == "UID" );
	}

	/// <summary>
	/// Returns type information for all [Saved] properties on this class instance.
	/// </summary>
	public static PropertyDescription[] GetPropertyDescriptionsForType( string classTypeName, object instance )
	{
		if ( _propertyDescriptionsCache.TryGetValue( classTypeName, out var properties ) )
			return properties;

		properties = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( instance )
			.Where( prop => prop.Attributes.Any( a => a is Saved ) )
			.ToArray();

		// Only cache if this is a valid type with a UID property.
		if ( properties.Any( x => x.Name == "UID" ) )
			_propertyDescriptionsCache[classTypeName] = properties;

		return properties;
	}
}
