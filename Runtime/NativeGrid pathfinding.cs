/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid
#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

using Debug = UnityEngine.Debug;

namespace NativeGridNamespace
{
	/// <summary> Non-generic, abstract parent class for NativeGrid<T>. </summary>
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

			public readonly int2 Start;
			public readonly int2 Destination;
			[ReadOnly] public readonly NativeArray<byte> MoveCost;
			public readonly int MoveCostWidth;
			public readonly float HMultiplier;
			public readonly float MoveCostSensitivity;
			public readonly int StepBudget;
			public readonly bool ResultsStartAtIndexZero;

			public NativeArray<half> G;
			public NativeArray<half> F;
			public NativeArray<int2> Solution;
			public NativeMinHeap<int2,half,Comparer> Frontier;
			public NativeHashSet<int2> Visited;

			ProfilerMarker _PM_Initialization, _PM_Search, _PM_Neighbours, _PM_FrontierPush, _PM_FrontierPop, _PM_UpdateFG, _PM_Trace;

			/// <summary> Traces path using some kind of A* algorithm </summary>
			/// <param name="start"> Start index 2d </param>
			/// <param name="destination"> Destination index 2d </param>
			/// <param name="moveCost"> Move cost data 2d array in 0.0-1.0 range format. Cells with value >= 1.0 are considered impassable. </param>
			/// <param name="moveCostWidth"> 2d array's width </param>
			/// <param name="results"> Resulting path goes here </param>
			/// <param name="hMultiplier"> Heuristic factor multiplier. Increasing this over 1.0 makes lines more straight and decrease cpu usage. </param>
			/// <param name="moveCostSensitivity"> Makes algorith evade cells with move cost > 0 more.</param>
			/// <param name="stepBudget"> CPU time budget you give this job. Expressind in number steps search algorihm is allowed to take.</param>
			/// <param name="resultsStartAtIndexZero"> When this is true then path indices in <param name="results"> will be ordered starting from index 0 and eding at index length-1. False reverses this order.</param>
			public AStarJob
			(
				INT2 start ,
				INT2 destination ,
				NativeArray<byte> moveCost ,
				int moveCostWidth ,
				NativeList<int2> results ,
				float hMultiplier = 1 ,
				float moveCostSensitivity = 1 ,
				int stepBudget = int.MaxValue ,
				bool resultsStartAtIndexZero = true
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
				this.ResultsStartAtIndexZero = resultsStartAtIndexZero;

				int length = moveCost.Length;
				int startIndex = CoordToIndex( start , moveCostWidth );
				this.G = new NativeArray<half>( length , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
				this.F = new NativeArray<half>( length , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
				this.Solution = new NativeArray<int2>( length , Allocator.TempJob );
				this.Frontier = new NativeMinHeap<int2,half,Comparer>( length , Allocator.TempJob , new Comparer( moveCostWidth ) , this.F );
				this.Visited = new NativeHashSet<int2>( length , Allocator.TempJob );
				
				this._PM_Initialization = new ProfilerMarker("initialization");
				this._PM_Search = new ProfilerMarker("search");
				this._PM_Neighbours = new ProfilerMarker("scan neighbors");
				this._PM_FrontierPush = new ProfilerMarker("frontier.push");
				this._PM_FrontierPop = new ProfilerMarker("frontier.pop");
				this._PM_UpdateFG = new ProfilerMarker("update f & g");
				this._PM_Trace = new ProfilerMarker("trace path");
			}
			public void Execute ()
			{
				_PM_Initialization.Begin();
				int startIndex = CoordToIndex( Start , MoveCostWidth );
				int destIndex = CoordToIndex( Destination , MoveCostWidth );
				{
					// early test for unsolvable input:
					if( (MoveCost[startIndex]/255f)>=1 ) return;
					if( (MoveCost[destIndex]/255f)>=1 ) return;
				}
				{
					// initialize GData array:
					for( int i=G.Length-1 ; i!=-1 ; i-- )
						G[i] = (half) half.MaxValue;
					G[startIndex] = half.zero;
				}
				{
					// initialize FData array:
					for( int i=F.Length-1 ; i!=-1 ; i-- )
						F[i] = (half) half.MaxValue;
					F[startIndex] = half.zero;
				}
				Solution[startIndex] = Start;
				Frontier.Push( Start );
				Visited.Add( Start );
				_PM_Initialization.End();
				
				// solve
				_PM_Search.Begin();
				int moveCostHeight = MoveCost.Length / MoveCostWidth;
				int2 currentCoord = -1;
				int numSearchSteps = 0;
				bool destinationReached = false;
				while(
						Frontier.Length!=0
					&&	!( destinationReached = math.all(currentCoord==Destination) )
					&&	numSearchSteps++<StepBudget
				)
				{
					_PM_Initialization.Begin();
					_PM_FrontierPop.Begin();
					currentCoord = Frontier.Pop();// grab candidate with lowest F
					_PM_FrontierPop.End();
					int currentIndex = CoordToIndex( currentCoord , MoveCostWidth );
					float node_g = G[currentIndex];
					_PM_Initialization.End();

					// lets check all its neighbours:
					_PM_Neighbours.Begin();
					var enumerator = new NeighbourEnumerator( coord:currentCoord , gridWidth:MoveCostWidth , gridHeight:moveCostHeight );
					while( enumerator.MoveNext(out int2 neighbourCoord) )
					{
						int neighbourIndex = CoordToIndex( neighbourCoord , MoveCostWidth );
						bool orthogonal = math.any(currentCoord==neighbourCoord);
						byte moveCostByte = MoveCost[neighbourIndex];
						if( moveCostByte==(byte)255 ) continue;// 100% obstacle
						float movecost = ( moveCostByte / 255f ) * MoveCostSensitivity;

						// g - exact dist from start node
						// h - approx dist to dest node as predicted by heuristic func
						float g = node_g + ( 1f + movecost ) * ( orthogonal ? 1f : 1.41421356237f );
						float h = EuclideanHeuristic( neighbourCoord , Destination ) * HMultiplier;
						float f = g + h;
						
						// update F & G:
						if( g<G[neighbourIndex] )
						{
							_PM_UpdateFG.Begin();
							F[neighbourIndex] = (half) f;
							G[neighbourIndex] = (half) g;
							Solution[neighbourIndex] = currentCoord;
							_PM_UpdateFG.End();
						}

						// update frontier:
						_PM_FrontierPush.Begin();
						if( !Visited.Contains(neighbourCoord) )
							// if( !Frontier.AsArray().Contains(neighbour) )
							Frontier.Push(neighbourCoord);
						_PM_FrontierPush.End();

						// update frontier:
						Visited.Add(neighbourCoord);
					}
					_PM_Neighbours.End();
				}
				_PM_Search.End();

				// create path:
				_PM_Trace.Begin();
				if( destinationReached )
				{
					// Debug.Log($"A* job took {step} steps, path resolved.");

					bool backtrackSuccess = BacktrackToPath( Solution , MoveCostWidth , Destination , Results , ResultsStartAtIndexZero );
					
					#if UNITY_ASSERTIONS
					Assert.IsTrue( backtrackSuccess );
					#endif
				}
				else
				{
					// Debug.Log($"A* job took {step} steps, <b>no path found</b>.");
					
					Results.Clear();// make sure to communite there is no path
				}
				_PM_Trace.End();
			}
			public void Dispose ()
			{
				this.G.Dispose();
				this.F.Dispose();
				this.Solution.Dispose();
				this.Frontier.Dispose();
				this.Visited.Dispose();
			}
			public struct Comparer : INativeMinHeapComparer<int2,half>
			{
				public int Width;
				public Comparer ( int width ) => this.Width = width;
				public int Compare( int2 lhs , int2 rhs , NativeSlice<half> comparables )
				{
					float lhsValue = comparables[ CoordToIndex(lhs,Width) ];
					float rhsValue = comparables[ CoordToIndex(rhs,Width) ];
					return lhsValue.CompareTo(rhsValue);
				}
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
			NativeList<int2> results ,
			bool resultsStartAtIndexZero
		)
		{
			results.Clear();
			if( results.Capacity<width*2 ) results.Capacity = width*2;
			int solutionLength = solution.Length;

			int2 posCoord = destination;
			int posIndex = CoordToIndex( posCoord , width );
			int step = 0;
			while( !math.all(posCoord==solution[posIndex]) && step<solutionLength )
			{
				results.Add( posCoord );
				posCoord = solution[posIndex];
				posIndex = CoordToIndex( posCoord , width );
				step++;
			}
			bool wasDestinationReached = math.all( posCoord==solution[posIndex] );

			if( resultsStartAtIndexZero )
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
			INT segmentStart , // position for first path coord
			INT segmentEnd , // position for last path coord
			out int pathLength
		)
		{
			#if UNITY_ASSERTIONS
			ASSERT_TRUE( destination.x>=0 && destination.y>=0 , $"destination: {destination} >= 0" );
			ASSERT_TRUE( destination.x<solvedGridWidth && destination.y<solvedGridWidth , $"destination: {destination} < {solvedGridWidth} solutionWidth" );
			#endif

			int2 posCoord = destination;
			int posIndex = CoordToIndex( posCoord , solvedGridWidth );
			int availableSpace = segmentEnd - segmentStart;
			int step = 0;

			#if UNITY_ASSERTIONS
			localAssertions();
			#endif
			
			while(
				math.any( posCoord!=solvedGrid[posIndex] )
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

				segmentedIndices[segmentedArrayIndex] = posCoord;
				posCoord = solvedGrid[posIndex];
				posIndex = CoordToIndex( posCoord , solvedGridWidth );

				#if UNITY_ASSERTIONS
				localAssertions();
				#endif

				step++;
			}
			pathLength = step;
			bool wasDestinationReached = math.all( posCoord==solvedGrid[posIndex] );

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
				FixedString128 debugInfo = $"posCoord: {posCoord}, posIndex:{posIndex}, solution.Length:{solvedGrid.Length}, solutionWidth:{solvedGridWidth} squared: {solvedGridWidth}";
				ASSERT_TRUE( posIndex>=0 , debugInfo );
				ASSERT_TRUE( posIndex<solvedGrid.Length , debugInfo );
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
}
