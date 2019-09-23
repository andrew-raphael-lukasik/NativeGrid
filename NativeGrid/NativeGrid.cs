/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;


/// <summary>
/// NativeGrid<STRUCT> is grid data layout class. Parent NativeGrid class is for static functions and nested types.
/// </summary>
public partial class NativeGrid <STRUCT>
	: NativeGrid, System.IDisposable
	where STRUCT : unmanaged
{
	#region FIELDS & PROPERTIES


	/// <summary> Internal 1d data array </summary>
	public NativeArray<STRUCT> Array => _array;
	[System.Obsolete("Renamed to 'Array'")] public NativeArray<STRUCT> Values => Array;
	protected NativeArray<STRUCT> _array;

	public readonly int Width;
	public readonly int Height;
	public readonly int Length;
	public bool IsCreated => _array.IsCreated;
	public JobHandle Dependency = default(JobHandle);

	[System.Obsolete("Renamed to: 'Dependency'")] public JobHandle WriteAccess { get => Dependency; set => Dependency = value; }


	#endregion
	#region CONSTRUCTORS

	public NativeGrid ( int width , int height , Allocator allocator )
	{
		this._array = new NativeArray<STRUCT>( width * height , allocator );
		this.Width = width;
		this.Height = height;
		this.Length = width * height;
	}
	public NativeGrid ( int width , int height , NativeArray<STRUCT> nativeArray )
	{
		this._array = nativeArray;
		this.Width = width;
		this.Height = height;
		this.Length = width * height;
	}

	#region factory pattern
	public static NativeGrid<STRUCT> Factory ( int width , int height , Allocator allocator ) => new NativeGrid<STRUCT>( width , height , allocator );
	public static NativeGrid<STRUCT> Factory ( int width , int height , NativeArray<STRUCT> nativeArrayToNest ) => new NativeGrid<STRUCT>( width , height , nativeArrayToNest );
	#endregion


	#endregion
	#region OPERATORS


	public STRUCT this [ int i ]
	{
		get { return _array[i]; }
		set { _array[i] = value; }
	}

	public STRUCT this [ int x , int y ]
	{
		get { return _array[Index2dTo1d(x,y)]; }
		set { _array[Index2dTo1d(x,y)] = value; }
	}

	public STRUCT this [ INT2 i2 ]
	{
		get { return _array[Index2dTo1d(i2)]; }
		set { _array[Index2dTo1d(i2)] = value; }
	}

	public STRUCT this [ INT2 innerSegmentIndex , int segment ]
	{
		get { return _array[ GetSegmentedIndex(segment,innerSegmentIndex) ]; }
		set { _array[ GetSegmentedIndex(segment,innerSegmentIndex) ] = value; }
	}


	#endregion
}
