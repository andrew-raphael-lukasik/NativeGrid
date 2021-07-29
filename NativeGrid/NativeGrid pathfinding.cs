/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

using IComparerInt2 = System.Collections.Generic.IComparer<Unity.Mathematics.int2>;
using Debug = UnityEngine.Debug;

/// <summary>
/// Abstract parent class for generic NativeGrid{STRUCT}. To simplify referencing static functions/types from "NativeGrid{byte}.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
/// </summary>
public abstract partial class NativeGrid
{
	#region PUBLIC METHODS


	/// <summary> Traces path using some kind of A* algorithm </summary>
	/// Format weights to 0.0 to 1.0 range. Heuristic can cease to work otherwise.
	[Unity.Burst.BurstCompile]
	public struct AStarJob : IJob, System.IDisposable
	{

		/// <summary> Job results goes here. List of indices to form a path. </summary>
		public NativeList<int2> Results;

		readonly int2 start;
		readonly int2 destination;
		[ReadOnly] readonly NativeArray<byte> moveCost;
		readonly int moveCost_width;
		readonly float heuristic_cost;
		readonly float heuristic_search;

		public NativeArray<float> _F_;
		public NativeArray<int2> solution;
		NativeMinHeap<int2,AStarJobComparer> frontier;
		public NativeHashMap<int2,byte> visited;
		NativeList<int2> neighbours;
		public int _step_limit;

		/// <summary> Traces path using some kind of A* algorithm </summary>
		/// <param name="start"> Start index 2d </param>
		/// <param name="destination"> Destination index 2d </param>
		/// <param name="moveCost"> Move cost data 2d array </param>
		/// <param name="moveCost_width"> 2d array's width </param>
		/// <param name="heuristic_cost"> Cost heuristic multiplier. Figure out yourself what value works best for your specific moveCost data </param>
		/// <param name="heuristic_search"> Search heuristic multiplier. Figure out yourself what value works best for your specific moveCost data </param>
		/// <param name="output_path"> Resulting path goes here </param>
		public unsafe AStarJob
		(
			INT2 start ,
			INT2 destination ,
			NativeArray<byte> moveCost ,
			int moveCost_width ,
			float heuristic_cost ,
			float heuristic_search ,
			NativeList<int2> output_path ,
			int step_limit = int.MaxValue
		)
		{
			this.start = start;
			this.destination = destination;
			this.moveCost = moveCost;
			this.moveCost_width = moveCost_width;
			this.Results = output_path;
			this.heuristic_cost = heuristic_cost;
			this.heuristic_search = heuristic_search;

			int length = moveCost.Length;
			int start1d = Index2dTo1d( start , moveCost_width );
			this._F_ = new NativeArray<float>( length , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
			this.solution = new NativeArray<int2>( length , Allocator.TempJob );
			this.frontier = new NativeMinHeap<int2,AStarJobComparer>(
				new AStarJobComparer( _F_ , moveCost_width , destination , heuristic_search ) ,
				Allocator.TempJob , length
			);
			this.visited = new NativeHashMap<int2,byte>( length , Allocator.TempJob );//TODO: use actual hashSet once available
			this.neighbours = new NativeList<int2>( 8 , Allocator.TempJob );
			this._step_limit = step_limit;
		}
		public void Execute ()
		{
			int length = moveCost.Length;
			int start1d = Index2dTo1d( start , moveCost_width );
			{
				for( int i=_F_.Length-1 ; i!=-1 ; i-- )
					_F_[i] = float.MaxValue;
				_F_[start1d] = 0;
			}
			solution[start1d] = start;
			frontier.Push( start );
			visited.TryAdd( start , 0 );
			
			//solve;
			float euclideanMaxLength = EuclideanHeuristicMaxLength( length , moveCost_width );
			int2 node = int2.zero-1;
			int step = 0;
			while( frontier.Count!=0 && math.any(node!=destination) && step++<_step_limit )
			{
				node = frontier.Pop();//we grab candidate with lowest F so far
				int node1d = Index2dTo1d( node , moveCost_width );
				float node_f = _F_[node1d];

				//lets check all its neighbours:
				EnumerateNeighbours( neighbours , moveCost_width , moveCost_width , node );
				int neighboursLength = neighbours.Length;
				for( int i=0 ; i<neighboursLength ; i++ )
				{
					int2 neighbour = neighbours[i];
					int neighbour1d = Index2dTo1d( neighbour , moveCost_width );
					
					bool isOrthogonal = math.any( node==neighbour );//is relative position orthogonal or diagonal

					float g = node_f + (moveCost[neighbour1d]/255f) * ( isOrthogonal ? 1f : 1.41421356237f );
					float h = EuclideanHeuristicNormalized( neighbour , destination , euclideanMaxLength )*heuristic_cost;
					float f = g + h;
					
					//update F when this connection is less costly:
					if( f<_F_[neighbour1d] )
					{
						_F_[neighbour1d] = f;
						solution[neighbour1d] = node;
					}

					//update frontier:
					if( visited.TryAdd(neighbour,0) )
						frontier.Push(neighbour);
				}
			}

			//create path:
			BacktrackToPath( solution , moveCost_width , destination , Results );
		}
		public void Dispose ()
		{
			this._F_.Dispose();
			this.solution.Dispose();
			this.frontier.Dispose();
			this.visited.Dispose();
			this.neighbours.Dispose();
		}
	}
	

	public unsafe struct AStarJobComparer : IComparerInt2
	{
		readonly void* _F_Ptr;
		readonly int _width;
		readonly int2 _dest;
		readonly float heuristic_search;
		public AStarJobComparer ( NativeArray<float> _G_ , int weightsWidth , INT2 destination , float heuristic_search )
		{
			this._F_Ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr( _G_ );
			this._width = weightsWidth;
			this._dest = destination;
			this.heuristic_search = heuristic_search;
		}
		int IComparerInt2.Compare ( int2 lhs , int2 rhs )
		{
			int lhs1d = Index2dTo1d( lhs , _width );
			int rhs1d = Index2dTo1d( rhs , _width );
			
			float euclideanHeuristicMaxLength = EuclideanHeuristicMaxLength(_width*_width,_width);
			
			float lhs_g = UnsafeUtility.ReadArrayElement<float>( _F_Ptr , lhs1d ) + EuclideanHeuristicNormalized(lhs,_dest,euclideanHeuristicMaxLength)*heuristic_search;
			float rhs_g = UnsafeUtility.ReadArrayElement<float>( _F_Ptr , rhs1d ) + EuclideanHeuristicNormalized(rhs,_dest,euclideanHeuristicMaxLength)*heuristic_search;

			return lhs_g.CompareTo(rhs_g);
		}
	}

	
	public static float EuclideanHeuristic ( INT2 a , INT2 b ) => math.length( a-b );
	public static float EuclideanHeuristicNormalized ( INT2 a , INT2 b , float maxLength ) => math.length( a-b ) / maxLength;
	public static float EuclideanHeuristicMaxLength ( INT arrayLength , INT arrayWidth ) => EuclideanHeuristic( int2.zero , new int2{ x=arrayWidth-1 , y=arrayLength/arrayWidth-1 } );


	/// <summary> Finds sequence of indices (a path) for given AStar solution </summary>
	/// <returns> Was destination reached </returns>
	public static bool BacktrackToPath
	(
		NativeArray<int2> solution ,
		INT solutionWidth ,
		INT2 destination ,
		NativeList<int2> path
	)
	{
		path.Clear();
		if( path.Capacity<solutionWidth*2 ) path.Capacity = solutionWidth*2;//preallocate for reasonably common/bad scenario
		int solutionLength = solution.Length;

		int2 pos = destination;
		int pos1d = Index2dTo1d( pos , solutionWidth );
		int step = 0;
		while(
			math.any( pos!=solution[pos1d] )
			&& step<solutionLength
		)
		{
			path.Add( pos );
			pos = solution[pos1d];
			pos1d = Index2dTo1d( pos , solutionWidth );
			step++;
		}
		bool wasDestinationReached = math.all( pos==solution[pos1d] );

		// TODO: can this step be avoided?
		ReverseArray<int2>( path );

		return wasDestinationReached;
	}
	/// <summary> Finds sequence of indices (a path) for given AStar solution </summary>
	/// <remarks> Uses segmented array for output </remarks>
	/// <returns> Was destination reached </returns>
	public static bool BacktrackToPath
	(
		NativeArray<int2> solvedGrid ,
		INT solvedGridWidth ,
		INT2 destination ,
		NativeArray<int2> segmentedIndices , // array segmented to store multiple paths
		INT segmentStart , // position for first path index2d
		INT segmentEnd , // position for last path index2d
		out int pathLength
	)
	{
		#if UNITY_ASSERTIONS
		ASSERT_TRUE( destination.x>=0 && destination.y>=0 , $"destination: {destination} >= 0" );
		ASSERT_TRUE( destination.x<solvedGridWidth && destination.y<solvedGridWidth , $"destination: {destination} < {solvedGridWidth} solutionWidth" );
		#endif

		int2 pos = destination;
		int pos1d = Index2dTo1d( pos , solvedGridWidth );
		int availableSpace = segmentEnd - segmentStart;
		int step = 0;

		#if UNITY_ASSERTIONS
		localAssertions();
		#endif
		
		while(
			math.any( pos!=solvedGrid[pos1d] )
			&& step<availableSpace
		)
		{
			int segmentedArrayIndex = segmentStart + step;
			
			#if UNITY_ASSERTIONS
			if( segmentedArrayIndex<segmentStart || segmentedArrayIndex>segmentEnd )
			{
				// throw new System.Exception
				Debug.LogError($"segmentedArrayIndex {segmentedArrayIndex} is outside it's range of {{{segmentStart}...{segmentEnd}}}");
			}
			#endif

			segmentedIndices[segmentedArrayIndex] = pos;
			pos = solvedGrid[pos1d];
			pos1d = Index2dTo1d( pos , solvedGridWidth );

			#if UNITY_ASSERTIONS
			localAssertions();
			#endif

			step++;
		}
		pathLength = step;
		bool wasDestinationReached = math.all( pos==solvedGrid[pos1d] );

		#if UNITY_ASSERTIONS
		for( int n=0 ; n<pathLength ; n++ )
		{
			int index = segmentStart + n;
			if( math.all(segmentedIndices[index]==int2.zero) )
			{
				// throw new System.Exception
				Debug.LogError($"segmentedIndices[{index}] is {segmentedIndices[index]}, segmentStart: {segmentStart}, segmentEnd: {segmentEnd}, pathLength: {pathLength}, step: {step}");
			}
		}
		#endif

		// TODO: can this step be avoided?
		// reverse path order:
		{
			int first = segmentStart;
			int last = segmentStart + math.max( pathLength-1 , 0 );
			
			#if UNITY_ASSERTIONS
			if( last>segmentEnd )
			{
				// throw new System.Exception
				Debug.LogError($"last {last} > {segmentEnd} segmentEnd");
			}
			#endif

			ReverseArraySegment( segmentedIndices , first , last );
		}

		#if UNITY_ASSERTIONS
		void localAssertions ()
		{
			FixedString128 debugInfo = $"pos: {pos}, pos1d:{pos1d}, solution.Length:{solvedGrid.Length}, solutionWidth:{solvedGridWidth} squared: {solvedGridWidth}";
			ASSERT_TRUE( pos1d>=0 , debugInfo );
			ASSERT_TRUE( pos1d<solvedGrid.Length , debugInfo );
		}
		#endif

		return wasDestinationReached;
	}

	public static void ReverseArray <T> ( NativeArray<T> array ) where T : unmanaged
	{
		int length = array.Length;
		int lengthHalf = length / 2;
		int last = length-1;
		for( int i=0 ; i<lengthHalf ; i++ )
		{
			var tmp = array[i];
			array[i] = array[last-i];
			array[last-i] = tmp;
		}
	}

	public static void ReverseArraySegment <T> ( NativeArray<T> array , INT first , INT last ) where T : unmanaged
	{
		int length = last - first;
		int lengthHalf = length / 2;
		for( int step=0 ; step<lengthHalf ; step++ )
		{
			var tmp = array[first+step];
			array[first+step] = array[last-step];
			array[last-step] = tmp;
		}
	}


	#endregion
}
