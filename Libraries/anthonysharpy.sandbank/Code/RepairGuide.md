## no definition.txt for collection "X" found

You have probably accidentally deleted the definition.txt file in the collection's folder. You should un-delete it. If you have lost it, you can write one manually. The file should look something like this:

```
{
  "DocumentClassTypeSerialized": "MyGameNamespace.PlayerData",
  "CollectionName": "players"
}
```

Replace "MyGameNamespace.PlayerData" with whatever class type you're using.

Replace "players" with the name of your collection. It must match the name of the folder it's in.

Alternatively, if you want to re-create the collection from scratch, just delete the folder entirely.

## the CollectionName in the definition.txt differed from the name of the directory

"CollectionName" in the definition.txt must match the name of the folder it is in. Either re-name your folder or edit the definition.txt.

## your JSON is probably invalid

The JSON is messed-up somehow. You need to repair it manually. Alternatively, you can delete the document if you don't care about it.

## the filename does not match the UID

The name of the file must match the "UID" field in the document. Either change the file name or the value of the UID field so that they match.

## couldn't load the type described by the definition.txt

The definition.txt for each collection contains a "DocumentClassTypeSerialized" field, which describes the name of the data type used by this collection. If you rename your data type class, or delete it, you will no longer be able to load the data, as the database can no longer find it. To fix this, change the name of the type described by "DocumentClassTypeSerialized" so that it actually matches the name of a class in your codebase. Alternatively, just delete the entire collection if you don't need it anymore.