/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace NativeGridNamespace
{
	/// <summary> Non-generic, abstract parent class for NativeGrid<T>. </summary>
	public abstract partial class NativeGrid
	{
		#region PUBLIC METHODS


		/// <summary> Converts int index to int2 coord </summary>
		public static int2 IndexToCoord ( INT i , INT width )
		{
			Assert_IndexToCoord( i , width );

			return new int2{ x=i%width , y=i/width };
		}
		

		/// <summary> Converts coord to it's index equivalent </summary>
		public static int CoordToIndex ( INT x , INT y , INT width )
		{
			Assert_CoordToIndex( x , y , width );

			return y * width + x;
		}
		public static int CoordToIndex ( INT2 i2 , INT width ) => CoordToIndex( i2.x , i2.y , width );


		/// <summary> Translate regional coordinate to outer array index </summary>
		/// <param name="R">Outer RectInt</param>
		/// <param name="r">Inner, smaller RectInt</param>
		/// <param name="rx">Inner x coordinate</param>
		/// <param name="ry">Inner y coordinate</param>
		/// <param name="R_width">Outer RectInt.width</param>
		public static int IndexTranslate ( RectInt r , INT rx , INT ry , INT R_width )
		{
			Assert_IndexTranslate( r , rx , ry , R_width );

			return CoordToIndex( r.x+rx , r.y+ry , R_width );
		}

		/// <summary> Translate regional coordinate to outer array index </summary>
		/// <param name="R">Outer RectInt</param>
		/// <param name="r">Inner, smaller RectInt</param>
		/// <param name="rx">Inner x coordinate</param>
		/// <param name="ry">Inner y coordinate</param>
		/// <param name="R_width">Outer RectInt.width</param>
		public static int2 IndexTranslate ( RectInt r , INT2 rxy )
		{
			Assert_IndexTranslate( r , rxy.x , rxy.y );

			return new int2{ x=r.x , y=r.y } + (int2)rxy;
		}

		/// <summary> Translate regional index to outer one </summary>
		/// <param name="R">Outer RectInt</param>
		/// <param name="r">Inner, smaller RectInt</param>
		/// <param name="ri">Index in inner rect</param>
		/// <param name="R_width">Outer RectInt.width</param>
		public static int IndexTranslate ( RectInt r , INT ri , INT R_width )
		{
			int2 ri2d = IndexToCoord( ri , r.width );
			return IndexTranslate( r , ri2d.x , ri2d.y , R_width );
		}


		/// <summary> Determines whether int2 coord is inside array bounds </summary>
		public static bool IsCoordValid ( INT x , INT y , INT w , INT h ) => x>=0 && x<w && y>=0 && y<h;
		public static bool IsCoordValid ( INT2 i2 , INT w , INT h ) => IsCoordValid( i2.x , i2.y , w , h );


		/// <summary> Determines whether index is inside array bounds </summary>
		public static bool IsIndexValid ( INT i , INT len ) => 0>=0 && i<len;


		/// <summary> Point from a coordinate </summary>
		public static float2 CoordToPoint ( INT x , INT y , FLOAT stepX , FLOAT stepY ) => new float2{ x=(float)x*stepX , y=(float)y*stepY };
		public static float2 CoordToPoint ( INT2 i2 , FLOAT stepX , FLOAT stepY ) => CoordToPoint( i2.x , i2.y , stepX , stepY );
		public static float2 CoordToPoint ( INT2 i2 , FLOAT2 step ) => CoordToPoint( i2.x , i2.y , step.x , step.y );


		/// <summary> Value at point </summary>
		public static T PointToValue <T> ( FLOAT2 point , FLOAT2 worldSize , NativeArray<T> array , INT width , INT height ) where T : unmanaged
		{
			return array[ PointToIndex( point , worldSize , width , height ) ];
		}


		/// <summary> Index from point </summary>
		public static int PointToIndex ( FLOAT2 point , FLOAT2 worldSize , INT width , INT height )
		{
			int2 coord = PointToCoord( point , worldSize , width , height );
			return CoordToIndex( coord , width );
		}
		public static int2 PointToCoord ( FLOAT2 point , FLOAT2 worldSize , INT width , INT height )
		{
			GetPositionInsideCell( point , new int2{ x=width , y=height } , worldSize , out int2 lo , out int2 hi , out float2 f );
			int2 index = new int2{
				x = AboutEqual( f.x , 1f ) ? hi.x : lo.x ,
				y = AboutEqual( f.y , 1f ) ? hi.y : lo.y
			};
			return math.clamp( index , 0 , new int2{ x=width-1 , y=height-1 } );
		}


		/// <summary>  </summary>
		public static void GetPositionInsideCell
		(
			FLOAT point , INT numCells , FLOAT worldSize ,
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
			FLOAT2 point , INT2 numCells , FLOAT2 worldSize ,
			out int2 lowerCell , out int2 upperCell , out float2 normalizedPositionBetweenThoseTwoPoints
		)
		{
			GetPositionInsideCell( point.x , numCells.x , worldSize.x , out int xlo , out int xhi , out float xf );
			GetPositionInsideCell( point.y , numCells.y , worldSize.y , out int ylo , out int yhi , out float yf );
			lowerCell = new int2{ x=xlo , y=ylo };
			upperCell = new int2{ x=xhi , y=yhi };
			normalizedPositionBetweenThoseTwoPoints = new float2{ x=xf , y=yf };
		}


		/// <returns>
		/// Result other than 0 means that this point lies directly on a border between two neighbouring cells.
		/// And thus it must be determined to which of the cells this point will fall into. This method provides an index offset to solve just that.
		/// </returns>
		public static int IsPointBetweenCells ( FLOAT point , INT width , FLOAT worldSize )
		{
			float cellSize = worldSize / (float)width;
			float cellFraction = (point%cellSize) / cellSize;
			bool isMiddlePoint = AboutEqual( cellFraction , 1f );
			return isMiddlePoint ? (point<0f?-1:1) : 0;
		}

		public static bool AboutEqual ( double x , double y ) => math.abs(x-y) <= math.max( math.abs(x) , math.abs(y) ) * 1E-15;


		public static int ClampIndex ( INT i , INT length ) => math.clamp( i , 0 , length-1 );
		public static int2 ClampCoord ( INT x , INT y , INT width , INT height ) => math.clamp( new int2{ x=x , y=y } , int2.zero , new int2{ x=width-1 , y=height-1 } );
		public static int2 ClampCoord ( INT2 i2 , INT width , INT height ) => ClampCoord( i2.x , i2.y , width , height );
		

		/// <summary> System.Math.Round( value, System.MidpointRounding.AwayFromZero ) equivalent </summary>
		public static int MidpointRoundingAwayFromZero ( FLOAT value ) => (int)( value + (value<0f ? -0.5f : 0.5f) );
		/// <summary> System.Math.Round( value, System.MidpointRounding.AwayFromZero ) equivalent </summary>
		public static float MidpointRoundingAwayFromZero ( FLOAT value , FLOAT step ) => (float)MidpointRoundingAwayFromZero(value/step) * step;
		/// <summary> System.Math.Round( value , System.MidpointRounding.AwayFromZero ) equivalent </summary>
		public static int2 MidpointRoundingAwayFromZero ( FLOAT2 value ) => new int2{ x=MidpointRoundingAwayFromZero(value.x) , y=MidpointRoundingAwayFromZero(value.y) };
		/// <summary> System.Math.Round( value, System.MidpointRounding.AwayFromZero ) equivalent </summary>
		public static float2 MidpointRoundingAwayFromZero ( FLOAT2 value , FLOAT2 step ) => (float2)MidpointRoundingAwayFromZero(value/step) * (float2)step;
		

		#endregion
	}
}
