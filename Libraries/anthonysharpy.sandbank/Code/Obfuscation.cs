using System;
using System.Runtime.CompilerServices;

namespace SandbankDatabase; 

static class Obfuscation
{
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static int[] GenerateShiftMask()
	{
		int[] result = new int[32];

		for (int i = 0; i < 32; i++)
			result[i] = Random.Shared.Next(256);

		return result;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static string ObfuscateFileText( string originalText )
	{
		var textArray = originalText.ToCharArray();
		var shiftMask = GenerateShiftMask();
		var maskN = 0;

		for ( int i = 0; i < textArray.Length; i++ )
		{
			textArray[i] = (char)(textArray[i] + shiftMask[maskN]);

			if ( textArray[i] > char.MaxValue )
				textArray[i] = (char)(textArray[i] - char.MaxValue);

			maskN = (maskN + 1) % 32;
		}

		return $"OBFS|{string.Join( '-', shiftMask )}|{new string(textArray) }";
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static string UnobfuscateFileText( string obfuscatedText )
	{
		var maskStart = obfuscatedText.IndexOf( '|' )+1;
		var maskEnd = obfuscatedText.IndexOf( '|', maskStart )-1;

		var mask = obfuscatedText.Substring( maskStart, (maskEnd - maskStart) + 1 );
		var maskParts = mask.Split( '-' );
		var shiftMask = new int[32];

		for ( int i = 0; i < 32; i++ )
			shiftMask[i] = int.Parse( maskParts[i] );

		var textArray = obfuscatedText.ToCharArray().AsSpan();
		var maskN = 0;

		for ( int c = maskEnd+2; c < textArray.Length; c++ )
		{
			textArray[c] = (char)(textArray[c] - shiftMask[maskN]);

			if ( textArray[c] < 0 )
				textArray[c] = (char)(textArray[c] + char.MaxValue);

			maskN = ( maskN + 1 ) % 32;
		}

		return new string( textArray.Slice(maskEnd+2) );
	}
}
