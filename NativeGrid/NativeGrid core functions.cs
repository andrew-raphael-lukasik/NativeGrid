/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

/// <summary>
/// Abstract parent class for generic NativeGrid<STRUCT>. To simplify referencing static functions/types from "NativeGrid<byte>.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
/// </summary>
public abstract partial class NativeGrid
{
	#region PUBLIC METHODS


	/// <summary> Converts 1d to 2d array index </summary>
	public static int2 Index1dTo2d ( int i , int width )
	{
		Assert_Index1dTo2d( i , width );

		return BurstSafe.Index1dTo2d( i , width );
	}
	

	/// <summary> Converts index 2d to 1d equivalent </summary>
	public static int Index2dTo1d ( int x , int y , int width )
	{
		Assert_Index2dTo1d( x , y , width );

		return BurstSafe.Index2dTo1d( x , y , width );
	}
	public static int Index2dTo1d ( INT2 index2d , int width ) => Index2dTo1d( index2d.x , index2d.y , width );


	/// <summary> Translate regional coordinate to outer array index 1d </summary>
	/// <param name="R">Outer RectInt</param>
	/// <param name="r">Inner, smaller RectInt</param>
	/// <param name="rx">Inner x coordinate</param>
	/// <param name="ry">Inner y coordinate</param>
	/// <param name="R_width">Outer RectInt.width</param>
	public static int IndexTranslate ( RectInt r , int rx , int ry , int R_width )
	{
		Assert_IndexTranslate( r , rx , ry , R_width );

		return Index2dTo1d( r.x+rx , r.y+ry , R_width );
	}

	/// <summary> Translate regional coordinate to outer array index 1d </summary>
	/// <param name="R">Outer RectInt</param>
	/// <param name="r">Inner, smaller RectInt</param>
	/// <param name="rx">Inner x coordinate</param>
	/// <param name="ry">Inner y coordinate</param>
	/// <param name="R_width">Outer RectInt.width</param>
	public static int2 IndexTranslate ( RectInt r , int2 rxy )
	{
		Assert_IndexTranslate( r , rxy.x , rxy.y );

		return BurstSafe.IndexTranslate( r , rxy );
	}

	/// <summary> Translate regional index to outer one </summary>
	/// <param name="R">Outer RectInt</param>
	/// <param name="r">Inner, smaller RectInt</param>
	/// <param name="ri">Index in inner rect</param>
	/// <param name="R_width">Outer RectInt.width</param>
	public static int IndexTranslate ( RectInt r , int ri , int R_width )
	{
		int2 ri2d = Index1dTo2d( ri , r.width );
		return IndexTranslate( r , ri2d.x , ri2d.y , R_width );
	}


	/// <summary> Determines whether index 2d is inside array bounds </summary>
	public static bool IsIndex2dValid ( int x , int y , int w , int h ) => BurstSafe.IsIndex2dValid( x , y , w , h );
	public static bool IsIndex2dValid ( INT2 index2d , int w , int h ) => IsIndex2dValid( index2d.x , index2d.y , w , h );


	/// <summary> Determines whether index 1d is inside array bounds </summary>
	public static bool IsIndex1dValid ( int i , int len ) => BurstSafe.IsIndex1dValid( i , len );


	/// <summary> Point from 2d indices </summary>
	public static float2 Index2dToPoint ( int x , int y , float stepX , float stepY ) => BurstSafe.Index2dToPoint ( x , y , stepX , stepY );
	public static float2 Index2dToPoint ( INT2 index2d , float stepX , float stepY ) => Index2dToPoint( index2d.x , index2d.y , stepX , stepY );


	/// <summary> Value at point </summary>
	public static T PointToValue <T> ( FLOAT2 point , FLOAT2 worldSize , NativeArray<T> array , int width , int height ) where T : unmanaged
	{
		return array[ PointToIndex( point , worldSize , width , height ) ];
	}


	/// <summary> Index from point </summary>
	public static int PointToIndex ( FLOAT2 point , FLOAT2 worldSize , int width , int height )
	{
		int2 xy = BurstSafe.PointToIndex2d( point , worldSize , width , height );
		return Index2dTo1d( xy.x , xy.y , width );
	}


	#endregion
}
