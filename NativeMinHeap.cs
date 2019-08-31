using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


public unsafe struct NativeMinHeap <VALUE,COMPARER>
    : System.IDisposable
    where VALUE : unmanaged
    where COMPARER : unmanaged, System.Collections.Generic.IComparer<VALUE>
{

    readonly Allocator _allocator;
    NativeStack<VALUE> _stack;
    public int Count => _stack.Count;

    [NativeDisableUnsafePtrRestriction]
    COMPARER* _comparer;
    

    public NativeMinHeap ( COMPARER comparer , Allocator allocator , int capacity )
    {
        _allocator = allocator;
        _stack = new NativeStack<VALUE>( capacity , allocator );

        _comparer = (COMPARER*)UnsafeUtility.Malloc( sizeof(COMPARER) , UnsafeUtility.AlignOf<COMPARER>() , allocator );
        UnsafeUtility.CopyStructureToPtr( ref comparer , _comparer );
    }

    public void Push ( VALUE item )
    {
        _stack.Push( item );
        MinHeapifyUp( _stack.LastItemIndex );
    }

    public VALUE Pop ()
    {
        VALUE removedItem = _stack.Pop();
        MinHeapifyDown( 0 );
        return removedItem;
    }

    public VALUE Peek () => _stack.Peek();

    public void Clear () => _stack.Clear();

    void MinHeapifyUp ( int childIndex )
    {
        if( childIndex==0 ) return;

        int parentIndex = (childIndex-1)/2;

        VALUE childVal = _stack[childIndex];
        VALUE parentVal = _stack[parentIndex];

        if( _comparer->Compare(childVal,parentVal)<0 )
        {
            // swap the parent and the child
            _stack[childIndex] = parentVal;
            _stack[parentIndex] = childVal;

            MinHeapifyUp2( parentIndex , childVal );
        }
    }
    /// one memory read less
    void MinHeapifyUp2 ( int childIndex , VALUE childVal )
    {
        if( childIndex==0 ) return;

        int parentIndex = (childIndex-1)/2;

        VALUE parentVal = _stack[parentIndex];

        if( _comparer->Compare(childVal,parentVal)<0 )
        {
            // swap the parent and the child
            _stack[childIndex] = parentVal;
            _stack[parentIndex] = childVal;

            MinHeapifyUp2( parentIndex , parentVal );
        }
    }

    void MinHeapifyDown ( int index )
    {
        int leftChildIndex = index * 2 + 1;
        int rightChildIndex = index * 2 + 2;
        int smallestItemIndex = index;// The index of the parent

        if(
            leftChildIndex<= this._stack.LastItemIndex
            && _comparer->Compare( _stack[leftChildIndex] , _stack[smallestItemIndex])<0 )
        {
            smallestItemIndex = leftChildIndex;
        }

        if(
            rightChildIndex<= this._stack.LastItemIndex
            && _comparer->Compare( _stack[rightChildIndex] , _stack[smallestItemIndex])<0 )
        {
            smallestItemIndex = rightChildIndex;
        }

        if( smallestItemIndex!=index )
        {
            // swap the parent with the smallest of the child items
            VALUE temp = _stack[index];
            _stack[index] = _stack[smallestItemIndex];
            _stack[smallestItemIndex] = temp;
            MinHeapifyDown( smallestItemIndex );
        }
    }

    public void Dispose ()
    {
        _stack.Dispose();
        UnsafeUtility.Free( _comparer , _allocator );
    }

}
