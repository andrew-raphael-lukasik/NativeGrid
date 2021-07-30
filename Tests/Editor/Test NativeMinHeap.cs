using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

static class NATIVE_MIN_HEAP
{
	static class INT
	{
		[Test] public static void Peek_01 ()
		{
			var minHeap = new NativeMinHeap<int,IntComparer>( default(IntComparer) , Allocator.Temp , 1 );
			minHeap.Push(3);
			minHeap.Push(10);
			minHeap.Push(7);
			minHeap.Push(14);
			minHeap.Push(1);
			minHeap.Push(5);
			minHeap.Push(2);
			minHeap.Push(22);
			
			Debug.Log( $"min-heap:\t{minHeap.ToString()}" );
			Debug.Log( $"Peek():\t{minHeap.Peek()}" );

			Assert.AreEqual( minHeap.Pop() , 1 );
		}

		[Test] public static void Peek_02 ()
		{
			var minHeap = new NativeMinHeap<int,IntComparer>( default(IntComparer) , Allocator.Temp , 1 );
			minHeap.Push(33);
			minHeap.Push(22);
			minHeap.Push(11);
			minHeap.Push(14);
			
			Debug.Log( $"min-heap:\t{minHeap.ToString()}" );
			Debug.Log( $"Peek():\t{minHeap.Peek()}" );

			Assert.AreEqual( minHeap.Pop() , 11 );
		}

		[Test] public static void Peek_03 ()
		{
			var minHeap = new NativeMinHeap<int,IntComparer>( default(IntComparer) , Allocator.Temp , 1 );
			minHeap.Push(3);
			minHeap.Push(1);
			minHeap.Push(-1);
			minHeap.Push(2);
			
			Debug.Log( $"min-heap:\t{minHeap.ToString()}" );
			Debug.Log( $"Peek():\t{minHeap.Peek()}" );

			Assert.AreEqual( minHeap.Pop() , -1 );
		}

		struct IntComparer : IComparer<int> { int IComparer<int>.Compare ( int lhs , int rhs ) => lhs.CompareTo(rhs); }

	}

	static class ASTAR_JOB_COMPARER
	{

		[Test] public static void Peek_01 ()
		{
			var weights = new NativeArray<half>( 4 , Allocator.Temp );
			weights[0] = (half) 10;
			weights[1] = (half) 20;
			weights[2] = (half) 30;
			weights[3] = (half) 5;
			var comparer = new NativeGrid.AStarJobComparer( weights , weights.Length/2 );
			var minHeap = new NativeMinHeap<int2,NativeGrid.AStarJobComparer>( comparer , Allocator.Temp , weights.Length );
			minHeap.Push( new int2(0,0) );
			minHeap.Push( new int2(0,1) );
			minHeap.Push( new int2(1,0) );
			minHeap.Push( new int2(1,1) );
			
			Debug.Log( $"weights:\t{weights.ToString()}" );
			Debug.Log( $"min-heap:\t{minHeap.ToString()}" );
			Debug.Log( $"Peek():\t{minHeap.Peek()}" );

			Assert.AreEqual( minHeap.Pop() , new int2(1,1) );
		}

		[Test] public static void Peek_02 ()
		{
			var weights = new NativeArray<half>( 4 , Allocator.Temp );
			weights[0] = (half) 11;
			weights[1] = (half) 111;
			weights[2] = (half) 222;
			weights[3] = (half) 333;
			var comparer = new NativeGrid.AStarJobComparer( weights , weights.Length/2 );
			var minHeap = new NativeMinHeap<int2,NativeGrid.AStarJobComparer>( comparer , Allocator.Temp , weights.Length );
			minHeap.Push( new int2(0,0) );
			minHeap.Push( new int2(0,1) );
			minHeap.Push( new int2(1,0) );
			minHeap.Push( new int2(1,1) );
			
			Debug.Log( $"weights:\t{weights.ToString()}" );
			Debug.Log( $"min-heap:\t{minHeap.ToString()}" );
			Debug.Log( $"Peek():\t{minHeap.Peek()}" );

			Assert.AreEqual( minHeap.Pop() , new int2(0,0) );
		}

		[Test] public static void Peek_03 ()
		{
			var weights = new NativeArray<half>( 4 , Allocator.Temp );
			weights[0] = (half) 0.5;
			weights[1] = (half) 0.1;
			weights[2] = (half) 0.2;
			weights[3] = (half) 0.3;
			var comparer = new NativeGrid.AStarJobComparer( weights , weights.Length/2 );
			var minHeap = new NativeMinHeap<int2,NativeGrid.AStarJobComparer>( comparer , Allocator.Temp , weights.Length );
			minHeap.Push( new int2(0,0) );
			minHeap.Push( new int2(0,1) );
			minHeap.Push( new int2(1,0) );
			minHeap.Push( new int2(1,1) );
			
			Debug.Log( $"weights:\t{weights.ToString()}" );
			Debug.Log( $"min-heap:\t{minHeap.ToString()}" );
			Debug.Log( $"Peek():\t{minHeap.Peek()}" );

			Assert.AreEqual( minHeap.Pop() , NativeGrid.Index1dTo2d(1,weights.Length/2) );
		}

		[Test] public static void Peek_04 ()
		{
			var weights = new NativeArray<half>( 4 , Allocator.Temp );
			weights[0] = (half) 50;
			weights[1] = (half) 1;
			weights[2] = (half) (-0.1);
			weights[3] = (half) 300;
			var comparer = new NativeGrid.AStarJobComparer( weights , weights.Length/2 );
			var minHeap = new NativeMinHeap<int2,NativeGrid.AStarJobComparer>( comparer , Allocator.Temp , weights.Length );
			minHeap.Push( new int2(0,0) );
			minHeap.Push( new int2(0,1) );
			minHeap.Push( new int2(1,0) );
			minHeap.Push( new int2(1,1) );
			
			Debug.Log( $"weights:\t{weights.ToString()}" );
			Debug.Log( $"min-heap:\t{minHeap.ToString()}" );
			Debug.Log( $"Peek():\t{minHeap.Peek()}" );

			Assert.AreEqual( minHeap.Pop() , NativeGrid.Index1dTo2d(2,weights.Length/2) );
		}

	}

}
