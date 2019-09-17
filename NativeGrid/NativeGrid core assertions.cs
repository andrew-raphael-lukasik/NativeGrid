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


	#if DEBUG

	[Unity.Burst.BurstDiscard]
	static void Assert_IndexTranslate ( RectInt r , int rx , int ry , int R_width )
	{
		Assert.IsTrue( R_width>0 , $"FAILED: R_width ({R_width}) > 0" );
		Assert.IsTrue( r.width<=R_width , $"FAILED: r.width ({r.width}) > ({R_width})  R_width" );
		Assert_IndexTranslate( r , rx , ry );
	}
	[Unity.Burst.BurstDiscard]
	static void Assert_IndexTranslate ( RectInt r , int rx , int ry )
	{
		Assert.IsTrue( rx>=0 , $"FAILED: rx ({rx}) >= 0" );
		Assert.IsTrue( ry>=0 , $"FAILED: ry ({ry}) >= 0" );

		Assert.IsTrue( r.width>0 , $"FAILED: r.width ({r.width}) > 0" );
		Assert.IsTrue( r.height>0 , $"FAILED: r.height ({r.height}) > 0" );
		Assert.IsTrue( r.x>=0 , $"FAILED: r.x ({r.x}) >= 0" );
		Assert.IsTrue( r.y>=0 , $"FAILED: r.y ({r.y}) >= 0" );

		Assert.IsTrue( rx>=0 && rx<r.width , $"FAILED: rx ({rx}) is out of bounds for r ({r})" );
		Assert.IsTrue( ry>=0 && ry<r.height , $"FAILED: ry ({ry}) is out of bounds for r ({r})" );
	}

	[Unity.Burst.BurstDiscard]
	static void Assert_Index1dTo2d ( int i , int width )
	{
		Assert.IsTrue( width>0 , $"FAILED: width ({width}) > 0" );
		Assert.IsTrue( i>=0 , $"FAILED: i ({i}) >= 0" );
	}

	[Unity.Burst.BurstDiscard]
	static void Assert_Index2dTo1d ( int x , int y , int width )
	{
		Assert.IsTrue( width>0 , $"FAILED: width ({width}) > 0" );
		Assert.IsTrue( x>=0 , $"FAILED: x ({x}) >= 0" );
		Assert.IsTrue( y>=0 , $"FAILED: y ({y}) >= 0" );
		Assert.IsTrue( x<width , $"FAILED: x ({x}) < ({width}) width" );
	}
	static void Assert_Index2dTo1d ( INT2 Index2d , int width ) => Assert_Index2dTo1d( Index2d.x , Index2d.y , width );

	#endif


	#endregion
}
