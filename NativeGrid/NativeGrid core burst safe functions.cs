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
    #region BURST SAFE


    /// <summary>
    /// Methods safely accessible from within Burst-compiled code (no assertions tho)
    /// </summary>
    [Unity.Burst.BurstCompile]
    public abstract partial class BurstSafe
    {
        public static int2 Index1dTo2d ( int i , int width ) => new int2{ x=i%width , y=i/width };
        public static int Index2dTo1d ( int x , int y , int width ) => y * width + x;
        public static int Index2dTo1d ( INT2 i2 , int width ) => Index2dTo1d( i2.x , i2.y , width );
        public static int ClampIndex ( int i , int length ) => math.clamp( i , 0 , length-1 );
        public static int2 ClampIndex2d ( int x , int y , int width , int height ) => math.clamp( new int2{ x=x , y=y } , int2.zero , new int2{ x=width-1 , y=height-1 } );
        public static int2 ClampIndex2d ( INT2 i2 , int width , int height ) => ClampIndex2d( i2.x , i2.y , width , height );
        public static int IndexTranslate ( RectInt r , int rx , int ry , int R_width ) => Index2dTo1d( r.x+rx , r.y+ry , R_width );
        public static int2 IndexTranslate ( RectInt r , int2 rxy ) => new int2{ x=r.x , y=r.y } + rxy;
        public static int IndexTranslate ( RectInt r , int ri , int R_width )
        {
            int2 ri2d = Index1dTo2d( ri , r.width );
            return IndexTranslate( r , ri2d.x , ri2d.y , R_width );
        }
        public static bool IsIndex2dValid ( int x , int y , int w , int h ) => x>=0 && x<w && y>=0 && y<h;
        public static bool IsIndex2dValid ( INT2 i2 , int w , int h ) => IsIndex2dValid( i2.x , i2.y , w , h );
        public static bool IsIndex1dValid ( int i , int len ) => 0>=0 && i<len;
        public static float2 Index2dToPoint ( int x , int y , float stepX , float stepY ) => new float2{ x=(float)x*stepX , y=(float)y*stepY };
        public static float2 Index2dToPoint ( INT2 i2 , float stepX , float stepY ) => Index2dToPoint( i2.x , i2.y , stepX , stepY );
        public static float2 Index2dToPoint ( INT2 i2 , FLOAT2 step ) => Index2dToPoint( i2.x , i2.y , step.x , step.y );
        public static T PointToValue <T> ( FLOAT2 point , FLOAT2 worldSize , NativeArray<T> array , int width , int height ) where T : unmanaged
        {
            return array[ BurstSafe.PointToIndex( point , worldSize , width , height ) ];
        }
        public static int PointToIndex ( FLOAT2 point , FLOAT2 worldSize , int width , int height )
        {
            int2 xy = BurstSafe.PointToIndex2d( point , worldSize , width , height );
            return BurstSafe.Index2dTo1d( xy , width );
        }
        public static int2 PointToIndex2d ( FLOAT2 point , FLOAT2 worldSize , int width , int height )
        {
            BurstSafe.GetPositionInsideCell( point , new int2{ x=width , y=height } , worldSize , out int2 lo , out int2 hi , out float2 f );
            int2 index = new int2{
                x = BurstSafe.AboutEqual( f.x , 1f ) ? hi.x : lo.x ,
                y = BurstSafe.AboutEqual( f.y , 1f ) ? hi.y : lo.y
            };
            return math.clamp( index , 0 , new int2{ x=width-1 , y=height-1 } );
        }
        public static void GetPositionInsideCell
        (
            float point , int numCells , float worldSize ,
            out int lowerCell , out int upperCell , out float normalizedPositionBetweenThoseTwoPoints
        )
        {
            float cellSize = worldSize / (float)numCells;
            float cellFraction = point / cellSize;
            lowerCell = cellSize<0f ? (int)math.ceil( cellFraction ) : (int)math.floor( cellFraction );
            upperCell = lowerCell<0 ? lowerCell-1 : lowerCell+1;
            normalizedPositionBetweenThoseTwoPoints = ( point - (float)lowerCell*cellSize ) / cellSize;
        }
        public static void GetPositionInsideCell
        (
            float2 point , int2 numCells , float2 worldSize ,
            out int2 lowerCell , out int2 upperCell , out float2 normalizedPositionBetweenThoseTwoPoints
        )
        {
            BurstSafe.GetPositionInsideCell( point.x , numCells.x , worldSize.x , out int xlo , out int xhi , out float xf );
            BurstSafe.GetPositionInsideCell( point.y , numCells.y , worldSize.y , out int ylo , out int yhi , out float yf );
            lowerCell = new int2{ x=xlo , y=ylo };
            upperCell = new int2{ x=xhi , y=yhi };
            normalizedPositionBetweenThoseTwoPoints = new float2{ x=xf , y=yf };
        }

        /// <returns>
        /// Result other than 0 means that this point lies directly on a border between two neighbouring cells.
        /// And thus it must be determined to which of the cells this point will fall into. This method provides an index offset to solve just that.
        /// </returns>
        public static int IsPointBetweenCells ( float point , int width , float worldSize )
        {
            float cellSize = worldSize / (float)width;
            float cellFraction = (point%cellSize) / cellSize;
            bool isMiddlePoint = BurstSafe.AboutEqual( cellFraction , 1f );
            return isMiddlePoint ? (point<0f?-1:1) : 0;
        } 

        public static bool AboutEqual ( double x , double y ) => math.abs(x-y) <= math.max( math.abs(x) , math.abs(y) ) * 1E-15;

        /// <summary> System.Math.Round( value, System.MidpointRounding.AwayFromZero ) equivalent </summary>
        public static int MidpointRoundingAwayFromZero ( float value ) => (int)( value + (value<0f ? -0.5f : 0.5f) );
        /// <summary> System.Math.Round( value, System.MidpointRounding.AwayFromZero ) equivalent </summary>
        public static float MidpointRoundingAwayFromZero ( float value , float step ) => (float)BurstSafe.MidpointRoundingAwayFromZero(value/step) * step;
        /// <summary> System.Math.Round( value , System.MidpointRounding.AwayFromZero ) equivalent </summary>
        public static int2 MidpointRoundingAwayFromZero ( FLOAT2 value ) => new int2{ x=BurstSafe.MidpointRoundingAwayFromZero(value.x) , y=BurstSafe.MidpointRoundingAwayFromZero(value.y) };
        /// <summary> System.Math.Round( value, System.MidpointRounding.AwayFromZero ) equivalent </summary>
        public static float2 MidpointRoundingAwayFromZero ( FLOAT2 value , FLOAT2 step ) => (float2)BurstSafe.MidpointRoundingAwayFromZero(value/step) * (float2)step;
    }

    #endregion
}
