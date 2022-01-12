using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace NativeGridNamespace
{
	public interface INativeMinHeapComparer<INDEX,VALUE>
		where INDEX : unmanaged
		where VALUE : unmanaged
	{
		int Compare ( INDEX lhs , INDEX rhs , NativeSlice<VALUE> comparables );
	}

	public struct NativeMinHeap <INDEX,VALUE,COMPARER> : INativeDisposable
		where INDEX : unmanaged, System.IEquatable<INDEX>
		where COMPARER : unmanaged, INativeMinHeapComparer<INDEX,VALUE>
		where VALUE : unmanaged
	{

		NativeList<INDEX> _stack;
		COMPARER _comparer;
		[NativeDisableContainerSafetyRestriction]// oh boi, here comes trouble!
		NativeSlice<VALUE> _comparables;

		public bool IsCreated => _stack.IsCreated;
		public int Length => _stack.Length;
		public int Count => _stack.Length;
		
		public NativeMinHeap ( int capacity , Allocator allocator , COMPARER coparer , NativeSlice<VALUE> comparables )
		{
			this._stack = new NativeList<INDEX>( capacity , allocator );
			this._comparer = coparer;
			this._comparables = comparables;
		}

		public void Push ( INDEX item )
		{
			_stack.Add( item );
			MinHeapifyUp( _stack.Length-1 );
		}
		public INDEX Pop ()
		{
			INDEX removedItem = _stack[0];
			_stack.RemoveAtSwapBack(0);
			MinHeapifyDown( 0 );
			return removedItem;
		}

		public INDEX Peek () => _stack[0];
		public void Clear () => _stack.Clear();

		void MinHeapifyUp ( int childIndex )
		{
			if( childIndex==0 ) return;
			int parentIndex = (childIndex-1)/2;
			INDEX childVal = _stack[childIndex];
			INDEX parentVal = _stack[parentIndex];
			if( _comparer.Compare(childVal,parentVal,_comparables)<0 )
			{
				// swap the parent and the child
				_stack[childIndex] = parentVal;
				_stack[parentIndex] = childVal;
				MinHeapifyUp( parentIndex );
			}
		}

		void MinHeapifyDown ( int index )
		{
			int leftChildIndex = index * 2 + 1;
			int rightChildIndex = index * 2 + 2;
			int smallestItemIndex = index;// The index of the parent
			if(
				leftChildIndex<=this._stack.Length-1
				&& _comparer.Compare(_stack[leftChildIndex],_stack[smallestItemIndex],_comparables)<0 )
			{
				smallestItemIndex = leftChildIndex;
			}
			if(
				rightChildIndex<=this._stack.Length-1
				&& _comparer.Compare(_stack[rightChildIndex],_stack[smallestItemIndex],_comparables)<0 )
			{
				smallestItemIndex = rightChildIndex;
			}
			if( smallestItemIndex!=index )
			{
				// swap the parent with the smallest of the child items
				INDEX temp = _stack[index];
				_stack[index] = _stack[smallestItemIndex];
				_stack[smallestItemIndex] = temp;
				MinHeapifyDown( smallestItemIndex );
			}
		}

		public int Parent ( int key ) => (key-1)/2;
		public int Left ( int key ) => 2*key + 1;
		public int Right ( int key ) => 2*key + 2;

		public NativeArray<INDEX> AsArray () => _stack.AsArray();

		public void Dispose ()
		{
			if( _stack.IsCreated ) _stack.Dispose();
		}
		public JobHandle Dispose ( JobHandle inputDeps) => _stack.Dispose( inputDeps );

		public override string ToString ()
		{
			var sb = new System.Text.StringBuilder("{ ");
			var array = _stack.AsArray().ToArray();
			if( array.Length!=0 )
			{
				sb.Append($"{array[0]}");
				for( int i=1 ; i<array.Length ; i++ )
					sb.Append($" , {array[i]}");
			}
			sb.Append(" }");
			return sb.ToString();
		}

	}
}
