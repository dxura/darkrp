using System;
using Sandbox.Internal;

namespace SandbankDatabase;

sealed class Document
{
	/// <summary>
	/// This is also stored embedded in the Data object, but we keep it
	/// here as an easily-accessible copy for convenience. We call it UID instead
	/// of ID because s&amp;box already has its own "Id" field on components.
	/// </summary>
	[Saved] public string UID { get; private set; }

	/// <summary>
	/// We could save the data as a dictionary, which would stop us from having to
	/// clone a new object on document creation. However, this would stop us from
	/// easily doing lambdas against the document data, so it's not really worth it.
	/// </summary>
	[Saved] public object Data { get; private set; }

	public Type DocumentType;
	public string CollectionName;

	public Document( object data, Type documentType, bool needsCloning, string collectionName )
	{
		if ( !PropertyDescriptionsCache.DoesClassHaveUIDProperty( documentType.FullName, data ) )
			throw new SandbankException( "cannot handle a document without a \"UID\" property - make sure your data class has a public property called UID, like this: \"[Saved] public string UID { get; set; }\"" );

		string id = (string)GlobalGameNamespace.TypeLibrary.GetPropertyValue( data, "UID" );

		if ( id != null && id.Length > 0 )
		{
			UID = id;
		}
		else
		{
			UID = Guid.NewGuid().ToString().Replace( "-", "" );

			// We DO want to modify the UID of the passed-in reference.
			GlobalGameNamespace.TypeLibrary.SetProperty( data, "UID", UID );
		}

		DocumentType = documentType;
		CollectionName = collectionName;

		// We want to avoid modifying a passed-in reference, so we clone it.
		// But this is redundant in some cases, in which case we don't do it.
		if ( needsCloning )
			data = ObjectPool.CloneObject( data, documentType );

		Data = data;
		Cache.StaleDocuments.Add( this );
	}
}
