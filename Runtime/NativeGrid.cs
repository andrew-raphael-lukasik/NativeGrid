/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using Unity.Mathematics;
using Unity.Collections;
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
		#region FIELDS & PROPERTIES


		/// <summary> Internal 1d data array </summary>
		public NativeArray<T> Array => _array;
		[System.Obsolete("Renamed to 'Array'")] public NativeArray<T> Values => Array;
		protected NativeArray<T> _array;

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
			this._array = new NativeArray<T>( width * height , allocator );
			this.Width = width;
			this.Height = height;
			this.Length = width * height;
		}
		public NativeGrid ( int width , int height , NativeArray<T> nativeArray )
		{
			this._array = nativeArray;
			this.Width = width;
			this.Height = height;
			this.Length = width * height;
		}
		public NativeGrid ( int width , int height )
			: this( width:width , height:height , allocator:Allocator.Persistent )
			{}

		#region factory pattern
		public static NativeGrid<T> Factory ( int width , int height , Allocator allocator ) => new NativeGrid<T>( width , height , allocator );
		public static NativeGrid<T> Factory ( int width , int height , NativeArray<T> nativeArrayToNest ) => new NativeGrid<T>( width , height , nativeArrayToNest );
		#endregion


		#endregion
		#region OPERATORS


		public T this [ int i ]
		{
			get { return _array[i]; }
			set { _array[i] = value; }
		}

		public T this [ int x , int y ]
		{
			get { return _array[Index2dTo1d(x,y)]; }
			set { _array[Index2dTo1d(x,y)] = value; }
		}

		public T this [ INT2 i2 ]
		{
			get { return _array[Index2dTo1d(i2)]; }
			set { _array[Index2dTo1d(i2)] = value; }
		}

		#endregion
	}
}
