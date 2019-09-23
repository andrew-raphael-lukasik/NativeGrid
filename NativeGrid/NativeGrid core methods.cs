/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;


/// <summary>
/// NativeGrid<STRUCT> is grid data layout class. Parent NativeGrid class is for static functions and nested types.
/// </summary>
public partial class NativeGrid <STRUCT>
	: NativeGrid, System.IDisposable
	where STRUCT : unmanaged
{
	#region index transformation methods


	/// <summary> Converts index 2d to 1d equivalent </summary>
	public int Index2dTo1d ( int x , int y )
	{
		#if DEBUG
		if( IsIndex2dValid(x,y)==false ) { Debug.LogWarningFormat( "[{0},{1}] index is invalid for this grid" , x , y ); }
		#endif
		return y * Width + x;
	}
	public int Index2dTo1d ( INT2 index2d ) => Index2dTo1d( index2d.x , index2d.y );
	public int Index2dTo1d ( int2 index2d ) => Index2dTo1d( index2d.x , index2d.y );

	
	/// <summary> Converts 1d to 2d array index </summary>
	public int2 Index1dTo2d ( int i ) => new int2 { x=i%Width , y=i/Width };

	
	/// <summary> Transforms local position to cell index </summary>
	public bool LocalPointToIndex2d ( FLOAT3 localPoint , float spacing , out int2 result )
	{
		int x = (int)( ( localPoint.x+(float)Width*0.5f*spacing )/spacing );
		int z = (int)( ( localPoint.z+(float)Height*0.5f*spacing )/spacing );
		if( IsIndex2dValid(x,z) )
		{
			result = new int2{ x=x , y=z };
			return true;
		} else {
			result = new int2{ x=-1 , y=-1 };
			return false;
		}
	}


	/// <summary> Determines whether index 2d is inside array bounds </summary>
	public bool IsIndex2dValid ( int x , int y ) => IsIndex2dValid( x , y , Width , Height );
	public bool IsIndex2dValid ( INT2 index2d ) => IsIndex2dValid( index2d.x , index2d.y );


	/// <summary> Determines whether index 1d is inside array bounds </summary>
	public bool IsIndex1dValid ( int i ) => IsIndex1dValid( i , this.Length );


	/// <summary> Transforms index to local position. </summary>
	public float3 IndexToLocalPoint ( int x , int y , float spacing )
	{
		return new float3(
			( (float)x*spacing )+( -Width*spacing*0.5f )+( spacing*0.5f ) ,
			0f ,
			( (float)y*spacing )+( -Height*spacing*0.5f )+( spacing*0.5f )
		);
	}
	public float3 IndexToLocalPoint ( int index1d , float spacing )
	{
		int2 index2d = Index1dTo2d( index1d );
		return new float3(
			( index2d.x*spacing )+( -Width*spacing*0.5f )+( spacing*0.5f ) ,
			0f ,
			( index2d.y*spacing )+( -Height*spacing*0.5f )+( spacing*0.5f )
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
	public unsafe ref STRUCT AsRef ( int x , int y ) => ref AsRef( BurstSafe.Index2dTo1d( x , y , this.Width ) );
	public unsafe ref STRUCT AsRef ( INT2 i2 ) => ref AsRef( BurstSafe.Index2dTo1d( i2 , this.Width ) );
	public unsafe ref STRUCT AsRef ( int i ) => ref ( (STRUCT*)_array.GetUnsafePtr() )[i];


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
	public byte GetMarchingSquares ( INT2 index2d , System.Predicate<STRUCT> predicate )
	{
		int x = index2d.x;
		int y = index2d.y;

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
	public JobHandle Fill ( STRUCT value , JobHandle dependency = default(JobHandle) )
	{
		var job = new FillJob<STRUCT>( array:_array , value:value );
		return Dependency = job.Schedule(
			_array.Length , 1024 ,
			JobHandle.CombineDependencies( dependency , Dependency )
		);
	}

	/// <summary> Fill rectangle </summary>
	public JobHandle Fill ( RectInt region , STRUCT value , JobHandle dependency = default(JobHandle) )
	{
		var job = new FillRegionJob<STRUCT>( region:region , array:this._array , value:value , array_width:this.Width );
		return Dependency = job.Schedule(
			region.width*region.height , 1024 ,
			JobHandle.CombineDependencies( dependency , Dependency )
		);
	}

	
	/// <summary> Fills grid border cells. </summary>
	public JobHandle FillBorders ( STRUCT fill , JobHandle dependency = default(JobHandle) )
	{
		var job = new FillBordersJob<STRUCT>( array:_array , width:Width , height:Height , fill:fill );
		return Dependency = job.Schedule( JobHandle.CombineDependencies(dependency,Dependency) );
	}


	#endregion
	#region copy methods


	public JobHandle Copy ( RectInt region , out NativeGrid<STRUCT> copy ) => Copy( this , region , out copy );


	#endregion
	#region deallocation methods


	public void Dispose () => _array.Dispose();


	#endregion
}
