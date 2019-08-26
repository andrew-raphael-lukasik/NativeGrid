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

    void MinHeapifyUp ( int index )
    {
        if( index==0 ) return;

        int childIndex = index;
        int parentIndex = (index-1)/2;

        if( _comparer->Compare( _stack[childIndex] , _stack[parentIndex])<0 )
        {
            // swap the parent and the child
            VALUE temp = _stack[childIndex];
            _stack[childIndex] = _stack[parentIndex];
            _stack[parentIndex] = temp;

            MinHeapifyUp( parentIndex );
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
