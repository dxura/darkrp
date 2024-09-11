using System;
using System.Collections.Concurrent;

namespace SandbankDatabase;

sealed class Collection
{
	/// <summary>
	/// Due to s&amp;box restrictions we have to save a string of the class type.
	/// We'll convert it back to a type when we load the collection from file.
	/// </summary>
	[Saved] public string DocumentClassTypeSerialized { get; set; }
	[Saved] public string CollectionName { get; set; }

	public Type DocumentClassType;

	/// <summary>
	/// All the documents in this collection.
	/// </summary>
	public ConcurrentDictionary<string, Document> CachedDocuments = new();

	/// <summary>
	/// This should be used to insert documents since this enforces that the class type is
	/// correct.
	/// </summary>
	public void InsertDocument( Document document )
	{
		if ( document.Data.GetType().ToString() != DocumentClassTypeSerialized )
		{
			throw new SandbankException( $"cannot insert a document of type {document.Data.GetType().FullName} " +
				$"into a collection which expects type {DocumentClassTypeSerialized}" );
		}

		CachedDocuments[document.UID] = document;
	}
}
