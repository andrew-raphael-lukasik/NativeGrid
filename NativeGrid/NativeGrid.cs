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
	public JobHandle WriteAccess = default(JobHandle);


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


	#endregion
	#region OPERATORS


	public STRUCT this [ int i ]
	{
		get { WriteAccess.Complete(); return _values[i]; }
		set { WriteAccess.Complete(); _values[i] = value; }
	}

	public STRUCT this [ int x , int y ]
	{
		get { WriteAccess.Complete(); return _values[Index2dTo1d(x,y)]; }
		set { WriteAccess.Complete(); _values[Index2dTo1d(x,y)] = value; }
	}

	public STRUCT this [ INT2 i2 ]
	{
		get { WriteAccess.Complete(); return _values[Index2dTo1d(i2)]; }
		set { WriteAccess.Complete(); _values[Index2dTo1d(i2)] = value; }
	}


	#endregion
}
