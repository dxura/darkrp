using Sandbox;
using Sandbox.Internal;
using System;
using System.Collections.Concurrent;

namespace SandbankDatabase;

/// <summary>
/// Provides class instances so that we don't need to create instances on-the-fly,
/// which is a major performance bottleneck.
/// </summary>
internal static class ObjectPool
{
	private static DateTime _timeLastCheckedPool;
	private static ConcurrentDictionary<string, PoolTypeDefinition> _objectPool = new();

	public static void WipeStaticFields()
	{
		_objectPool = new();
		_timeLastCheckedPool = DateTime.UtcNow.AddHours( -1 );
	}

	public static T CloneObject<T>( T theObject, string classTypeName ) where T : class, new()
	{
		var instance = GetInstance<T>( classTypeName );
		Cloning.CopyClassData( theObject, instance, classTypeName );
		return instance;
	}

	public static object CloneObject( object theObject, Type objectType )
	{
		var instance = GetInstance( objectType.FullName, objectType );
		Cloning.CopyClassData( theObject, instance, objectType.FullName );
		return instance;
	}

	public static object GetInstance( string classTypeName, Type classType )
	{
		if ( !_objectPool.ContainsKey(classTypeName) )
		{
			throw new SandbankException( $"there is no registered instance pool for the type {classTypeName} - " +
				"are you using the wrong class type for this collection?" );
		}

		if ( _objectPool[classTypeName].TypePool.TryTake( out var instance ) )
			return instance;

		// If we couldn't get an instance, then we just have to create a new one.
		return GlobalGameNamespace.TypeLibrary.Create<object>( classType );
	}

	public static T GetInstance<T>(string classType) where T : class, new()
	{
		if ( _objectPool[classType].TypePool.TryTake( out var instance ) )
			return (T)instance;

		// If we couldn't get an instance, then we just have to create a new one.
		return new T();
	}

	/// <summary>
	/// Tell the pool that we want to pool this class type.
	/// </summary>
	public static void TryRegisterType( string classTypeName, Type classType )
	{
		// Different collections might use the same type. So this is possible.
		if ( _objectPool.ContainsKey( classTypeName ) )
			return;

		_objectPool[classTypeName] = new PoolTypeDefinition
		{
			ObjectType = classType,
			TypePool = new ConcurrentBag<object>()
		};
	}

	public static void TryCheckPool()
	{
		var now = DateTime.UtcNow;

		if ( now.Subtract( _timeLastCheckedPool ).TotalMilliseconds <= 1000 )
			return;

		_timeLastCheckedPool = now;

		foreach (var poolPair in _objectPool)
		{
			if ( Config.CLASS_INSTANCE_POOL_SIZE - poolPair.Value.TypePool.Count >= Config.CLASS_INSTANCE_POOL_SIZE / 2)
			{
				GameTask.RunInThreadAsync( () => ReplenishPoolType( poolPair.Key, poolPair.Value.ObjectType ) );
			}
		}
	}

	private static void ReplenishPoolType(string classTypeName, Type classType )
	{
		var concurrentList = _objectPool[classTypeName].TypePool;
		int instancesToCreate = Config.CLASS_INSTANCE_POOL_SIZE - concurrentList.Count;

		for ( int i = 0; i < instancesToCreate; i++ )
		{
			concurrentList.Add(GlobalGameNamespace.TypeLibrary.Create<object>( classType ));
		}
	}
}

internal struct PoolTypeDefinition
{
	public Type ObjectType;
	public ConcurrentBag<object> TypePool;
}
