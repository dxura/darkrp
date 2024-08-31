using Dxura.Darkrp;

namespace Dxura.Darkrp;

public class GameManager : SingletonComponent<GameManager>
{
	private readonly Dictionary<Type, Component> _componentCache = new();

	/// <summary>
	/// Gets the given component from within the game mode's object hierarchy, or null if not found / enabled.
	/// </summary>
	public T Get<T>( bool required = false )
		where T : class
	{
		if ( !_componentCache.TryGetValue( typeof(T), out var component ) || component is { IsValid: false } ||
		     component is { Active: false } )
		{
			component = Components.GetInDescendantsOrSelf<T>() as Component;
			_componentCache[typeof(T)] = component;
		}

		if ( required && component is not T )
		{
			throw new Exception( $"Expected a {typeof(T).Name} to be active!" );
		}

		return component as T;
	}
}
