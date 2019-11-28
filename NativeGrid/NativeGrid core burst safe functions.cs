/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

/// <summary>
/// Abstract parent class for generic NativeGrid<STRUCT>. To simplify referencing static functions/types from "NativeGrid<byte>.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
/// </summary>
public abstract partial class NativeGrid
{
	#region BURST SAFE

	[System.Obsolete("\"BurstSafe\" is obsolete and no longer needed.")]
	public abstract partial class BurstSafe
	{
		public static int2 Index1dTo2d ( INT i , INT width ) => NativeGrid.Index1dTo2d( i , width );
		public static int Index2dTo1d ( INT x , INT y , INT width ) => NativeGrid.Index2dTo1d( x , y , width );
		public static int Index2dTo1d ( INT2 i2 , INT width ) => NativeGrid.Index2dTo1d( i2 , width );
		public static int ClampIndex ( INT i , INT length ) => NativeGrid.ClampIndex( i , length );
		public static int2 ClampIndex2d ( INT x , INT y , INT width , INT height ) => NativeGrid.ClampIndex2d( x , y , width , height );
		public static int2 ClampIndex2d ( INT2 i2 , INT width , INT height ) => NativeGrid.ClampIndex2d ( i2 , width , height );
		public static int IndexTranslate ( RectInt r , INT rx , INT ry , INT R_width ) => NativeGrid.IndexTranslate( r , rx , ry , R_width );
		public static int2 IndexTranslate ( RectInt r , INT2 rxy ) => NativeGrid.IndexTranslate( r , rxy );
		public static int IndexTranslate ( RectInt r , INT ri , INT R_width ) => NativeGrid.IndexTranslate( r , ri , R_width );
		public static bool IsIndex2dValid ( INT x , INT y , INT w , INT h ) => NativeGrid.IsIndex2dValid( x , y , w , h );
		public static bool IsIndex2dValid ( INT2 i2 , INT w , INT h ) => NativeGrid.IsIndex2dValid( i2 , w , h );
		public static bool IsIndex1dValid ( INT i , INT len ) => NativeGrid.IsIndex1dValid( i , len );
		public static float2 Index2dToPoint ( INT x , INT y , FLOAT stepX , FLOAT stepY ) => NativeGrid.Index2dToPoint( x , y , stepX , stepY );
		public static float2 Index2dToPoint ( INT2 i2 , FLOAT stepX , FLOAT stepY ) => NativeGrid.Index2dToPoint( i2 , stepX , stepY );
		public static float2 Index2dToPoint ( INT2 i2 , FLOAT2 step ) => NativeGrid.Index2dToPoint( i2 , step );
		public static T PointToValue <T> ( FLOAT2 point , FLOAT2 worldSize , NativeArray<T> array , INT width , INT height ) where T : unmanaged => NativeGrid.PointToValue<T>( point , worldSize , array , width , height );
		public static int PointToIndex ( FLOAT2 point , FLOAT2 worldSize , INT width , INT height ) => NativeGrid.PointToIndex( point , worldSize , width , height );
		public static int2 PointToIndex2d ( FLOAT2 point , FLOAT2 worldSize , INT width , INT height ) => NativeGrid.PointToIndex2d( point , worldSize , width , height );
		public static void GetPositionInsideCell ( FLOAT point , INT numCells , FLOAT worldSize , out int lowerCell , out int upperCell , out float normalizedPositionBetweenThoseTwoPoints ) => NativeGrid.GetPositionInsideCell ( point , numCells , worldSize , out lowerCell , out upperCell , out normalizedPositionBetweenThoseTwoPoints );
		public static void GetPositionInsideCell ( FLOAT2 point , INT2 numCells , FLOAT2 worldSize , out int2 lowerCell , out int2 upperCell , out float2 normalizedPositionBetweenThoseTwoPoints ) => NativeGrid.GetPositionInsideCell( point , numCells , worldSize , out lowerCell , out upperCell , out normalizedPositionBetweenThoseTwoPoints );
		public static int IsPointBetweenCells ( FLOAT point , INT width , FLOAT worldSize ) => NativeGrid.IsPointBetweenCells( point , width , worldSize );
		public static bool AboutEqual ( double x , double y ) => NativeGrid.AboutEqual( x , y );
		public static int MidpointRoundingAwayFromZero ( FLOAT value ) => NativeGrid.MidpointRoundingAwayFromZero( value );
		public static float MidpointRoundingAwayFromZero ( FLOAT value , FLOAT step ) => NativeGrid.MidpointRoundingAwayFromZero( value , step );
		public static int2 MidpointRoundingAwayFromZero ( FLOAT2 value ) => NativeGrid.MidpointRoundingAwayFromZero( value );
		public static float2 MidpointRoundingAwayFromZero ( FLOAT2 value , FLOAT2 step ) => NativeGrid.MidpointRoundingAwayFromZero( value , step );
	}

	#endregion
}
