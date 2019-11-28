using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

static class NATIVE_GRID
{
	static class INDICES
	{
		
		[Test] public static void Index1dTo2d ()
		{
			RectInt R = new RectInt{ width=6 , height=6 };
			Assert.AreEqual( new int2{ x=1 , y=2 } , NativeGrid.Index1dTo2d(13,R.width) );
		}

		[Test] public static void Index2dTo1d ()
		{
			RectInt R = new RectInt{ width=6 , height=6 };
			Assert.AreEqual( 13 , NativeGrid.Index2dTo1d(1,2,R.width) );
		}
		
		[Test] public static void IndexTranslate ()
		{
			RectInt R = new RectInt{ width=6 , height=6 };
			RectInt r = new RectInt{ x=1 , y=1 , width=3 , height=3 };
			Assert.AreEqual( new int2(1,2) , NativeGrid.IndexTranslate(r,new int2(0,1)) );
			Assert.AreEqual( 20 , NativeGrid.IndexTranslate(r,1,2,R.width) );
		}

		static class BURST_SAFE
		{
			[Test] public static void MidpointRoundingAwayFromZero ()
			{
				for( float range=2.1f, value=-range ; value<range ; value+=0.001f )
					Assert.AreEqual( System.Math.Round( value , System.MidpointRounding.AwayFromZero ) , NativeGrid.MidpointRoundingAwayFromZero(value) );
			}

			[Test] public static void PointToIndex2d_ReproCase1 () => PointToIndex2d_Test( new float2(-1498.664f,-176.8691f) , new float2(-1499f,-177f) );
			[Test] public static void PointToIndex2d_ReproCase2 () => PointToIndex2d_Test( new float2(-1486.99f,-167.4532f) , new float2(-1487f,-167f) );
			[Test] public static void PointToIndex2d_ReproCase3 () => PointToIndex2d_Test( new float2(-1052.006f,-125.7217f) , new float2(-1053f,-125f) );
			[Test] public static void PointToIndex2d_ReproCase4 () => PointToIndex2d_Test( new float2(-362.202f,78.26967f) , new float2(-363f,79f) );
			[Test] public static void PointToIndex2d_ReproCase5 () => PointToIndex2d_Test( new float2(640.7607f,-180.0993f) , new float2(641f,-181f) );
			[Test] public static void PointToIndex2d_ReproCase6 () => PointToIndex2d_Test( new float2(992.0559f,-373.1479f) , new float2(993f,-373f) );
			[Test] public static void PointToIndex2d_ReproCase7 () => PointToIndex2d_Test( new float2(1267.036f,-476.1908f) , new float2(1267f,-477f) );
			static void PointToIndex2d_Test ( float2 a , float2 b )
			{
				float2 worldSize = new float2( 3000f , 3000f );
				float2 gridOrigin = new float2( -1500f , -1500f );
				const int width = 1500, height = 1500;
				INT2 A = NativeGrid.PointToIndex2d( a-gridOrigin, worldSize , width , height );
				INT2 B = NativeGrid.PointToIndex2d( b-gridOrigin , worldSize , width , height );
				// Debug.Log( $"a:{GetPositionInsideCell_GetDebugString(a,width,height,worldSize)}\nb:{GetPositionInsideCell_GetDebugString(b,width,height,worldSize)}" );
				Assert.AreEqual( A , B );
			}

			[Test] public static void PointToIndex2d_2x2_0f_N0f1 () => PointToIndex2d_Test_2x2( 0f , -0.1f );
			[Test] public static void PointToIndex2d_2x2_0f_0f1 () => PointToIndex2d_Test_2x2( 0f , 0.1f );
			[Test] public static void PointToIndex2d_2x2_0f_0f2 () => PointToIndex2d_Test_2x2( 0f , 0.2f );
			[Test] public static void PointToIndex2d_2x2_0f_0f499 () => PointToIndex2d_Test_2x2( 0f , 0.499f );
			[Test] public static void PointToIndex2d_2x2_0f5_0f3 () => PointToIndex2d_Test_2x2( 0f , 0.3f );
			[Test] public static void PointToIndex2d_2x2_0f5_0f4 () => PointToIndex2d_Test_2x2( 0f , 0.4f );
			[Test] public static void PointToIndex2d_2x2_0f5_0f5 () => PointToIndex2d_Test_2x2( 0f , 0.5f );
			[Test] public static void PointToIndex2d_2x2_0f5_0f6 () => PointToIndex2d_Test_2x2( 0f , 0.6f );
			[Test] public static void PointToIndex2d_2x2_0f_0f9999 () => PointToIndex2d_Test_2x2( 0f , 0.9999f );
			[Test] public static void PointToIndex2d_2x2_0f_1fMinusEpsilon () => PointToIndex2d_Test_2x2( 0f , 1f-float.Epsilon , false );// Jeśli zacznie działać to znaczy że się precyzja zmieniła
			[Test] public static void PointToIndex2d_2x2_0f_1fMinus1E08 () => PointToIndex2d_Test_2x2( 0f , 1f - 1E-8f , false );// Jeśli zacznie działać to znaczy że się precyzja zmieniła
			[Test] public static void PointToIndex2d_2x2_0f_1fMinus1E07 () => PointToIndex2d_Test_2x2( 0f , 1f - 1E-7f );
			[Test] public static void PointToIndex2d_2x2_0f_1fMinus1E06 () => PointToIndex2d_Test_2x2( 0f , 1f - 1E-6f );
			[Test] public static void PointToIndex2d_2x2_1f_1f () => PointToIndex2d_Test_2x2( 1f , 1f );
			[Test] public static void PointToIndex2d_2x2_1f_1fPlusEpsilon () => PointToIndex2d_Test_2x2( 1f , 1f+float.Epsilon );
			[Test] public static void PointToIndex2d_2x2_1f_1f00001 () => PointToIndex2d_Test_2x2( 1f , 1.00001f );
			public static void PointToIndex2d_Test_2x2 ( float2 a , float2 b , bool equalityTest = true )
			{
				float2 worldSize = new float2( 2f , 2f ); const int width = 2, height = 2;
				INT2 A = NativeGrid.PointToIndex2d( a , worldSize , width , height );
				INT2 B = NativeGrid.PointToIndex2d( b , worldSize , width , height );
				// Debug.Log( $"a:{GetPositionInsideCell_GetDebugString(a,width,height,worldSize)}\nb:{GetPositionInsideCell_GetDebugString(b,width,height,worldSize)}" );
				if( equalityTest ) Assert.AreEqual( A , B );
				else Assert.AreNotEqual( A , B );
			}


			[Test] public static void PointToIndex2d_1x1_0f_N0f00001 () => PointToIndex2d_Test_1x1( 0f , -0.00001f );
			[Test] public static void PointToIndex2d_1x1_0f_0f00001 () => PointToIndex2d_Test_1x1( 0f , 0.00001f );
			[Test] public static void PointToIndex2d_1x1_0f_0f1 () => PointToIndex2d_Test_1x1( 0f , 0.1f );
			[Test] public static void PointToIndex2d_1x1_0f_0f49999 () => PointToIndex2d_Test_1x1( 0f , 0.49999f );
			[Test] public static void PointToIndex2d_1x1_0f_0f5MinusEpsilon () => PointToIndex2d_Test_1x1( 0f , 0.5f-float.Epsilon );
			[Test] public static void PointToIndex2d_1x1_1f_0f5 () => PointToIndex2d_Test_1x1( 1f , 0.5f );
			[Test] public static void PointToIndex2d_1x1_0f_0f5PlusEpsilon () => PointToIndex2d_Test_1x1( 1f , 0.5f+float.Epsilon );
			[Test] public static void PointToIndex2d_1x1_1f_0f9 () => PointToIndex2d_Test_1x1( 1f , 0.9f );
			[Test] public static void PointToIndex2d_1x1_1f_1f1 () => PointToIndex2d_Test_1x1( 1f , 1.1f );
			public static void PointToIndex2d_Test_1x1 ( float2 a , float2 b )
			{
				float2 worldSize = new float2{ x=2f , y=1f }; const int width = 1, height = 1;
				INT2 A = NativeGrid.PointToIndex2d( a , worldSize , width , height );
				INT2 B = NativeGrid.PointToIndex2d( b , worldSize , width , height );
				// Debug.Log( $"a:{GetPositionInsideCell_GetDebugString(a,width,height,worldSize)}\nb:{GetPositionInsideCell_GetDebugString(b,width,height,worldSize)}" );
				Assert.AreEqual( A , B );
			}

			static string GetPositionInsideCell_GetDebugString ( float2 p , int width , int height , float2 worldSize )
			{
				NativeGrid.GetPositionInsideCell( p.x , width , worldSize.x , out int xlo , out int xhi , out float xf );
				NativeGrid.GetPositionInsideCell( p.y , height , worldSize.y , out int ylo , out int yhi , out float yf );
				return $"\n	x: {xlo}... {xf:R} ...{xhi}\n	y: {ylo}... {yf:R} ...{yhi}";
			}


			[Test] public static void IsPointBetweenCells___3000f_1500___1f_IS_0 () => Assert.AreEqual( 0 , NativeGrid.IsPointBetweenCells(1f,1500,3000f) );
			// [Test] public static void IsPointBetweenCells___3000f_1500___2f_IS_1 () => Assert.AreEqual( 1 , NativeGrid.IsPointBetweenCells(2f,1500,3000f) );

			[Test] public static void IsPointBetweenCells___2f_2___1fMinusEpsilon_IS_0 () => Assert.AreEqual( 0 , NativeGrid.IsPointBetweenCells(1f-float.Epsilon,2,2f) );
			[Test] public static void IsPointBetweenCells___2f_2___1fPlusEpsilon_IS_0 () => Assert.AreEqual( 0 , NativeGrid.IsPointBetweenCells(1f+float.Epsilon,2,2f) );
			// [Test] public static void IsPointBetweenCells___2f_2___1f_IS_1 () => Assert.AreEqual( 1 , NativeGrid.IsPointBetweenCells(1f,2,2f) );

		}

		static class EQUIVALENT_METHODS_COMPARED
		{
			[Test] public static void Index1dTo2d_VS_BurstSafePointToIndex2d_CASE01 ()
			{
				Index1dTo2d_VS_BurstSafePointToIndex2d(
					position:			new float2{ x = 0.333f , y = 0.666f } ,
					gridWorldSize:		new float2{ x = 1f , y = 1f } ,
					gridWidth:			100 ,
					gridHeight:			100
				);
			}
			[Test] public static void Index1dTo2d_VS_BurstSafePointToIndex2d_CASE02 ()
			{
				Index1dTo2d_VS_BurstSafePointToIndex2d(
					position:			new float2{ x = 1557.4f , y = 1521.5f } ,
					gridWorldSize:		new float2{ x = 3000f , y = 3000f } ,
					gridWidth:			1500 ,
					gridHeight:			1500
				);
			}
			[Test] public static void Index1dTo2d_VS_BurstSafePointToIndex2d_CASE03 ()
			{
				Index1dTo2d_VS_BurstSafePointToIndex2d(
					position:			new float2{ x = -1499f , y = -177f } ,
					gridWorldSize:		new float2{ x = 3000f , y = 3000f } ,
					gridWidth:			1500 ,
					gridHeight:			1500
				);
			}
			static void Index1dTo2d_VS_BurstSafePointToIndex2d ( float2 position , float2 gridWorldSize , int gridWidth , int gridHeight )
			{
				int Ai = NativeGrid.PointToIndex( position , gridWorldSize , gridWidth , gridHeight );
				int2 Ai2 = NativeGrid.Index1dTo2d( Ai , gridWidth );
				int2 Bi2 = NativeGrid.PointToIndex2d( position , gridWorldSize , gridWidth , gridHeight );
				Assert.AreEqual( Ai2 , Bi2 , $"\tNativeGrid.Index1dTo2d(NativeGrid.PointToIndex(params)) returned: {Ai2}\n\tNativeGrid.PointToIndex2d returned: {Bi2}" );
			}
		}

	}
}
