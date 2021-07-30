using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public unsafe struct NativeStack <VALUE>
	: System.IDisposable
	where VALUE : unmanaged
{

	[NativeDisableUnsafePtrRestriction] internal VALUE* _ptr;
	internal int _capacity;
	internal Allocator _allocator;
	internal readonly int _sizeOfValue;
	internal readonly int _alignOfValue;

	internal int _lastItemIndex;
	public int LastItemIndex => _lastItemIndex;
	public int Length => _lastItemIndex + 1;

	public NativeStack ( int capacity , Allocator allocator  )
	{
		_sizeOfValue = UnsafeUtility.SizeOf<VALUE>();
		_alignOfValue = UnsafeUtility.AlignOf<VALUE>();
		_allocator = allocator;
		_capacity = capacity;
		_ptr = (VALUE*)UnsafeUtility.Malloc( _sizeOfValue * _capacity , _alignOfValue , _allocator );
		_lastItemIndex = -1;
	}

	public VALUE this [ int i ]
	{
		get
		{
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if( i<0 || i>_lastItemIndex ) throw new System.IndexOutOfRangeException($"Index {i} is out of range for buffer capacity of {_capacity}");
			#endif
			
			return UnsafeUtility.ReadArrayElement<VALUE>( _ptr , i );
		}
		set
		{
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if( i<0 || i>_lastItemIndex ) throw new System.IndexOutOfRangeException($"Index {i} is out of range for buffer capacity of {_capacity}");
			#endif

			UnsafeUtility.WriteArrayElement( _ptr , i , value );
		}
	}
	
	public void Push ( VALUE item )
	{
		_lastItemIndex++;
		this[_lastItemIndex] = item;
	}

	public VALUE Pop ()
	{
		#if ENABLE_UNITY_COLLECTIONS_CHECKS
		if( _lastItemIndex==-1 ) throw new System.InvalidOperationException("The heap is empty");
		#endif

		VALUE removedItem = this[0];
		this[0] = this[_lastItemIndex];
		_lastItemIndex--;

		return removedItem;
	}

	public VALUE Peek ()
	{
		#if ENABLE_UNITY_COLLECTIONS_CHECKS
		if( _lastItemIndex==-1 ) throw new System.InvalidOperationException("The heap is empty");
		#endif

		return this[0];
	}

	public void Clear () => _lastItemIndex = -1;

	public void Dispose ()
	{
		UnsafeUtility.Free( _ptr , _allocator );
		_ptr = null;
		_lastItemIndex = -1;
	}

	/// <returns>A NativeArray "view" of the data.</returns>
	public NativeArray<VALUE> AsArray ()
	{
		var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<VALUE>( this._ptr , this.Length , Allocator.None );
		#if ENABLE_UNITY_COLLECTIONS_CHECKS
		NativeArrayUnsafeUtility.SetAtomicSafetyHandle( ref nativeArray , AtomicSafetyHandle.Create() );
		#endif
		return nativeArray;
	}

}
