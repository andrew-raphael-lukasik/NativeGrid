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


	[UnityEngine.SerializeField] protected NativeArray<STRUCT> _values;
	/// <summary> Internal 1d data array </summary>
	public NativeArray<STRUCT> Values => _values;

	public readonly int Width;
	public readonly int Height;
	public readonly int Length;
	public bool IsCreated => _values.IsCreated;
	public JobHandle Dependency = default(JobHandle);

    [System.Obsolete("Rename to: 'Dependency'")] public JobHandle WriteAccess { get => Dependency; set => Dependency = value; }


	#endregion
	#region CONSTRUCTORS


	public NativeGrid ( int width , int height , Allocator allocator )
	{
		this._values = new NativeArray<STRUCT>( width * height , allocator );
		this.Width = width;
		this.Height = height;
		this.Length = width * height;
	}
	public NativeGrid ( int width , int height , NativeArray<STRUCT> nativeArray )
	{
		this._values = nativeArray;
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
		get { Dependency.Complete(); return _values[i]; }
		set { Dependency.Complete(); _values[i] = value; }
	}

	public STRUCT this [ int x , int y ]
	{
		get { Dependency.Complete(); return _values[Index2dTo1d(x,y)]; }
		set { Dependency.Complete(); _values[Index2dTo1d(x,y)] = value; }
	}

	public STRUCT this [ INT2 i2 ]
	{
		get { Dependency.Complete(); return _values[Index2dTo1d(i2)]; }
		set { Dependency.Complete(); _values[Index2dTo1d(i2)] = value; }
	}


	#endregion
}
