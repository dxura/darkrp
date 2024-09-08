namespace SandbankDatabase;

internal static class Cloning
{
	public static void CopyClassData<T>( T sourceClass, T destinationClass )
	{
		// This is probably not much faster since we still have to call GetType, but
		// oh well.
		var properties = PropertyDescriptionsCache.GetPropertyDescriptionsForType( sourceClass.GetType().FullName,
			sourceClass );
		
		foreach ( var property in properties )
		{
			property.SetValue( destinationClass, property.GetValue( sourceClass ) );
		}
	}

	public static void CopyClassData( object sourceClass, object destinationClass, string classTypeName )
	{
		var properties = PropertyDescriptionsCache.GetPropertyDescriptionsForType( classTypeName, sourceClass );

		foreach ( var property in properties )
		{
			property.SetValue( destinationClass, property.GetValue( sourceClass ) );
		}
	}
}
