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


	/// <summary> Traces path using A* algorithm </summary>
	/// Format weights to 0.0 to 1.0 range. Heuristic can cease to work otherwise.
	[Unity.Burst.BurstCompile]
	public struct AStarJob : IJob, System.IDisposable
	{

		/// <summary> Job results goes here. List of indices to form a path. </summary>
		public NativeList<int2> Results;

		readonly int2 Start;
		readonly int2 Destination;
		[ReadOnly] readonly NativeArray<byte> MoveCost;
		readonly int MoveCostWidth;
		readonly float HMultiplier;
		readonly float MoveCostSensitivity;

		public NativeArray<half> GData;
		public NativeArray<half> FData;
		public NativeArray<int2> Solution;
		public NativeMinHeap<int2,AStarJobComparer> Frontier;
		public NativeHashSet<int2> Visited;
		public NativeList<int2> Neighbours;
		public int StepBudget;

		/// <summary> Traces path using some kind of A* algorithm </summary>
		/// <param name="start"> Start index 2d </param>
		/// <param name="destination"> Destination index 2d </param>
		/// <param name="moveCost"> Move cost data 2d array in 0.0-1.0 range format. Cells with value >= 1.0 are considered impassable. </param>
		/// <param name="moveCostWidth"> 2d array's width </param>
		/// <param name="results"> Resulting path goes here </param>
		/// <param name="hMultiplier"> Heuristic factor multiplier. Increasing this over 1.0 makes lines more straight and decrease cpu usage. </param>
		/// <param name="moveCostSensitivity"> Makes algorith evade cells with move cost > 0 more.</param>
		/// <param name="stepBudget"> CPU time budget you give this job. Expressind in number steps search algorihm is allowed to take.</param>
		public unsafe AStarJob
		(
			INT2 start ,
			INT2 destination ,
			NativeArray<byte> moveCost ,
			int moveCostWidth ,
			NativeList<int2> results ,
			float hMultiplier = 1 ,
			float moveCostSensitivity = 1 ,
			int stepBudget = int.MaxValue
		)
		{
			this.Start = start;
			this.Destination = destination;
			this.MoveCost = moveCost;
			this.MoveCostWidth = moveCostWidth;
			this.Results = results;
			this.HMultiplier = hMultiplier;
			this.MoveCostSensitivity = moveCostSensitivity;
			this.StepBudget = stepBudget;

			int length = moveCost.Length;
			int start1d = Index2dTo1d( start , moveCostWidth );
			this.GData = new NativeArray<half>( length , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
			this.FData = new NativeArray<half>( length , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
			this.Solution = new NativeArray<int2>( length , Allocator.TempJob );
			this.Frontier = new NativeMinHeap<int2,AStarJobComparer>(
				new AStarJobComparer( FData , moveCostWidth ) ,
				Allocator.TempJob , length
			);
			this.Visited = new NativeHashSet<int2>( length , Allocator.TempJob );
			this.Neighbours = new NativeList<int2>( 8 , Allocator.TempJob );
		}
		public void Execute ()
		{
			int start1d = Index2dTo1d( Start , MoveCostWidth );
			int dest1d = Index2dTo1d( Destination , MoveCostWidth );
			{
				// early test for unsolvable input:
				if( (MoveCost[start1d]/255f)>=1 ) return;
				if( (MoveCost[dest1d]/255f)>=1 ) return;
			}
			{
				// initialize GDAta array:
				for( int i=GData.Length-1 ; i!=-1 ; i-- )
					GData[i] = (half) half.MaxValue;
				GData[start1d] = half.zero;
			}
			{
				// initialize FDAta array:
				for( int i=FData.Length-1 ; i!=-1 ; i-- )
					FData[i] = (half) half.MaxValue;
				FData[start1d] = half.zero;
			}
			Solution[start1d] = Start;
			Frontier.Push( Start );
			Visited.Add( Start );
			
			// solve
			int2 node = -1;
			int step = 0;
			bool destinationReached = false;
			while(
					Frontier.Length!=0
				&&	!( destinationReached = math.all(node==Destination) )
				&&	step<StepBudget
			)
			{
				node = Frontier.Pop();// we grab candidate with lowest F
				int node1d = Index2dTo1d( node , MoveCostWidth );
				float node_g = GData[node1d];

				// string frontierBefore = Frontier.ToString();
				// node = Frontier.Pop();// we grab candidate with lowest F so far
				// string frontierAfter = Frontier.ToString();
				// Debug.Log($"step {step} at [{node.x},{node.y}]");// \nfrontier before: {frontierBefore}\nfrontier after: {frontierAfter}"

				// lets check all its neighbours:
				EnumerateNeighbours( Neighbours , MoveCostWidth , MoveCostWidth , node );
				int neighboursLength = Neighbours.Length;
				for( int i=0 ; i<neighboursLength ; i++ )
				{
					int2 neighbour = Neighbours[i];
					int neighbour1d = Index2dTo1d( neighbour , MoveCostWidth );
					bool orthogonal = math.any(node==neighbour);
					float movecost = MoveCost[neighbour1d] / 255f;
					if( movecost<1f )
					{
						movecost *= MoveCostSensitivity;

						// g - dist from start node
						// h - dist from dest node as predicted by heuristic func

						float g = node_g + ( 1f + movecost ) * ( orthogonal ? 1f : 1.41421356237f );
						float h = EuclideanHeuristic( neighbour , Destination ) * HMultiplier;
						float f = g + h;
						
						// update G:
						if( g<GData[neighbour1d] )
						{
							GData[neighbour1d] = (half) math.min( g , half.MaxValue );
						}

						// update F:
						if( f<FData[neighbour1d] )
						{
							FData[neighbour1d] = (half) math.min( f , half.MaxValue );
							Solution[neighbour1d] = node;
						}

						// update frontier:
						if( !Visited.Contains(neighbour) )
							Frontier.Push(neighbour);
					}

					// update frontier:
					Visited.Add(neighbour);
				}

				step++;
			}

			// create path:
			if( destinationReached )
			{
				// Debug.Log($"A* job took {step} steps, path resolved.");

				bool backtrackSuccess = BacktrackToPath( Solution , MoveCostWidth , Destination , Results );
				Assert.IsTrue( backtrackSuccess );
			}
			else
			{
				// Debug.Log($"A* job took {step} steps, <b>no path found</b>.");
				
				Results.Clear();// make sure to communite there is no path
			}
		}
		public void Dispose ()
		{
			this.GData.Dispose();
			this.FData.Dispose();
			this.Solution.Dispose();
			this.Frontier.Dispose();
			this.Visited.Dispose();
			this.Neighbours.Dispose();
		}
	}
	

	public unsafe struct AStarJobComparer : IComparerInt2
	{
		readonly void* _ptr;
		readonly int _width;
		public AStarJobComparer ( NativeArray<half> weights , int width )
		{
			this._ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr( weights );
			this._width = width;
		}
		int IComparerInt2.Compare ( int2 lhs , int2 rhs )
		{
			float lhsValue = UnsafeUtility.ReadArrayElement<half>( _ptr , Index2dTo1d(lhs,_width) );
			float rhsValue = UnsafeUtility.ReadArrayElement<half>( _ptr , Index2dTo1d(rhs,_width) );
			return lhsValue.CompareTo(rhsValue);
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
		INT width ,
		INT2 destination ,
		NativeList<int2> results
	)
	{
		results.Clear();
		if( results.Capacity<width*2 ) results.Capacity = width*2;
		int solutionLength = solution.Length;

		int2 pos = destination;
		int pos1d = Index2dTo1d( pos , width );
		int step = 0;
		while( !math.all(pos==solution[pos1d]) && step<solutionLength )
		{
			results.Add( pos );
			pos = solution[pos1d];
			pos1d = Index2dTo1d( pos , width );
			step++;
		}
		bool wasDestinationReached = math.all( pos==solution[pos1d] );

		// TODO: can this step be avoided?
		ReverseArray<int2>( results );

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
