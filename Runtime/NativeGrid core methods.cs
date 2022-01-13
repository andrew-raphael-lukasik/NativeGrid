/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace NativeGridNamespace
{
	/// <summary>
	/// NativeGrid<T> is grid data layout class. Parent NativeGrid class is for static functions and nested types.
	/// </summary>
	public partial class NativeGrid <T>
		: NativeGrid, System.IDisposable
		where T : unmanaged
	{
		#region index transformation methods


		/// <summary> Converts coord to it's index equivalent </summary>
		public int CoordToIndex ( int x , int y )
		{
			#if DEBUG
			if( IsCoordValid(x,y)==false ) { Debug.LogWarningFormat( "[{0},{1}] index is invalid for this grid" , x , y ); }
			#endif
			return y * Width + x;
		}
		public int CoordToIndex ( INT2 coord ) => CoordToIndex( coord.x , coord.y );
		public int CoordToIndex ( int2 coord ) => CoordToIndex( coord.x , coord.y );

		
		/// <summary> Converts index to coord </summary>
		public int2 IndexToCoord ( int i ) => new int2 { x=i%Width , y=i/Width };

		
		/// <summary> Transforms local position to cell index </summary>
		public bool LocalPointToCoord ( FLOAT3 localPoint , float spacing , out int2 result )
		{
			int x = (int)( ( localPoint.x+(float)Width*0.5f*spacing )/spacing );
			int z = (int)( ( localPoint.z+(float)Height*0.5f*spacing )/spacing );
			if( IsCoordValid(x,z) )
			{
				result = new int2{ x=x , y=z };
				return true;
			} else {
				result = new int2{ x=-1 , y=-1 };
				return false;
			}
		}


		/// <summary> Determines whether coord is inside array bounds </summary>
		public bool IsCoordValid ( int x , int y ) => IsCoordValid( x , y , Width , Height );
		public bool IsCoordValid ( INT2 coord ) => IsCoordValid( coord.x , coord.y );


		/// <summary> Determines whether index is inside array bounds </summary>
		public bool IsIndexValid ( int i ) => IsIndexValid( i , this.Length );


		/// <summary> Transforms index to local position. </summary>
		public float3 IndexToLocalPoint ( int x , int y , float spacing )
		{
			return new float3(
				( (float)x*spacing )+( -Width*spacing*0.5f )+( spacing*0.5f ) ,
				0f ,
				( (float)y*spacing )+( -Height*spacing*0.5f )+( spacing*0.5f )
			);
		}
		public float3 IndexToLocalPoint ( int index , float spacing )
		{
			int2 coord = IndexToCoord( index );
			return new float3(
				( coord.x*spacing )+( -Width*spacing*0.5f )+( spacing*0.5f ) ,
				0f ,
				( coord.y*spacing )+( -Height*spacing*0.5f )+( spacing*0.5f )
			);
		}


		/// <returns> Rect center position </returns>
		public float3 IndexToLocalPoint ( int x , int y , int w , int h , float spacing )
		{
			float3 cornerA = IndexToLocalPoint( x , y , spacing );
			float3 cornerB = IndexToLocalPoint( x+w-1 , y+h-1 , spacing );
			return cornerA+(cornerB-cornerA)*0.5f;
		}

		
		#endregion
		#region ref return methods


		/// <returns> Get ref to array element </returns>
		/// <note> Make sure index is in bound </note>
		public unsafe ref T AsRef ( int x , int y ) => ref AsRef( CoordToIndex( x , y , this.Width ) );
		public unsafe ref T AsRef ( INT2 i2 ) => ref AsRef( CoordToIndex( i2 , this.Width ) );
		public unsafe ref T AsRef ( int i ) => ref ( (T*)_array.GetUnsafePtr() )[i];


		#endregion
		#region marching squares methods


		/// <summary> Gets the surrounding field values </summary>
		/// <returns>
		/// 8-bit clockwise-enumerated bit values 
		///		7 0 1		[x-1,y+1]   [x,y+1]   [x+1,y+1]
		///		6 ^ 2	==	[x-1,y]      [x,y]    [x+1,y]
		///		5 4 3		[x-1,y-1]   [x,y-1]   [x+1,y-1]
		/// for example: 1<<0 is top, 1<<1 is top-right, 1<<2 is right, 1<<6|1<<4|1<<2 is both left,down and right
		/// </returns>
		public byte GetMarchingSquares ( INT2 coord , System.Predicate<T> predicate )
		{
			int x = coord.x;
			int y = coord.y;

			const byte zero = 0b_0000_0000;
			byte result = zero;

			//out of bounds test:
			bool xPlus = x+1 < Width;
			bool yPlus = y+1 < Height;
			bool xMinus = x-1 >= 0;
			bool yMinus = y-1 >= 0;

			//top, down:
			result |= yPlus && predicate(this[x,y+1]) ? (byte)0b_0000_0001 : zero;
			result |= yMinus && predicate(this[x,y-1]) ? (byte)0b_0001_0000 : zero;

			//right side:
			result |= xPlus && yPlus && predicate(this[x+1,y+1]) ? (byte)0b_0000_0010 : zero;
			result |= xPlus && predicate(this[x+1,y]) ? (byte)0b_0000_0100 : zero;
			result |= xPlus && yMinus && predicate(this[x+1,y-1]) ? (byte)0b_0000_1000 : zero;

			//left side:
			result |= xMinus && yPlus && predicate(this[x-1,y+1]) ? (byte)0b_0010_0000 : zero;
			result |= xMinus && predicate(this[x-1,y]) ? (byte)0b_0000_0100 : zero;
			result |= xMinus && yMinus && predicate(this[x-1,y-1]) ? (byte)0b_1000_0000 : zero;
			
			return result;
		}


		#endregion
		#region fill methods


		/// <summary> Fill </summary>
		public JobHandle Fill ( T value , JobHandle dependency = default(JobHandle) )
		{
			var job = new FillJob<T>( array:_array , value:value );
			return Dependency = job.Schedule(
				_array.Length , 1024 ,
				JobHandle.CombineDependencies( dependency , Dependency )
			);
		}

		/// <summary> Fill rectangle </summary>
		public JobHandle Fill ( RectInt region , T value , JobHandle dependency = default(JobHandle) )
		{
			var job = new FillRegionJob<T>( region:region , array:this._array , value:value , array_width:this.Width );
			return Dependency = job.Schedule(
				region.width*region.height , 1024 ,
				JobHandle.CombineDependencies( dependency , Dependency )
			);
		}

		
		/// <summary> Fills grid border cells. </summary>
		public JobHandle FillBorders ( T fill , JobHandle dependency = default(JobHandle) )
		{
			var job = new FillBordersJob<T>( array:_array , width:Width , height:Height , fill:fill );
			return Dependency = job.Schedule( JobHandle.CombineDependencies(dependency,Dependency) );
		}


		#endregion
		#region copy methods


		public JobHandle Copy ( RectInt region , out NativeGrid<T> copy ) => Copy( this , region , out copy );


		#endregion
		#region deallocation methods


		public void Dispose ()
		{
			if( _array.IsCreated )
			{
				Dependency.Complete();
				_array.Dispose();
			}
		}


		#endregion
	}
}