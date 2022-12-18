/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

using Conditional = System.Diagnostics.ConditionalAttribute;

namespace NativeGridNamespace
{
	/// <summary> Non-generic, abstract parent class for NativeGrid<T>. </summary>
	public abstract partial class NativeGrid
	{
		#region ASSERTIONS


		[Conditional("UNITY_ASSERTIONS")]
		static void ASSERT_TRUE ( in bool b , in FixedString128Bytes text )
		{
			if( !b ) Debug.LogError(text);
		}


		[Conditional("UNITY_ASSERTIONS")]
		static void Assert_IndexTranslate ( RectInt r , int rx , int ry , int R_width )
		{
			ASSERT_TRUE( R_width>0 , $"FAILED: R_width ({R_width}) > 0" );
			ASSERT_TRUE( r.width<=R_width , $"FAILED: r.width ({r.width}) > ({R_width})  R_width" );
			Assert_IndexTranslate( r , rx , ry );
		}
		[Conditional("UNITY_ASSERTIONS")]
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

		[Conditional("UNITY_ASSERTIONS")]
		static void Assert_IndexToCoord ( int i , int width )
		{
			ASSERT_TRUE( width>0 , $"FAILED: width ({width}) > 0" );
			ASSERT_TRUE( i>=0 , $"FAILED: i ({i}) >= 0" );
		}

		[Conditional("UNITY_ASSERTIONS")]
		static void Assert_CoordToIndex ( int x , int y , int width )
		{
			ASSERT_TRUE( width>0 , $"FAILED: width ({width}) > 0" );
			ASSERT_TRUE( x>=0 , $"FAILED: x ({x}) >= 0" );
			ASSERT_TRUE( y>=0 , $"FAILED: y ({y}) >= 0" );
			ASSERT_TRUE( x<width , $"FAILED: x ({x}) < ({width}) width" );
		}
		static void Assert_CoordToIndex ( INT2 coord , int width ) => Assert_CoordToIndex( coord.x , coord.y , width );


		#endregion
	}
}
