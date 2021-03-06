/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

/// <summary>
/// Abstract parent class for generic NativeGrid<STRUCT>. To simplify referencing static functions/types from "NativeGrid<byte>.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
/// </summary>
public abstract partial class NativeGrid
{
	#region ASSERTIONS


	[System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
	static void ASSERT_TRUE ( in bool b , in FixedString128 text )
	{
		if( !b ) Debug.LogError(text);
	}


	[System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
	static void Assert_IndexTranslate ( RectInt r , int rx , int ry , int R_width )
	{
		ASSERT_TRUE( R_width>0 , $"FAILED: R_width ({R_width}) > 0" );
		ASSERT_TRUE( r.width<=R_width , $"FAILED: r.width ({r.width}) > ({R_width})  R_width" );
		Assert_IndexTranslate( r , rx , ry );
	}
	[System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
	static void Assert_IndexTranslate ( RectInt r , int rx , int ry )
	{
		ASSERT_TRUE( rx>=0 , $"FAILED: rx ({rx}) >= 0" );
		ASSERT_TRUE( ry>=0 , $"FAILED: ry ({ry}) >= 0" );

		ASSERT_TRUE( r.width>0 , $"FAILED: r.width ({r.width}) > 0" );
		ASSERT_TRUE( r.height>0 , $"FAILED: r.height ({r.height}) > 0" );
		ASSERT_TRUE( r.x>=0 , $"FAILED: r.x ({r.x}) >= 0" );
		ASSERT_TRUE( r.y>=0 , $"FAILED: r.y ({r.y}) >= 0" );

		ASSERT_TRUE( rx>=0 && rx<r.width , $"FAILED: rx ({rx}) is out of bounds for r ({r})" );
		ASSERT_TRUE( ry>=0 && ry<r.height , $"FAILED: ry ({ry}) is out of bounds for r ({r})" );
	}

	[System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
	static void Assert_Index1dTo2d ( int i , int width )
	{
		ASSERT_TRUE( width>0 , $"FAILED: width ({width}) > 0" );
		ASSERT_TRUE( i>=0 , $"FAILED: i ({i}) >= 0" );
	}

	[System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
	static void Assert_Index2dTo1d ( int x , int y , int width )
	{
		ASSERT_TRUE( width>0 , $"FAILED: width ({width}) > 0" );
		ASSERT_TRUE( x>=0 , $"FAILED: x ({x}) >= 0" );
		ASSERT_TRUE( y>=0 , $"FAILED: y ({y}) >= 0" );
		ASSERT_TRUE( x<width , $"FAILED: x ({x}) < ({width}) width" );
	}
	static void Assert_Index2dTo1d ( INT2 Index2d , int width ) => Assert_Index2dTo1d( Index2d.x , Index2d.y , width );


	#endregion
}
