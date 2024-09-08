using Sandbox.Internal;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SandbankDatabase;

internal static class Serialisation
{
	private static JsonSerializerOptions _jsonOptions = new()
	{
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = Config.INDENT_JSON,
		Converters = { new GenericSavedDataConverter() }
	};

	public static string SerialiseClass<T>( T theClass )
	{
		return JsonSerializer.Serialize( theClass, _jsonOptions );
	}

	public static T DeserialiseClass<T>( string data )
	{
		return JsonSerializer.Deserialize<T>( data, _jsonOptions );
	}

	public static string SerialiseJSONObject( JsonObject obj )
	{
		return obj.ToJsonString( _jsonOptions );
	}

	public static string SerialiseClass( object theClass, Type classType )
	{
		return JsonSerializer.Serialize( theClass, classType, _jsonOptions );
	}

	public static object DeserialiseClass( string data, Type type )
	{
		return JsonSerializer.Deserialize( data, type, _jsonOptions );
	}
}

public class GenericSavedDataConverter : JsonConverter<object>
{
	public override object Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		var instance = GlobalGameNamespace.TypeLibrary.Create<object>( typeToConvert );
		var properties = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( instance, true )
			.Where( prop => prop.Attributes.Any( a => a is Saved ) );

		if ( reader.TokenType != JsonTokenType.StartObject )
		{
			throw new JsonException();
		}

		while ( reader.Read() )
		{
			if ( reader.TokenType == JsonTokenType.EndObject )
			{
				return instance;
			}

			if ( reader.TokenType != JsonTokenType.PropertyName )
			{
				throw new JsonException();
			}

			var propertyName = reader.GetString();
			var property = properties.FirstOrDefault( prop =>
				string.Equals( prop.Name, propertyName, StringComparison.OrdinalIgnoreCase ) );

			if ( property != null )
			{
				reader.Read();
				var value = JsonSerializer.Deserialize( ref reader, property.PropertyType );
				property.SetValue( instance, value );
			}
			else
			{
				reader.Skip();
			}
		}

		throw new JsonException( "Expected end of object." );
	}

	public override void Write( Utf8JsonWriter writer, object value, JsonSerializerOptions options )
	{
		writer.WriteStartObject();

		var properties = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( value )
			.Where( prop => prop.Attributes.Any( a => a is Saved ) );

		foreach ( var prop in properties )
		{
			writer.WritePropertyName( prop.Name );
			JsonSerializer.Serialize( writer, prop.GetValue( value ), prop.PropertyType );
		}

		writer.WriteEndObject();
	}

	/// <summary>
	/// Don't delete this as it does actually do something!
	/// </summary>
	public override bool CanConvert( Type typeToConvert )
	{
		// Optionally, you can refine this method to return false for types that shouldn't use this converter
		return true; // As a simple approach, return true to indicate it can convert any object
	}
}

